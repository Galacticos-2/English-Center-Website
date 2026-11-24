using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Notifications;

public class UserNotification : BaseEntity
{
    public int UserNotificationId { get; set; }

    public int NotificationId { get; set; }

    /// <summary>
    /// User nhận thông báo.
    ///
    /// Có thể là giáo viên tạo nội dung
    /// hoặc học sinh thuộc lớp được giao.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// URL điều hướng riêng cho người nhận.
    ///
    /// Teacher và Student có thể có URL khác nhau
    /// dù cùng nhận một Notification.
    /// </summary>
    public string? ActionUrl { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    public Notification Notification { get; set; } = null!;

    public User User { get; set; } = null!;
}