namespace EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

public class NotificationPagedResultDto
{
    public List<NotificationDto> Items { get; set; }
        = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public int UnreadCount { get; set; }

    public bool HasPreviousPage =>
        Page > 1;

    public bool HasNextPage =>
        Page < TotalPages;
}