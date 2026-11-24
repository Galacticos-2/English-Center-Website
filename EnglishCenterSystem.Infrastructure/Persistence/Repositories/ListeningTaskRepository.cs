using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Listening;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class ListeningTaskRepository : IListeningTaskRepository
{
    private readonly AppDbContext _context;

    public ListeningTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<ListeningTask> Query()
    {
        return _context.ListeningTasks
            .Include(x => x.TeacherUser)
            .Include(x => x.Questions)
                .ThenInclude(x => x.Options);
    }

    public async Task<ListeningTask?> GetByIdAsync(
        int listeningTaskId)
    {
        return await _context.ListeningTasks
            .Include(x => x.TeacherUser)
            .Include(x => x.Questions)
                .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x =>
                x.ListeningTaskId == listeningTaskId);
    }

    public async Task AddAsync(ListeningTask listeningTask)
    {
        await _context.ListeningTasks.AddAsync(listeningTask);
    }

    public void Remove(ListeningTask listeningTask)
    {
        listeningTask.IsDeleted = true;
        listeningTask.IsActive = false;
        listeningTask.UpdatedAt = DateTime.UtcNow;
    }
}