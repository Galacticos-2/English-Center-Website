using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

public class NotificationRealtimeDto
{
    public int UserNotificationId { get; set; }

    public int NotificationId { get; set; }

    public NotificationType Type { get; set; }

    public string TypeName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? ActionUrl { get; set; }

    public int? RelatedEntityId { get; set; }

    public int? ClassRoomId { get; set; }

    public string? ClassRoomName { get; set; }

    public int CreatedByUserId { get; set; }

    public string CreatedByName { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }
}