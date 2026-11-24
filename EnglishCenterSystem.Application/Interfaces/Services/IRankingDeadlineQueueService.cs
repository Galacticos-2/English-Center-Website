namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IRankingDeadlineQueueService
{
    string ScheduleCloseCompetition(
        int rankingCompetitionId,
        DateTime deadlineAtUtc);

    bool DeleteScheduledJob(string hangfireJobId);
}