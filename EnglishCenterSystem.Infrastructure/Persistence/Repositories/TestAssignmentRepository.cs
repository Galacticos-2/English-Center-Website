using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class TestAssignmentRepository : ITestAssignmentRepository
{
    private readonly AppDbContext _context;

    public TestAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<TestAssignment> Query()
    {
        return _context.TestAssignments
            .Include(x => x.Test)
                .ThenInclude(x => x.ListeningTasks)
                    .ThenInclude(x => x.ListeningTask)
                        .ThenInclude(x => x.Questions)
                            .ThenInclude(x => x.Options)
            .Include(x => x.Test)
                .ThenInclude(x => x.ReadingTasks)
                    .ThenInclude(x => x.ReadingTask)
                        .ThenInclude(x => x.Questions)
                            .ThenInclude(x => x.Options)
            .Include(x => x.Test)
                .ThenInclude(x => x.WritingTasks)
                    .ThenInclude(x => x.WritingTask)
            .Include(x => x.Test)
                .ThenInclude(x => x.SpeakingTasks)
                    .ThenInclude(x => x.SpeakingTask)
            .Include(x => x.ClassRoom);
    }

    public async Task<TestAssignment?> GetByIdAsync(int testAssignmentId)
    {
        return await Query()
            .FirstOrDefaultAsync(x =>
                x.TestAssignmentId == testAssignmentId);
    }
    public async Task<TestAssignment?> GetForStartAttemptAsync(
    int testAssignmentId)
    {
        return await _context.TestAssignments
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x =>
                x.TestAssignmentId == testAssignmentId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Test.IsActive &&
                !x.Test.IsDeleted)
            .Include(x => x.Test)
                .ThenInclude(x => x.ListeningTasks
                    .OrderBy(link => link.OrderIndex))
                    .ThenInclude(x => x.ListeningTask)
                        .ThenInclude(x => x.Questions
                            .OrderBy(question => question.OrderIndex))
                            .ThenInclude(x => x.Options
                                .OrderBy(option => option.OrderIndex))
            .Include(x => x.Test)
                .ThenInclude(x => x.ReadingTasks
                    .OrderBy(link => link.OrderIndex))
                    .ThenInclude(x => x.ReadingTask)
                        .ThenInclude(x => x.Questions
                            .OrderBy(question => question.OrderIndex))
                            .ThenInclude(x => x.Options
                                .OrderBy(option => option.OrderIndex))
            .Include(x => x.Test)
                .ThenInclude(x => x.WritingTasks
                    .OrderBy(link => link.OrderIndex))
                    .ThenInclude(x => x.WritingTask)
            .Include(x => x.Test)
                .ThenInclude(x => x.SpeakingTasks
                    .OrderBy(link => link.OrderIndex))
                    .ThenInclude(x => x.SpeakingTask)
            .FirstOrDefaultAsync();
    }
    public async Task AddAsync(TestAssignment assignment)
    {
        await _context.TestAssignments.AddAsync(assignment);
    }
    public IQueryable<TestAssignment> QueryBasic()
    {
        return _context.TestAssignments;
    }
    public void Remove(TestAssignment assignment)
    {
        assignment.IsDeleted = true;
        assignment.IsActive = false;
        assignment.UpdatedAt = DateTime.UtcNow;
    }
}