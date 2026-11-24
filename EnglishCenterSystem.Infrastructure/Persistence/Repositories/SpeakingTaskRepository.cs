using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Speaking;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class SpeakingTaskRepository : ISpeakingTaskRepository
{
    private readonly AppDbContext _context;

    public SpeakingTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<SpeakingTask> Query()
    {
        return _context.SpeakingTasks
            .Include(x => x.TeacherUser);
    }

    public async Task<SpeakingTask?> GetByIdAsync(int speakingTaskId)
    {
        return await _context.SpeakingTasks
            .Include(x => x.TeacherUser)
            .FirstOrDefaultAsync(x => x.SpeakingTaskId == speakingTaskId);
    }

    public async Task AddAsync(SpeakingTask speakingTask)
    {
        await _context.SpeakingTasks.AddAsync(speakingTask);
    }

    public void Remove(SpeakingTask speakingTask)
    {
        speakingTask.IsDeleted = true;
        speakingTask.IsActive = false;
        speakingTask.UpdatedAt = DateTime.UtcNow;
    }
}
