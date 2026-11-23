using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Infrastructure.BackgroundJobs;
using Hangfire;

namespace EnglishCenterSystem.Infrastructure.Services;

public class CompetitionAiGradingQueueService
    : ICompetitionAiGradingQueueService
{
    private readonly IBackgroundJobClient
        _backgroundJobClient;

    public CompetitionAiGradingQueueService(
        IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient =
            backgroundJobClient;
    }

    public Task EnqueueAsync(
        int competitionTestAttemptId)
    {
        _backgroundJobClient.Enqueue<
            CompetitionAiGradingBackgroundJobService>(
                service =>
                    service.ProcessAsync(
                        competitionTestAttemptId));

        return Task.CompletedTask;
    }
}