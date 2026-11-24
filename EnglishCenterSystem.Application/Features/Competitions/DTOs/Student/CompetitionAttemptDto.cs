namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;

public class CompetitionAttemptDto
{
    public int CompetitionTestAttemptId { get; set; }

    public int CompetitionTestAssignmentId { get; set; }

    public int CompetitionTestId { get; set; }

    public string CompetitionTitle { get; set; } = "";

    public string CompetitionTestTitle { get; set; } = "";

    public DateTime StartedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime DeadlineAt { get; set; }

    public DateTime ServerTime { get; set; }

    public int DurationMinutes { get; set; }

    public List<CompetitionQuestionDto> Questions { get; set; }
        = new();
}