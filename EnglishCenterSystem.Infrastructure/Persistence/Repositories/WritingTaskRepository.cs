using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Writing;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class WritingTaskRepository : IWritingTaskRepository
{
    private readonly AppDbContext _context;

    public WritingTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<WritingTask> Query()
    {
        return _context.WritingTasks;
    }

    public async Task<WritingTask?> GetByIdAsync(int writingTaskId)
    {
        return await _context.WritingTasks
            .FirstOrDefaultAsync(x =>
                x.WritingTaskId == writingTaskId);
    }

    public async Task AddAsync(WritingTask writingTask)
    {
        await _context.WritingTasks.AddAsync(writingTask);
    }

    public void Remove(WritingTask writingTask)
    {
        writingTask.IsDeleted = true;
        writingTask.IsActive = false;
    }
}
