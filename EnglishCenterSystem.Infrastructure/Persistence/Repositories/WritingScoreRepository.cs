using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Writing;
using EnglishCenterSystem.Infrastructure.Persistence.Context;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class WritingScoreRepository : IWritingScoreRepository
{
    private readonly AppDbContext _context;

    public WritingScoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<WritingScore> Query()
    {
        return _context.WritingScores;
    }

    public async Task AddAsync(WritingScore score)
    {
        await _context.WritingScores.AddAsync(score);
    }
}