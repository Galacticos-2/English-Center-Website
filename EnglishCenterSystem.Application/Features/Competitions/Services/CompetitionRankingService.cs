using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application
    .Features.Competitions.Services;

public class CompetitionRankingService
{
    private readonly IUnitOfWork
        _unitOfWork;
    private readonly IStudentGamificationService _studentGamificationService;

    public CompetitionRankingService(
        IUnitOfWork unitOfWork,
        IStudentGamificationService studentGamificationService)
    {
        _unitOfWork =
            unitOfWork;
        _studentGamificationService = studentGamificationService;
    }

    public async Task<bool> FinalizeAsync(
        int competitionTestAssignmentId)
    {
        var assignment =
            await _unitOfWork
                .CompetitionTestAssignments
                .Query()
                .FirstOrDefaultAsync(x =>
                    x.CompetitionTestAssignmentId ==
                    competitionTestAssignmentId);

        if (assignment == null)
        {
            return false;
        }

        if (assignment.Status ==
            CompetitionStatus.Cancelled)
        {
            return false;
        }

        if (assignment.Status ==
                CompetitionStatus.Ranked &&
            assignment.Results.Any())
        {
            foreach (var result in assignment.Results)
            {
                await _studentGamificationService.AwardCompetitionRankAsync(
                    result.CompetitionResultId);
            }

            return true;
        }

        var now =
            DateTime.UtcNow;

        if (now < assignment.DeadlineAt)
        {
            return false;
        }

        ExpireUnsubmittedAttempts(
            assignment,
            now);

        var hasPendingAiGrading =
            assignment.Attempts.Any(x =>
                x.Status is
                    CompetitionAttemptStatus
                        .PendingAiGrading or
                    CompetitionAttemptStatus
                        .AiGrading);

        if (hasPendingAiGrading)
        {
            assignment.Status =
                CompetitionStatus
                    .WaitingForGrading;

            assignment.UpdatedAt =
                now;

            await _unitOfWork.SaveChangesAsync();

            return false;
        }

        /*
         * Chỉ xếp hạng học sinh đã nộp bài.
         *
         * AiGradingFailed vẫn được xếp hạng
         * theo số điểm đã có, thường gồm điểm
         * Listening/Reading và phần AI nào đã chấm.
         */
        var rankableAttempts =
            assignment.Attempts
                .Where(x =>
                    x.SubmittedAt != null &&
                    x.Status is not
                        CompetitionAttemptStatus
                            .InProgress and not
                        CompetitionAttemptStatus
                            .Expired)
                .Select(x => new
                {
                    Attempt = x,

                    CompletionSeconds =
                        GetCompletionSeconds(x)
                })
                .OrderByDescending(x =>
                    x.Attempt.Score)
                .ThenByDescending(x =>
                    x.Attempt.CorrectAnswers)
                .ThenBy(x =>
                    x.CompletionSeconds)
                .ThenBy(x =>
                    x.Attempt.SubmittedAt)
                .ThenBy(x =>
                    x.Attempt.StudentRoleId)
                .ToList();

        var existingResults =
            assignment.Results.ToList();

        if (existingResults.Count > 0)
        {
            _unitOfWork.CompetitionResults
                .RemoveRange(
                    existingResults);

            await _unitOfWork.SaveChangesAsync();
        }

        var rankedAt =
            DateTime.UtcNow;

        var results =
            rankableAttempts
                .Select((item, index) =>
                    new CompetitionResult
                    {
                        CompetitionTestAssignmentId =
                            assignment
                                .CompetitionTestAssignmentId,

                        CompetitionTestAttemptId =
                            item.Attempt
                                .CompetitionTestAttemptId,

                        StudentRoleId =
                            item.Attempt.StudentRoleId,

                        Rank = rankableAttempts.Take(index).Count(x => x.Attempt.Score > item.Attempt.Score) + 1,

                        FinalScore =
                            Math.Round(
                                item.Attempt.Score,
                                2),

                        CorrectAnswers =
                            item.Attempt
                                .CorrectAnswers,

                        TotalQuestions =
                            item.Attempt
                                .TotalQuestions,

                        CompletionSeconds =
                            item.CompletionSeconds,

                        SubmittedAt =
                            item.Attempt
                                .SubmittedAt!.Value,

                        RankedAt =
                            rankedAt
                    })
                .ToList();

        if (results.Count > 0)
        {
            await _unitOfWork
                .CompetitionResults
                .AddRangeAsync(results);
        }

        assignment.Status =
            CompetitionStatus.Ranked;

        assignment.RankedAt =
            rankedAt;

        assignment.UpdatedAt =
            rankedAt;

        await _unitOfWork.SaveChangesAsync();

        foreach (var result in results)
        {
            await _studentGamificationService.AwardCompetitionRankAsync(
                result.CompetitionResultId);
        }

        return true;
    }

    private static void ExpireUnsubmittedAttempts(
        CompetitionTestAssignment assignment,
        DateTime now)
    {
        var inProgressAttempts =
            assignment.Attempts
                .Where(x =>
                    x.Status ==
                        CompetitionAttemptStatus
                            .InProgress &&
                    x.SubmittedAt == null)
                .ToList();

        foreach (var attempt in
            inProgressAttempts)
        {
            attempt.Status =
                CompetitionAttemptStatus.Expired;

            attempt.UpdatedAt =
                now;
        }
    }

    private static int GetCompletionSeconds(
        CompetitionTestAttempt attempt)
    {
        if (attempt.SubmittedAt == null)
        {
            return int.MaxValue;
        }

        var totalSeconds =
            (attempt.SubmittedAt.Value -
             attempt.StartedAt)
            .TotalSeconds;

        if (totalSeconds <= 0)
        {
            return 0;
        }

        if (totalSeconds >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return Convert.ToInt32(
            Math.Ceiling(totalSeconds));
    }
}
