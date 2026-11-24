using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class TestAttemptRepository : ITestAttemptRepository
{
    private readonly AppDbContext _context;

    public TestAttemptRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<TestAttempt> Query()
    {
        return _context.TestAttempts
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.ListeningTasks)
                        .ThenInclude(x => x.ListeningTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)
                                .Include(x => x.TestAssignment)
    .ThenInclude(x => x.Test)
        .ThenInclude(x => x.ReadingTasks)
            .ThenInclude(x => x.ReadingTask)
                .ThenInclude(x =>
                    x.PassageImages)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.WritingTasks)
                        .ThenInclude(x => x.WritingTask)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.SpeakingTasks)
                        .ThenInclude(x => x.SpeakingTask)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.ClassRoom);
            






    }
    public IQueryable<TestAttempt> QueryBasic()
    {
        return _context.TestAttempts
            .AsNoTracking();
    }
    public async Task<TestAttempt?> GetTeacherAttemptDetailAsync(
    int teacherUserId,
    int testAttemptId)
    {
        return await _context.TestAttempts
            .Include(x => x.StudentRole)
                .ThenInclude(x => x.User)
            .Include(x => x.StudentRole)
                .ThenInclude(x => x.ClassRoom)

            .Include(x => x.Answers)

            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.ListeningTasks)
                        .ThenInclude(x => x.ListeningTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)

            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)
                                .Include(x => x.TestAssignment)
    .ThenInclude(x => x.Test)
        .ThenInclude(x => x.ReadingTasks)
            .ThenInclude(x => x.ReadingTask)
                .ThenInclude(x =>
                    x.PassageImages)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.WritingTasks)
                        .ThenInclude(x => x.WritingTask)

            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.SpeakingTasks)
                        .ThenInclude(x => x.SpeakingTask)

            .FirstOrDefaultAsync(x =>
                x.TestAttemptId == testAttemptId &&
                x.TestAssignment.Test.TeacherUserId == teacherUserId);
    }
    public async Task<List<TestAttempt>> GetTeacherAttemptsAsync(
    int teacherUserId,
    int testId)
    {
        return await _context.TestAttempts
            .Include(x => x.StudentRole)
                .ThenInclude(x => x.User)
            .Include(x => x.StudentRole)
                .ThenInclude(x => x.ClassRoom)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
            .Where(x =>
                x.TestAssignment.Test.TeacherUserId == teacherUserId &&
                x.TestAssignment.TestId == testId)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync();
    }
    public async Task<TestAttempt?> GetResultByIdAsync(
    int testAttemptId)
    {
        return await _context.TestAttempts
            .AsNoTracking()

            .Include(x => x.StudentRole)
                .ThenInclude(x => x.User)

            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.ListeningTasks)
                        .ThenInclude(x => x.ListeningTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)

            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)
.Include(x => x.TestAssignment)
    .ThenInclude(x => x.Test)
        .ThenInclude(x => x.ReadingTasks)
            .ThenInclude(x => x.ReadingTask)
                .ThenInclude(x =>
                    x.PassageImages)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.WritingTasks)
                        .ThenInclude(x => x.WritingTask)

            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.SpeakingTasks)
                        .ThenInclude(x => x.SpeakingTask)

            .Include(x => x.Answers)
                .ThenInclude(x => x.SelectedListeningOption)

            .Include(x => x.Answers)
                .ThenInclude(x => x.ListeningQuestion)
                    .ThenInclude(x => x.Options)

            .Include(x => x.Answers)
                .ThenInclude(x => x.ListeningQuestion)
                    .ThenInclude(x => x.ListeningTask)

            .Include(x => x.Answers)
                .ThenInclude(x => x.SelectedReadingOption)

            .Include(x => x.Answers)
                .ThenInclude(x => x.ReadingQuestion)
                    .ThenInclude(x => x.Options)

            .Include(x => x.Answers)
                .ThenInclude(x => x.ReadingQuestion)
                    .ThenInclude(x => x.ReadingTask)
                .Include(x => x.Answers)
    .ThenInclude(x => x.ReadingQuestion)
        .ThenInclude(x => x.ReadingTask)
            .ThenInclude(x => x.PassageImages)
            .Include(x => x.Answers)
                .ThenInclude(x => x.WritingTask)

            .Include(x => x.Answers)
                .ThenInclude(x => x.SpeakingTask)

            .AsSplitQuery()

            .FirstOrDefaultAsync(x =>
                x.TestAttemptId == testAttemptId &&
                !x.IsDeleted);
    }
    public async Task<TestAttempt?> GetForAiGradingAsync(int testAttemptId)
    {
        return await _context.TestAttempts
            .Include(x => x.Answers)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.WritingTasks)
                        .ThenInclude(x => x.WritingTask)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.SpeakingTasks)
                        .ThenInclude(x => x.SpeakingTask)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x =>
                x.TestAttemptId == testAttemptId &&
                !x.IsDeleted);
    }

    public async Task<TestAttempt?> GetForSubmitAsync(int testAttemptId)
    {
        return await _context.TestAttempts
            .Include(x => x.Answers)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.ListeningTasks)
                        .ThenInclude(x => x.ListeningTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.WritingTasks)
                        .ThenInclude(x => x.WritingTask)
            .Include(x => x.TestAssignment)
                .ThenInclude(x => x.Test)
                    .ThenInclude(x => x.SpeakingTasks)
                        .ThenInclude(x => x.SpeakingTask)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x =>
                x.TestAttemptId == testAttemptId &&
                !x.IsDeleted);
    }
    public async Task<TestAttempt?> GetByIdAsync(int testAttemptId)
    {
        return await Query()
            .FirstOrDefaultAsync(x => x.TestAttemptId == testAttemptId);
    }

    public async Task AddAsync(TestAttempt attempt)
    {
        await _context.TestAttempts.AddAsync(attempt);
    }
}