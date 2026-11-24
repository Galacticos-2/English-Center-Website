namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface ICompetitionDeadlineQueueService
{
    string ScheduleDeadline(
        int competitionTestAssignmentId,
        DateTime deadlineAtUtc);

    bool DeleteScheduledJob(
        string jobId);
}