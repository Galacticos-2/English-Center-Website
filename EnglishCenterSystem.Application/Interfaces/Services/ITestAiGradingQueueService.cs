namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface ITestAiGradingQueueService
{
    Task EnqueueAsync(int testAttemptId);
}