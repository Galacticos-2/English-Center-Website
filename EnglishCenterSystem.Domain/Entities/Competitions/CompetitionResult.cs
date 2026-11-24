using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Competitions;

public class CompetitionResult : BaseEntity
{
    public int CompetitionResultId { get; set; }

    public int CompetitionTestAssignmentId { get; set; }

    public int CompetitionTestAttemptId { get; set; }

    public int StudentRoleId { get; set; }

    public int Rank { get; set; }

    public double FinalScore { get; set; }

    public int CorrectAnswers { get; set; }

    public int TotalQuestions { get; set; }

    /// <summary>
    /// Tổng số giây từ lúc bắt đầu tới lúc nộp.
    /// Dùng để phân hạng nếu bằng điểm.
    /// </summary>
    public int CompletionSeconds { get; set; }

    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Thời điểm snapshot kết quả được tạo.
    /// </summary>
    public DateTime RankedAt { get; set; }

    public CompetitionTestAssignment Assignment { get; set; } = null!;

    public CompetitionTestAttempt Attempt { get; set; } = null!;

    public StudentRole StudentRole { get; set; } = null!;
}