namespace EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

public class NotificationIdsDto
{
    public List<int> UserNotificationIds
    {
        get;
        set;
    } = new();
}