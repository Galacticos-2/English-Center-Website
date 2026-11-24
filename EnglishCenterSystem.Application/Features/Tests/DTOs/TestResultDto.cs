namespace EnglishCenterSystem.Application.Features.Tests.DTOs;

public class TestResultDto
{
    public int TestAttemptId { get; set; }

    public string TestTitle { get; set; } = null!;

    public EnglishCenterSystem.Domain.Enums.TestType TestType { get; set; }

    public EnglishCenterSystem.Domain.Enums.CourseType CourseType { get; set; }

    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public double TotalPoints { get; set; }
    public string? SourceFileUrl { get; set; }

    public string? SourceFileType { get; set; }
    public double Score { get; set; }

    public double InternalScore { get; set; }

    public int? ToeicListeningScore { get; set; }

    public int? ToeicReadingScore { get; set; }

    public int? ToeicSpeakingScore { get; set; }

    public int? ToeicWritingScore { get; set; }

    public int? ToeicTotalScore { get; set; }

    public string Status { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime SubmittedAt { get; set; }

    public List<TestAnswerResultDto> Answers { get; set; } = new();
}
