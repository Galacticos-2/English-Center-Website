using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Tests.DTOs;

public class StudentTestDto
{
    public int TestAssignmentId { get; set; }

    public int TestId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public int MaxAttempts { get; set; }

    public int DurationMinutes { get; set; }

    public int AttemptCount { get; set; }

    public bool CanStart { get; set; }

    public int TotalQuestions { get; set; }

    public double TotalPoints { get; set; }

    public List<StudentTestAttemptHistoryItemDto> AttemptHistory { get; set; } =
        new();
}

public class StudentTestAttemptHistoryItemDto
{
    public int TestAttemptId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public double Score { get; set; }

    public double InternalScore { get; set; }

    public int? ToeicListeningScore { get; set; }

    public int? ToeicReadingScore { get; set; }

    public int? ToeicSpeakingScore { get; set; }

    public int? ToeicWritingScore { get; set; }

    public int? ToeicTotalScore { get; set; }

    public double? ListeningScore { get; set; }

    public double? ReadingScore { get; set; }

    public double? WritingScore { get; set; }

    public double? SpeakingScore { get; set; }

    public string Status { get; set; } = null!;
}
