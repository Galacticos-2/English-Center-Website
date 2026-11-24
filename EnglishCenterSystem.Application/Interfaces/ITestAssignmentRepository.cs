using EnglishCenterSystem.Domain.Entities.Tests;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ITestAssignmentRepository
{
    IQueryable<TestAssignment> Query();

    Task<TestAssignment?> GetByIdAsync(int testAssignmentId);

    Task AddAsync(TestAssignment assignment);
    IQueryable<TestAssignment> QueryBasic();
    Task<TestAssignment?> GetForStartAttemptAsync(
    int testAssignmentId);
    void Remove(TestAssignment assignment);
}