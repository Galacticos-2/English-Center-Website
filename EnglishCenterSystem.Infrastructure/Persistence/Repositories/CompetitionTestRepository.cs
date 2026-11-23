using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class CompetitionTestRepository
    : ICompetitionTestRepository
{
    private readonly AppDbContext _context;

    public CompetitionTestRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<CompetitionTest> QueryBasic()
    {
        return _context.CompetitionTests
            .Include(x => x.Assignments);
    }

    public IQueryable<CompetitionTest> Query()
    {
        return _context.CompetitionTests
            .AsSplitQuery()
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
            .Include(x => x.WritingTasks)
                .ThenInclude(x => x.WritingTask)
            .Include(x => x.SpeakingTasks)
                .ThenInclude(x => x.SpeakingTask);
    }

    public async Task<CompetitionTest?> GetByIdAsync(
        int competitionTestId)
    {
        return await Query()
            .FirstOrDefaultAsync(x =>
                x.CompetitionTestId ==
                competitionTestId);
    }

    public async Task AddAsync(
        CompetitionTest competitionTest)
    {
        await _context.CompetitionTests
            .AddAsync(competitionTest);
    }

    public void Remove(
        CompetitionTest competitionTest)
    {
        competitionTest.IsDeleted = true;
        competitionTest.IsActive = false;
        competitionTest.UpdatedAt = DateTime.UtcNow;
    }

    public async Task HardDeleteAsync(
        int competitionTestId,
        int teacherUserId)
    {
        var competitionTest =
            await _context.CompetitionTests
                .FirstOrDefaultAsync(x =>
                    x.CompetitionTestId ==
                        competitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (competitionTest == null)
        {
            throw new Exception(
                "Competition test not found");
        }

        var hasAssignments =
            await _context
                .CompetitionTestAssignments
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.CompetitionTestId ==
                    competitionTestId);

        if (hasAssignments)
        {
            throw new Exception(
                "Cannot delete a competition test that has already been assigned");
        }

        await _context
            .CompetitionTestListeningTasks
            .Where(x =>
                x.CompetitionTestId ==
                competitionTestId)
            .ExecuteDeleteAsync();

        await _context
            .CompetitionTestReadingTasks
            .Where(x =>
                x.CompetitionTestId ==
                competitionTestId)
            .ExecuteDeleteAsync();

        await _context
            .CompetitionTestWritingTasks
            .Where(x =>
                x.CompetitionTestId ==
                competitionTestId)
            .ExecuteDeleteAsync();

        await _context
            .CompetitionTestSpeakingTasks
            .Where(x =>
                x.CompetitionTestId ==
                competitionTestId)
            .ExecuteDeleteAsync();

        await _context.CompetitionTests
            .Where(x =>
                x.CompetitionTestId ==
                    competitionTestId &&
                x.TeacherUserId ==
                    teacherUserId)
            .ExecuteDeleteAsync();
    }
}