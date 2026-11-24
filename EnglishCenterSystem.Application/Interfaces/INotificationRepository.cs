using EnglishCenterSystem.Domain.Entities.Notifications;

namespace EnglishCenterSystem.Application.Interfaces;

public interface INotificationRepository
{
    IQueryable<Notification> Query();

    IQueryable<UserNotification> QueryUserNotifications();

    Task<Notification?> GetByIdAsync(
        int notificationId);

    Task<UserNotification?> GetUserNotificationByIdAsync(
        int userNotificationId);

    Task AddAsync(
        Notification notification);
    Task<bool> ExistsByDeduplicationKeyAsync(
    string deduplicationKey);
    Task AddUserNotificationsAsync(
        IEnumerable<UserNotification> userNotifications);

    void Remove(
        Notification notification);

    void RemoveUserNotification(
        UserNotification userNotification);
}