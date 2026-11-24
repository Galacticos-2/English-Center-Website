using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Notifications;

public class Notification : BaseEntity
{
    public int NotificationId { get; set; }

    /// <summary>
    /// Người tạo ra sự kiện thông báo.
    ///
    /// Trong module hiện tại, đây thường là
    /// UserId của giáo viên.
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Lớp nhận thông báo.
    ///
    /// Để nullable nhằm hỗ trợ notification cá nhân
    /// hoặc notification toàn hệ thống trong tương lai.
    /// </summary>
    public int? ClassRoomId { get; set; }
    /// <summary>
    /// Khóa nghiệp vụ dùng để ngăn Hangfire
    /// tạo cùng một cảnh báo Admin nhiều lần.
    ///
    /// Ví dụ:
    /// AdminStudentUnassigned:25
    /// AdminClassWithoutTeacher:8
    /// </summary>
    public string? DeduplicationKey
    {
        get;
        set;
    }

    /// <summary>
    /// Loại thông báo:
    /// LessonCreated,
    /// ExerciseAssigned,
    /// TestAssigned,
    /// CompetitionAssigned.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// ID của dữ liệu liên quan.
    ///
    /// Ví dụ:
    /// LessonId,
    /// ExerciseAssignmentId,
    /// TestAssignmentId,
    /// CompetitionTestAssignmentId.
    /// </summary>
    public int? RelatedEntityId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    /// <summary>
    /// Người tạo notification.
    /// </summary>
    public User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Lớp liên quan tới notification.
    /// </summary>
    public ClassRoom? ClassRoom { get; set; }

    /// <summary>
    /// Danh sách người nhận notification.
    /// </summary>
    public ICollection<UserNotification> Recipients { get; set; }
        = new List<UserNotification>();
}