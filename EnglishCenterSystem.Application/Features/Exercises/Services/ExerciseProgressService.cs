using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class ExerciseProgressService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExerciseProgressService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<TeacherExerciseProgressDto>>
        GetTeacherProgressAsync(int teacherUserId)
    {
        var assignments = await _unitOfWork.ExerciseAssignments
            .Query()
            .Where(x =>
                x.Exercise.TeacherUserId == teacherUserId &&
                x.IsActive)
            .ToListAsync();

        var classRoomIds = assignments
            .Select(x => x.ClassRoomId)
            .Distinct()
            .ToList();

        var students = await _unitOfWork.StudentRoles
            .Where(x =>
                x.ClassRoomId != null &&
                classRoomIds.Contains(x.ClassRoomId.Value))
            .Include(x => x.User)
            .Include(x => x.ClassRoom)
            .ToListAsync();

        var attempts = await _unitOfWork.ExerciseAttempts
            .Query()
            .Where(x =>
                assignments
                    .Select(a => a.ExerciseAssignmentId)
                    .Contains(x.ExerciseAssignmentId))
            .ToListAsync();

        return students.Select(student =>
        {
            var studentAssignments = assignments
                .Where(x => x.ClassRoomId == student.ClassRoomId)
                .ToList();

            var studentAttempts = attempts
                .Where(x => x.StudentRoleId == student.StudentRoleId)
                .ToList();

            var submittedAttempts = studentAttempts
                .Where(x =>
                    x.Status == ExerciseAttemptStatus.Submitted ||
x.Status == ExerciseAttemptStatus.Reviewed ||
x.Status == ExerciseAttemptStatus.AiGraded)
                .ToList();

            var completedAssignmentIds = submittedAttempts
                .Select(x => x.ExerciseAssignmentId)
                .Distinct()
                .ToList();

            var completed = studentAssignments.Count(x =>
                completedAssignmentIds.Contains(
                    x.ExerciseAssignmentId));

            var totalAssigned = studentAssignments.Count;

            return new TeacherExerciseProgressDto
            {
                StudentRoleId = student.StudentRoleId,
                StudentName = student.User.FullName,
                ClassRoomName = student.ClassRoom?.ClassName ?? "",
                TotalAssignedExercises = totalAssigned,
                CompletedExercises = completed,
                CompletionPercent = totalAssigned == 0
                    ? 0
                    : Math.Round(
                        (double)completed / totalAssigned * 100,
                        2),
                AverageScore = submittedAttempts.Any()
                    ? Math.Round(
                        submittedAttempts.Average(x => x.Score),
                        2)
                    : 0
            };
        }).ToList();
    }
}