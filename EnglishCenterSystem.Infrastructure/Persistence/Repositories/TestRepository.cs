using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class TestRepository : ITestRepository
{
    private readonly AppDbContext _context;

    public TestRepository(AppDbContext context)
    {
        _context = context;
    }
    public IQueryable<Test> QueryBasic()
    {
        return _context.Tests
            .Include(x => x.Assignments);
    }
    public IQueryable<Test> Query()
    {
        return _context.Tests
            .Include(x => x.TeacherUser)
            .Include(x => x.Assignments)
                .ThenInclude(x => x.ClassRoom)
            .Include(x => x.ListeningTasks)
                .ThenInclude(x => x.ListeningTask)
                    .ThenInclude(x => x.Questions)
                        .ThenInclude(x => x.Options)
            .Include(x => x.ReadingTasks)
                .ThenInclude(x => x.ReadingTask)
                    .ThenInclude(x => x.Questions)
                        .ThenInclude(x => x.Options)
                        .Include(x => x.ReadingTasks)
    .ThenInclude(x => x.ReadingTask)
        .ThenInclude(x =>
            x.PassageImages)
            .Include(x => x.WritingTasks)
                .ThenInclude(x => x.WritingTask)
            .Include(x => x.SpeakingTasks)
                .ThenInclude(x => x.SpeakingTask);
    }
    public async Task HardDeleteAsync(int testId, int teacherUserId)
    {
        var test = await _context.Tests
            .FirstOrDefaultAsync(x =>
                x.TestId == testId &&
                x.TeacherUserId == teacherUserId);

        if (test == null)
        {
            throw new Exception("Test not found");
        }
        
        var assignmentIds = await _context.TestAssignments
            .Where(x => x.TestId == testId)
            .Select(x => x.TestAssignmentId)
            .ToListAsync();

        var attemptIds = await _context.TestAttempts
    .Where(x =>
        assignmentIds.Contains(x.TestAssignmentId))
    .Select(x => x.TestAttemptId)
    .ToListAsync();

        await _context.TestAnswers
            .Where(x => attemptIds.Contains(x.TestAttemptId))
            .ExecuteDeleteAsync();

        await _context.TestAttempts
            .Where(x => attemptIds.Contains(x.TestAttemptId))
            .ExecuteDeleteAsync();

        await _context.TestAssignments
            .Where(x => x.TestId == testId)
            .ExecuteDeleteAsync();

        await _context.TestListeningTasks
            .Where(x => x.TestId == testId)
            .ExecuteDeleteAsync();

        await _context.TestReadingTasks
            .Where(x => x.TestId == testId)
            .ExecuteDeleteAsync();

        await _context.TestWritingTasks
            .Where(x => x.TestId == testId)
            .ExecuteDeleteAsync();

        await _context.TestSpeakingTasks
            .Where(x => x.TestId == testId)
            .ExecuteDeleteAsync();

        await _context.Tests
            .Where(x =>
                x.TestId == testId &&
                x.TeacherUserId == teacherUserId)
            .ExecuteDeleteAsync();
    }
    public async Task<Test?> GetForUpdateAsync(
    int testId,
    int teacherUserId)
    {
        return await _context.Tests
            .Include(x => x.Assignments)
            .FirstOrDefaultAsync(x =>
                x.TestId == testId &&
                x.TeacherUserId == teacherUserId);
    }
    public async Task<Test?> GetByIdAsync(int testId)
    {
        return await Query()
            .FirstOrDefaultAsync(x => x.TestId == testId);
    }

    public async Task AddAsync(Test test)
    {
        await _context.Tests.AddAsync(test);
    }

    public void Remove(Test test)
    {
        test.IsDeleted = true;
        test.IsActive = false;
        test.UpdatedAt = DateTime.UtcNow;
    }
}