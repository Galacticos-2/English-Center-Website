using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Infrastructure.BackgroundJobs;
using Hangfire;

namespace EnglishCenterSystem.Infrastructure.Services;

public class CompetitionDeadlineQueueService
    : ICompetitionDeadlineQueueService
{
    private readonly IBackgroundJobClient
        _backgroundJobClient;

    public CompetitionDeadlineQueueService(
        IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient =
            backgroundJobClient;
    }

    public string ScheduleDeadline(
        int competitionTestAssignmentId,
        DateTime deadlineAtUtc)
    {
        var delay =
            deadlineAtUtc - DateTime.UtcNow;

        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        return _backgroundJobClient.Schedule<
            CompetitionDeadlineBackgroundJobService>(
                service =>
                    service.ProcessAsync(
                        competitionTestAssignmentId),
                delay);
    }

    public bool DeleteScheduledJob(
        string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return false;
        }

        return BackgroundJob.Delete(jobId);
    }
}