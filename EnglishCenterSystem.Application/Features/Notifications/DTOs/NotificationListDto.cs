namespace EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

public class NotificationListDto
{
    public List<NotificationDto> Items { get; set; }
        = new();

    public int UnreadCount { get; set; }

    public int TotalCount { get; set; }
}