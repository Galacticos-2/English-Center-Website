using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class CompetitionResultRepository
    : ICompetitionResultRepository
{
    private readonly AppDbContext _context;

    public CompetitionResultRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<CompetitionResult> QueryBasic()
    {
        return _context.CompetitionResults;
    }

    public IQueryable<CompetitionResult> Query()
    {
        return _context.CompetitionResults
            .Include(x => x.Assignment)
            .Include(x => x.Attempt)
                .ThenInclude(x => x.Answers)
            .Include(x => x.StudentRole)
                .ThenInclude(x => x.User);
    }

    public async Task<CompetitionResult?> GetByIdAsync(
        int competitionResultId)
    {
        return await Query()
            .FirstOrDefaultAsync(x =>
                x.CompetitionResultId ==
                competitionResultId);
    }

    public async Task AddAsync(
        CompetitionResult result)
    {
        await _context.CompetitionResults
            .AddAsync(result);
    }

    public async Task AddRangeAsync(
        IEnumerable<CompetitionResult> results)
    {
        await _context.CompetitionResults
            .AddRangeAsync(results);
    }

    public void RemoveRange(
        IEnumerable<CompetitionResult> results)
    {
        _context.CompetitionResults
            .RemoveRange(results);
    }
}
