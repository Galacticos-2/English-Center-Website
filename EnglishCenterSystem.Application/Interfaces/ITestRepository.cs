using EnglishCenterSystem.Domain.Entities.Tests;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ITestRepository
{
    IQueryable<Test> Query();

    Task<Test?> GetByIdAsync(int testId);
    IQueryable<Test> QueryBasic();
    Task AddAsync(Test test);

    void Remove(Test test);
    Task<Test?> GetForUpdateAsync(
    int testId,
    int teacherUserId);
    Task HardDeleteAsync(int testId, int teacherUserId);
}