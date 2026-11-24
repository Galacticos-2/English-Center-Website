using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Reading;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class ReadingTaskRepository : IReadingTaskRepository
{
    private readonly AppDbContext _context;

    public ReadingTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<ReadingTask> Query()
    {
        return _context.ReadingTasks
            .Include(x => x.TeacherUser)
            .Include(x => x.Questions)
                .ThenInclude(x => x.Options);
    }

    public async Task<ReadingTask?> GetByIdAsync(
        int readingTaskId)
    {
        return await _context.ReadingTasks
            .Include(x => x.TeacherUser)
            .Include(x => x.Questions)
                .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x =>
                x.ReadingTaskId == readingTaskId);
    }

    public async Task AddAsync(ReadingTask readingTask)
    {
        await _context.ReadingTasks.AddAsync(readingTask);
    }

    public void Remove(ReadingTask readingTask)
    {
        readingTask.IsDeleted = true;
        readingTask.IsActive = false;
        readingTask.UpdatedAt = DateTime.UtcNow;
    }
}