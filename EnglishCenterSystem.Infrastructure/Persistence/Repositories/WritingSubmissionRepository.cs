using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Writing;
using EnglishCenterSystem.Infrastructure.Persistence.Context;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class WritingSubmissionRepository : IWritingSubmissionRepository
{
    private readonly AppDbContext _context;

    public WritingSubmissionRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<WritingSubmission> Query()
    {
        return _context.WritingSubmissions;
    }

    public async Task AddAsync(WritingSubmission submission)
    {
        await _context.WritingSubmissions.AddAsync(submission);
    }
}