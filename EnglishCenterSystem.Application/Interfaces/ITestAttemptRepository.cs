using EnglishCenterSystem.Domain.Entities.Tests;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ITestAttemptRepository
{
    IQueryable<TestAttempt> Query();
    IQueryable<TestAttempt> QueryBasic();
    Task<TestAttempt?> GetByIdAsync(int testAttemptId);
    Task<TestAttempt?> GetForAiGradingAsync(int testAttemptId);
    Task<TestAttempt?> GetForSubmitAsync(int testAttemptId);
    Task AddAsync(TestAttempt attempt);
    Task<List<TestAttempt>> GetTeacherAttemptsAsync(
    int teacherUserId,
    int testId);
    Task<TestAttempt?> GetResultByIdAsync(int testAttemptId);
    Task<TestAttempt?> GetTeacherAttemptDetailAsync(
        int teacherUserId,
        int testAttemptId);
}