using EnglishCenterSystem.API.Hubs;
using EnglishCenterSystem.Application
    .Features.Notifications.DTOs;
using EnglishCenterSystem.Application
    .Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace EnglishCenterSystem.API.Services;

public class SignalRNotificationRealtimeService
    : INotificationRealtimeService
{
    private readonly IHubContext<NotificationHub>
        _hubContext;

    public SignalRNotificationRealtimeService(
        IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(
        int userId,
        NotificationRealtimeDto notification,
        CancellationToken cancellationToken =
            default)
    {
        await _hubContext.Clients
            .User(userId.ToString())
            .SendAsync(
                NotificationHubEvents
                    .ReceiveNotification,
                notification,
                cancellationToken);
    }

    public async Task SendUnreadCountAsync(
        int userId,
        int unreadCount,
        CancellationToken cancellationToken =
            default)
    {
        await _hubContext.Clients
            .User(userId.ToString())
            .SendAsync(
                NotificationHubEvents
                    .UnreadCountChanged,
                new
                {
                    unreadCount
                },
                cancellationToken);
    }
}