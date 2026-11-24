using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using Microsoft.EntityFrameworkCore;
using EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

using EnglishCenterSystem.Application
    .Features.Notifications.Services;

using EnglishCenterSystem.Domain.Enums;

using Microsoft.Extensions.Logging;
namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class ExerciseAssignmentService
{
    private readonly IUnitOfWork
    _unitOfWork;

    private readonly NotificationService
        _notificationService;

    private readonly ILogger<
        ExerciseAssignmentService> _logger;

    public ExerciseAssignmentService(
        IUnitOfWork unitOfWork,
        NotificationService notificationService,
        ILogger<ExerciseAssignmentService> logger)
    {
        _unitOfWork = unitOfWork;

        _notificationService =
            notificationService;

        _logger = logger;
    }

    public async Task<List<ExerciseAssignmentDto>> GetAssignmentsAsync(
        int teacherUserId)
    {
        return await _unitOfWork.ExerciseAssignments
            .Query()
            .Where(x => x.Exercise.TeacherUserId == teacherUserId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ExerciseAssignmentDto
            {
                ExerciseAssignmentId = x.ExerciseAssignmentId,
                ExerciseId = x.ExerciseId,
                ExerciseTitle = x.Exercise.Title,
                ClassRoomId = x.ClassRoomId,
                ClassRoomName = x.ClassRoom.ClassName,
                
                MaxAttempts = x.MaxAttempts,
                DeadlineAt = x.DeadlineAt,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task CreateAssignmentAsync(
        int teacherUserId,
        CreateExerciseAssignmentDto dto)
    {
        

        if (dto.MaxAttempts <= 0)
        {
            throw new Exception("Max attempts must be greater than 0");
        }

        if (dto.DeadlineAt.HasValue && dto.DeadlineAt <= DateTime.UtcNow)
        {
            throw new Exception("Deadline must be in the future");
        }

        var exercise = await _unitOfWork.Exercises
            .Query()
            .FirstOrDefaultAsync(x =>
                x.ExerciseId == dto.ExerciseId &&
                x.TeacherUserId == teacherUserId);

        if (exercise == null)
        {
            throw new Exception("Exercise not found");
        }
        if (!exercise.IsActive)
        {
            throw new Exception("Exercise set is inactive");
        }

        var hasQuestions =
    exercise.ExerciseQuestions.Any() ||
    exercise.QuestionGroups.Any(x =>
        x.ExerciseQuestions.Any()) ||
    exercise.ListeningTasks.Any(x =>
        x.ListeningTask.Questions.Any()) ||
    exercise.ReadingTasks.Any(x =>
        x.ReadingTask.Questions.Any()) ||
    exercise.SpeakingTasks.Any() ||
    exercise.WritingTasks.Any();

        if (!hasQuestions)
        {
            throw new Exception(
                "Cannot assign exercise set without questions");
        }
        var classRoom = await _unitOfWork.ClassRooms
            .Query()
            .FirstOrDefaultAsync(x =>
                x.ClassRoomId == dto.ClassRoomId);

        if (classRoom == null)
        {
            throw new Exception("Class room not found");
        }
        var teacherHasClass =
    await _unitOfWork
        .TeacherRoleRepository
        .Query()
        .Where(x =>
            x.UserId ==
                teacherUserId)
        .SelectMany(x =>
            x.TeacherClasses)
        .AnyAsync(x =>
            x.ClassRoomId ==
                dto.ClassRoomId);

        if (!teacherHasClass)
        {
            throw new Exception(
                "Teacher is not assigned to this class");
        }
        var existed = await _unitOfWork.ExerciseAssignments
            .Query()
            .AnyAsync(x =>
                x.ExerciseId == dto.ExerciseId &&
                x.ClassRoomId == dto.ClassRoomId);

        if (existed)
        {
            throw new Exception(
                "This exercise has already been assigned to this class");
        }

        var assignment = new ExerciseAssignment
        {
            ExerciseId = dto.ExerciseId,
            ClassRoomId = dto.ClassRoomId,
         
            MaxAttempts = dto.MaxAttempts,
            DeadlineAt = dto.DeadlineAt,
            IsActive = true
        };

        await _unitOfWork.ExerciseAssignments
    .AddAsync(assignment);

        await _unitOfWork.SaveChangesAsync();

        await CreateExerciseNotificationSafelyAsync(
            teacherUserId,
            exercise.Title,
            classRoom.ClassRoomId,
            classRoom.ClassName,
            assignment.ExerciseAssignmentId);
    }
    private async Task
    CreateExerciseNotificationSafelyAsync(
        int teacherUserId,
        string exerciseTitle,
        int classRoomId,
        string classRoomName,
        int exerciseAssignmentId)
    {
        try
        {
            await _notificationService
                .CreateForClassAsync(
                    new CreateClassNotificationDto
                    {
                        CreatedByUserId =
                            teacherUserId,

                        ClassRoomId =
                            classRoomId,

                        Type =
                            NotificationType
                                .ExerciseAssigned,

                        RelatedEntityId =
                            exerciseAssignmentId,

                        Title =
                            "New exercise assigned",

                        Message =
                            $"Exercise \"{exerciseTitle}\" " +
                            $"has been assigned to class " +
                            $"{classRoomName}.",

                        TeacherActionUrl =
                            "/teacher/exercises",

                        StudentActionUrl =
                            "/student/exercises"
                    });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not create exercise notification for ExerciseAssignmentId {ExerciseAssignmentId}",
                exerciseAssignmentId);
        }
    }
    public async Task DeleteAssignmentAsync(
    int teacherUserId,
    int exerciseAssignmentId)
    {
        var assignment = await _unitOfWork.ExerciseAssignments
            .GetByIdAsync(exerciseAssignmentId);

        if (assignment == null ||
            assignment.Exercise.TeacherUserId != teacherUserId)
        {
            throw new Exception("Exercise assignment not found");
        }

        var attempts = await _unitOfWork.ExerciseAttempts
    .Query()
    .Where(x =>
        x.ExerciseAssignmentId == exerciseAssignmentId)
    .ToListAsync();

        _unitOfWork.ExerciseAttempts.RemoveRange(attempts);

        _unitOfWork.ExerciseAssignments.Remove(assignment);

        await _unitOfWork.SaveChangesAsync();
    }
}
