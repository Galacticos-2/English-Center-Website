using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;

public class StudentCompetitionDto
{
    public int CompetitionTestAssignmentId { get; set; }

    public int CompetitionTestId { get; set; }

    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public string CompetitionTestTitle { get; set; } = "";

    public string ClassRoomName { get; set; } = "";

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime DeadlineAt { get; set; }

    public int DurationMinutes { get; set; }

    public CompetitionStatus Status { get; set; }

    public bool HasAttempt { get; set; }

    public int? CompetitionTestAttemptId { get; set; }

    public DateTime? AttemptStartedAt { get; set; }

    public DateTime? AttemptExpiresAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public CompetitionAttemptStatus? AttemptStatus { get; set; }

    public bool CanStart { get; set; }

    public bool CanContinue { get; set; }

    public bool CanViewResult { get; set; }

    public double? Score { get; set; }

    public double? InternalScore { get; set; }

    public int? ToeicListeningScore { get; set; }

    public int? ToeicReadingScore { get; set; }

    public int? ToeicSpeakingScore { get; set; }

    public int? ToeicWritingScore { get; set; }

    public int? ToeicTotalScore { get; set; }

    public int TotalQuestions { get; set; }

    public double TotalPoints { get; set; }

    public DateTime ServerTime { get; set; }
}
