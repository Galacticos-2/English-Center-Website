using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;

public class CompetitionAttemptResultDto
{
    public int CompetitionTestAssignmentId { get; set; }

    public int CompetitionTestAttemptId { get; set; }

    public string CompetitionTitle { get; set; } = "";

    public string CompetitionTestTitle { get; set; } = "";

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public double TotalPoints { get; set; }

    public double Score { get; set; }

    public double InternalScore { get; set; }

    public int? ToeicListeningScore { get; set; }

    public int? ToeicReadingScore { get; set; }

    public int? ToeicSpeakingScore { get; set; }

    public int? ToeicWritingScore { get; set; }

    public int? ToeicTotalScore { get; set; }

    public CompetitionAttemptStatus Status { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime DeadlineAt { get; set; }

    public bool IsLeaderboardAvailable { get; set; }

    public int? Rank { get; set; }

    public List<CompetitionAnswerResultDto> Answers { get; set; }
        = new();
}
