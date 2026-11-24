using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

public class NotificationQueryDto
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }

    public bool? IsRead { get; set; }

    public NotificationType? Type { get; set; }
}