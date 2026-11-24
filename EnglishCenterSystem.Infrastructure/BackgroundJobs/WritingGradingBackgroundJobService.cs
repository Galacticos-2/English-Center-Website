using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Writing;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.BackgroundJobs;

public class WritingGradingBackgroundJobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWritingAiEvaluationService _writingAiEvaluationService;
    private readonly IStudentGamificationService _studentGamificationService;

    public WritingGradingBackgroundJobService(
        IUnitOfWork unitOfWork,
        IWritingAiEvaluationService writingAiEvaluationService,
        IStudentGamificationService studentGamificationService)
    {
        _unitOfWork = unitOfWork;
        _writingAiEvaluationService = writingAiEvaluationService;
        _studentGamificationService = studentGamificationService;
    }

    public async Task ProcessAsync(int writingSubmissionId)
    {
        var submission = await _unitOfWork.WritingSubmissions
            .Query()
            .FirstOrDefaultAsync(x =>
                x.WritingSubmissionId == writingSubmissionId);

        if (submission == null)
        {
            throw new Exception("Writing submission not found");
        }

        if (submission.Status == WritingSubmissionStatus.Completed)
        {
            return;
        }

        var attempt = await _unitOfWork.ExerciseAttempts
            .GetByIdAsync(submission.ExerciseAttemptId);

        if (attempt == null)
        {
            throw new Exception("Exercise attempt not found");
        }

        var writingTask = await _unitOfWork.WritingTasks
            .GetByIdAsync(submission.WritingTaskId);

        if (writingTask == null)
        {
            throw new Exception("Writing task not found");
        }

        try
        {
            submission.Status = WritingSubmissionStatus.Grading;
            submission.UpdatedAt = DateTime.UtcNow;

            attempt.Status = ExerciseAttemptStatus.AiGrading;
            attempt.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(
                    submission.EssayText))
            {
                var blankAnswer = attempt.Answers
                    .FirstOrDefault(x =>
                        x.WritingTaskId ==
                        submission.WritingTaskId);

                if (blankAnswer != null)
                {
                    blankAnswer.PointEarned = 0;
                    blankAnswer.ManualScore = 0;
                    blankAnswer.IsCorrect = false;
                    blankAnswer.TeacherFeedback =
                        "Student did not submit a writing answer";
                    blankAnswer.GradedAt = DateTime.UtcNow;
                    blankAnswer.UpdatedAt = DateTime.UtcNow;
                }

                await CompleteSubmissionAsync(submission);
                return;
            }

            var aiResult =
                await _writingAiEvaluationService.EvaluateAsync(
                    writingTask,
                    submission.EssayText);

            var existingScore = await _unitOfWork.WritingScores
                .Query()
                .FirstOrDefaultAsync(x =>
                    x.WritingSubmissionId ==
                    submission.WritingSubmissionId);

            if (existingScore == null)
            {
                await _unitOfWork.WritingScores.AddAsync(
                    new WritingScore
                    {
                        WritingSubmissionId =
                            submission.WritingSubmissionId,
                        OverallBand = aiResult.OverallBand,
                        TaskAchievement =
                            aiResult.TaskAchievement,
                        CoherenceAndCohesion =
                            aiResult.CoherenceAndCohesion,
                        LexicalResource =
                            aiResult.LexicalResource,
                        GrammaticalRangeAndAccuracy =
                            aiResult.GrammaticalRangeAndAccuracy,
                        Feedback = aiResult.Feedback,
                        ImprovementSuggestionsJson =
                            JsonSerializer.Serialize(
                                aiResult.ImprovementSuggestions)
                    });
            }

            var answer = attempt.Answers
                .FirstOrDefault(x =>
                    x.WritingTaskId == submission.WritingTaskId);

            if (answer != null)
            {
                var totalWritingTaskCount = attempt.ExerciseAssignment
    .Exercise
    .WritingTasks
    .Count;

                var pointPerWritingTask =
                    totalWritingTaskCount <= 0
                        ? 10
                        : 10.0 / totalWritingTaskCount;

                var earnedPoint =
                    Math.Round(
                        aiResult.OverallBand / 10 * pointPerWritingTask,
                        2);

                answer.PointEarned = earnedPoint;
                answer.ManualScore = aiResult.OverallBand;
                answer.TeacherFeedback = aiResult.Feedback;
                answer.IsCorrect = earnedPoint > 0;
                answer.UpdatedAt = DateTime.UtcNow;
            }

            await CompleteSubmissionAsync(submission);
            return;
        }
        catch
        {
            submission.Status =
                WritingSubmissionStatus.Failed;
            submission.UpdatedAt = DateTime.UtcNow;

            attempt.Status =
                ExerciseAttemptStatus.AiGradingFailed;
            attempt.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            throw;
        }
    }

    private async Task CompleteSubmissionAsync(
        WritingSubmission submission)
    {
        submission.Status = WritingSubmissionStatus.Completed;
        submission.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        var refreshedAttempt =
            await _unitOfWork.ExerciseAttempts
                .GetByIdAsync(submission.ExerciseAttemptId);

        if (refreshedAttempt == null)
        {
            throw new Exception("Exercise attempt not found");
        }

        var allWritingSubmissions =
            await _unitOfWork.WritingSubmissions
                .Query()
                .Where(x =>
                    x.ExerciseAttemptId ==
                    submission.ExerciseAttemptId)
                .ToListAsync();

        var latestAttemptScore =
            await _unitOfWork.ExerciseAttempts
                .Query()
                .Where(x =>
                    x.ExerciseAttemptId ==
                    submission.ExerciseAttemptId)
                .Select(x => new
                {
                    Score = x.Answers.Sum(a => a.PointEarned),
                    CorrectAnswers = x.Answers.Count(a => a.IsCorrect)
                })
                .FirstAsync();

        refreshedAttempt.Score = latestAttemptScore.Score;
        refreshedAttempt.CorrectAnswers =
            latestAttemptScore.CorrectAnswers;
        refreshedAttempt.Status =
            allWritingSubmissions.All(x =>
                x.Status == WritingSubmissionStatus.Completed)
                ? ExerciseAttemptStatus.AiGraded
                : ExerciseAttemptStatus.AiGrading;
        refreshedAttempt.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        if (refreshedAttempt.Status == ExerciseAttemptStatus.AiGraded)
        {
            await _studentGamificationService.AwardExerciseSubmissionAsync(refreshedAttempt.ExerciseAttemptId);
        }
    }
}
