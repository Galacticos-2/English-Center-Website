using EnglishCenterSystem.Application
    .Features.Competitions.Services;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.BackgroundJobs;

public class CompetitionDeadlineBackgroundJobService
{
    private readonly IUnitOfWork
        _unitOfWork;

    private readonly CompetitionRankingService
        _competitionRankingService;

    private readonly ICompetitionDeadlineQueueService
        _competitionDeadlineQueueService;

    public CompetitionDeadlineBackgroundJobService(
        IUnitOfWork unitOfWork,
        CompetitionRankingService
            competitionRankingService,
        ICompetitionDeadlineQueueService
            competitionDeadlineQueueService)
    {
        _unitOfWork =
            unitOfWork;

        _competitionRankingService =
            competitionRankingService;

        _competitionDeadlineQueueService =
            competitionDeadlineQueueService;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ProcessAsync(
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
            return;
        }

        if (assignment.Status is
            CompetitionStatus.Cancelled or
            CompetitionStatus.Ranked)
        {
            return;
        }

        var now =
            DateTime.UtcNow;

        /*
         * Nếu Hangfire chạy sớm do lệch clock,
         * schedule lại đúng deadline.
         */
        if (now < assignment.DeadlineAt)
        {
            var newJobId =
                _competitionDeadlineQueueService
                    .ScheduleDeadline(
                        assignment
                            .CompetitionTestAssignmentId,
                        assignment.DeadlineAt);

            assignment.RankingJobId =
                newJobId;

            assignment.UpdatedAt =
                now;

            await _unitOfWork.SaveChangesAsync();

            return;
        }

        var finalized =
            await _competitionRankingService
                .FinalizeAsync(
                    assignment
                        .CompetitionTestAssignmentId);

        if (finalized)
        {
            return;
        }

        /*
         * Finalize trả false thường vì vẫn còn
         * bài PendingAiGrading hoặc AiGrading.
         *
         * Schedule kiểm tra lại sau 1 phút.
         */
        var retryAt =
            DateTime.UtcNow.AddMinutes(1);

        var retryJobId =
            _competitionDeadlineQueueService
                .ScheduleDeadline(
                    assignment
                        .CompetitionTestAssignmentId,
                    retryAt);

        assignment.Status =
            CompetitionStatus
                .WaitingForGrading;

        assignment.RankingJobId =
            retryJobId;

        assignment.UpdatedAt =
            DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
    }
}