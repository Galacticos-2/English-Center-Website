namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface ICompetitionAiGradingQueueService
{
    Task EnqueueAsync(
        int competitionTestAttemptId);
}