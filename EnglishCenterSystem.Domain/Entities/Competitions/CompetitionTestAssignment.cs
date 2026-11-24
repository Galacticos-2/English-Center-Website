using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Competitions;

public class CompetitionTestAssignment : BaseEntity
{
    public int CompetitionTestAssignmentId { get; set; }

    public int CompetitionTestId { get; set; }

    public int TeacherUserId { get; set; }

    public int ClassRoomId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// Thời điểm cuộc thi bắt đầu mở.
    /// Luôn lưu UTC.
    /// </summary>
    public DateTime StartsAt { get; set; }

    /// <summary>
    /// Deadline chung của cuộc thi.
    /// Luôn lưu UTC.
    /// </summary>
    public DateTime DeadlineAt { get; set; }

    /// <summary>
    /// Thời lượng tối đa cho mỗi học sinh, tính bằng phút.
    /// Ví dụ 120 phút.
    /// </summary>
    public int DurationMinutes { get; set; }

    public CompetitionStatus Status { get; set; }
        = CompetitionStatus.Scheduled;

    /// <summary>
    /// Thời điểm bảng xếp hạng được chốt.
    /// </summary>
    public DateTime? RankedAt { get; set; }

    /// <summary>
    /// ID Hangfire job dùng xử lý deadline/ranking.
    /// </summary>
    public string? RankingJobId { get; set; }

    public bool IsActive { get; set; } = true;

    public CompetitionTest CompetitionTest { get; set; } = null!;

    public User TeacherUser { get; set; } = null!;

    public ClassRoom ClassRoom { get; set; } = null!;

    public ICollection<CompetitionTestAttempt>
        Attempts
    { get; set; }
        = new List<CompetitionTestAttempt>();

    public ICollection<CompetitionResult>
        Results
    { get; set; }
        = new List<CompetitionResult>();
}