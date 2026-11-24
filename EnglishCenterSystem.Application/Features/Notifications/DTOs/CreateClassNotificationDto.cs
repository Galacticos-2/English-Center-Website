using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

public class CreateClassNotificationDto
{
    public int CreatedByUserId { get; set; }

    public int ClassRoomId { get; set; }

    public NotificationType Type { get; set; }

    public int? RelatedEntityId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string TeacherActionUrl { get; set; } = null!;

    public string StudentActionUrl { get; set; } = null!;
}