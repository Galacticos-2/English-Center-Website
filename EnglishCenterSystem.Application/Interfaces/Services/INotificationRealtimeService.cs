using EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

namespace EnglishCenterSystem.Application
    .Interfaces.Services;

public interface INotificationRealtimeService
{
    Task SendNotificationAsync(
        int userId,
        NotificationRealtimeDto notification,
        CancellationToken cancellationToken =
            default);

    Task SendUnreadCountAsync(
        int userId,
        int unreadCount,
        CancellationToken cancellationToken =
            default);
}