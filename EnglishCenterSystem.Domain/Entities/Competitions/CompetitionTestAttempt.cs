using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Competitions;

public class CompetitionTestAttempt : BaseEntity
{
    public int CompetitionTestAttemptId { get; set; }

    public int CompetitionTestAssignmentId { get; set; }

    public int StudentRoleId { get; set; }

    public int AttemptNumber { get; set; } = 1;

    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Thời gian hết hạn riêng của lượt làm.
    ///
    /// ExpiresAt =
    /// min(StartedAt + DurationMinutes, DeadlineAt)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public CompetitionAttemptStatus Status { get; set; }
        = CompetitionAttemptStatus.InProgress;

    public double Score { get; set; }

    public double TotalPoints { get; set; } = 10;

    public int CorrectAnswers { get; set; }

    public int TotalQuestions { get; set; }

    public CompetitionTestAssignment Assignment { get; set; } = null!;

    public StudentRole StudentRole { get; set; } = null!;

    public ICollection<CompetitionTestAnswer>
        Answers
    { get; set; }
        = new List<CompetitionTestAnswer>();

    public CompetitionResult? Result { get; set; }
}