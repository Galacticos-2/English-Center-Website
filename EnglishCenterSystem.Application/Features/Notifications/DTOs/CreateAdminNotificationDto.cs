using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

public class CreateAdminNotificationDto
{
    public int CreatedByUserId
    {
        get;
        set;
    }

    public NotificationType Type
    {
        get;
        set;
    }

    public int? RelatedEntityId
    {
        get;
        set;
    }

    public int? ClassRoomId
    {
        get;
        set;
    }

    public string DeduplicationKey
    {
        get;
        set;
    } = null!;

    public string Title
    {
        get;
        set;
    } = null!;

    public string Message
    {
        get;
        set;
    } = null!;

    public string ActionUrl
    {
        get;
        set;
    } = null!;
}