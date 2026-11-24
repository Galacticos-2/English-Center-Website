using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class ExerciseResultService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentGamificationService _studentGamificationService;

    public ExerciseResultService(
        IUnitOfWork unitOfWork,
        IStudentGamificationService studentGamificationService)
    {
        _unitOfWork = unitOfWork;
        _studentGamificationService = studentGamificationService;
    }
    public async Task GradeAttemptAsync(
    int teacherUserId,
    GradeExerciseAttemptDto dto)
    {
        var attempt = await _unitOfWork.ExerciseAttempts
            .GetByIdAsync(dto.ExerciseAttemptId);

        if (attempt == null)
        {
            throw new Exception("Exercise attempt not found");
        }

        if (attempt.ExerciseAssignment.Exercise.TeacherUserId != teacherUserId)
        {
            throw new Exception("You cannot grade this attempt");
        }

        if (attempt.Status != ExerciseAttemptStatus.PendingReview)
        {
            throw new Exception("Only pending review attempts can be graded");
        }

        foreach (var gradeDto in dto.Answers)
        {
            var answer = attempt.Answers
                .FirstOrDefault(x =>
                    x.ExerciseAnswerId == gradeDto.ExerciseAnswerId);

            if (answer == null)
            {
                throw new Exception("Answer not found");
            }

            var exerciseQuestion = attempt.ExerciseAssignment.Exercise
                .ExerciseQuestions
                .Concat(
                    attempt.ExerciseAssignment.Exercise.QuestionGroups
                        .SelectMany(g => g.ExerciseQuestions)
                )
                .FirstOrDefault(x => x.QuestionId == answer.QuestionId);

            if (exerciseQuestion == null)
            {
                throw new Exception("Exercise question not found");
            }

            if (gradeDto.ManualScore < 0 ||
                gradeDto.ManualScore > exerciseQuestion.Point)
            {
                throw new Exception("Manual score is invalid");
            }

            answer.ManualScore = gradeDto.ManualScore;
            answer.PointEarned = gradeDto.ManualScore;
            answer.TeacherFeedback = gradeDto.TeacherFeedback;
            answer.GradedByTeacherUserId = teacherUserId;
            answer.GradedAt = DateTime.UtcNow;
            answer.IsCorrect = gradeDto.ManualScore > 0;
            answer.UpdatedAt = DateTime.UtcNow;
        }

        attempt.Score = attempt.Answers.Sum(x => x.PointEarned);
        attempt.CorrectAnswers = attempt.Answers.Count(x => x.IsCorrect);
        attempt.Status = ExerciseAttemptStatus.Reviewed;
        attempt.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        await _studentGamificationService.AwardExerciseSubmissionAsync(
            attempt.ExerciseAttemptId);
    }
    public async Task<TeacherExerciseAnalyticsDto> GetTeacherResultsAsync(
        int teacherUserId,
        int? exerciseAssignmentId)
    {
        var query = _unitOfWork.ExerciseAttempts
            .Query()
            .Where(x =>
                x.ExerciseAssignment.Exercise.TeacherUserId ==
                teacherUserId);

        if (exerciseAssignmentId.HasValue)
        {
            query = query.Where(x =>
                x.ExerciseAssignmentId ==
                exerciseAssignmentId.Value);
        }

        var results = await query
            .OrderByDescending(x => x.StartedAt)
            .Select(x => new TeacherExerciseResultDto
            {
                ExerciseAttemptId = x.ExerciseAttemptId,
                ExerciseAssignmentId = x.ExerciseAssignmentId,
                ExerciseTitle = x.ExerciseAssignment.Exercise.Title,
                StudentName = x.StudentRole.User.FullName,
                ClassRoomName = x.ExerciseAssignment.ClassRoom.ClassName,
                TotalQuestions = x.TotalQuestions,
                CorrectAnswers = x.CorrectAnswers,
                TotalPoints = x.TotalPoints,
                Score = x.Score,
                Answers = x.Answers.Select(answer => new ExerciseAnswerResultDto
                {
                    ExerciseAnswerId = answer.ExerciseAnswerId,
                    QuestionId = answer.QuestionId ?? 0,
                    QuestionContent = answer.Question != null
    ? answer.Question.Content
    : "",
                    Explanation = answer.Question != null
    ? answer.Question.Explanation
    : null,
                    SelectedQuestionOptionId = answer.SelectedQuestionOptionId,
                    TextAnswer = answer.TextAnswer,
                    AudioAnswerUrl = answer.AudioAnswerUrl,
                    IsCorrect = answer.IsCorrect,

                    PointEarned = answer.PointEarned,
                    ManualScore = answer.ManualScore,
                    TeacherFeedback = answer.TeacherFeedback,
                    GradedByTeacherUserId = answer.GradedByTeacherUserId,
                    GradedAt = answer.GradedAt,
                    WritingTaskId = answer.WritingTaskId,
                    WritingTaskTitle = answer.WritingTaskId != null
    ? x.ExerciseAssignment.Exercise.WritingTasks
        .Where(w => w.WritingTaskId == answer.WritingTaskId)
        .Select(w => w.WritingTask.Title)
        .FirstOrDefault()
    : null,
                    WritingOverallBand = answer.ManualScore,
                    WritingFeedback = answer.TeacherFeedback,
                    WritingStatus = x.Status.ToString()
                }).ToList(),
                StartedAt = x.StartedAt,
                SubmittedAt = x.SubmittedAt,
                Status = x.Status.ToString()
            })
            .ToListAsync();

        var completedResults = results
    .Where(x =>
        x.Status == ExerciseAttemptStatus.Submitted.ToString() ||
        x.Status == ExerciseAttemptStatus.Reviewed.ToString() ||
        x.Status == ExerciseAttemptStatus.AiGraded.ToString())
    .ToList();

        return new TeacherExerciseAnalyticsDto
        {
            TotalAttempts = results.Count,
            SubmittedAttempts = completedResults.Count,
            AverageScore = completedResults.Any()
    ? completedResults.Average(x => x.Score)
    : 0,
            HighestScore = completedResults.Any()
    ? completedResults.Max(x => x.Score)
    : 0,
            LowestScore = completedResults.Any()
    ? completedResults.Min(x => x.Score)
    : 0,
            Results = results
        };
    }
}
