using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Notifications;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure
    .Persistence.Repositories;

public class NotificationRepository
    : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<Notification> Query()
    {
        return _context.Notifications
            .Include(x => x.CreatedByUser)
            .Include(x => x.ClassRoom)
            .Include(x => x.Recipients);
    }
    public async Task<bool>
ExistsByDeduplicationKeyAsync(
    string deduplicationKey)
    {
        return await _context.Notifications
            .AsNoTracking()
            .AnyAsync(x =>
                x.DeduplicationKey ==
                    deduplicationKey &&
                !x.IsDeleted);
    }
    public IQueryable<UserNotification>
    QueryUserNotifications()
    {
        return _context.UserNotifications;
    }
    public async Task<Notification?> GetByIdAsync(
        int notificationId)
    {
        return await Query()
            .FirstOrDefaultAsync(x =>
                x.NotificationId ==
                notificationId);
    }

    public async Task<UserNotification?>
    GetUserNotificationByIdAsync(
        int userNotificationId)
    {
        return await _context.UserNotifications
            .Include(x => x.Notification)
                .ThenInclude(x =>
                    x.CreatedByUser)
            .Include(x => x.Notification)
                .ThenInclude(x =>
                    x.ClassRoom)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.UserNotificationId ==
                userNotificationId);
    }

    public async Task AddAsync(
        Notification notification)
    {
        await _context.Notifications
            .AddAsync(notification);
    }

    public async Task AddUserNotificationsAsync(
        IEnumerable<UserNotification>
            userNotifications)
    {
        await _context.UserNotifications
            .AddRangeAsync(userNotifications);
    }

    public void Remove(
        Notification notification)
    {
        notification.IsDeleted = true;
        notification.UpdatedAt =
            DateTime.UtcNow;
    }

    public void RemoveUserNotification(
        UserNotification userNotification)
    {
        userNotification.IsDeleted = true;
        userNotification.UpdatedAt =
            DateTime.UtcNow;
    }
}