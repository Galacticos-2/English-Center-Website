using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Infrastructure.BackgroundJobs;
using Hangfire;

namespace EnglishCenterSystem.Infrastructure.Services;

public class TestAiGradingQueueService : ITestAiGradingQueueService
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public TestAiGradingQueueService(
        IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task EnqueueAsync(int testAttemptId)
    {
        _backgroundJobClient.Enqueue<TestAiGradingBackgroundJobService>(
            service => service.ProcessAsync(testAttemptId));

        return Task.CompletedTask;
    }
}