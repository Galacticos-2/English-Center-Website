using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;

public class StudentCompetitionLeaderboardDto
{
    public int CompetitionTestAssignmentId { get; set; }

    public string CompetitionTitle { get; set; } = "";

    public string CompetitionTestTitle { get; set; } = "";

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public string ClassRoomName { get; set; } = "";

    public CompetitionStatus Status { get; set; }

    public DateTime DeadlineAt { get; set; }

    public DateTime? RankedAt { get; set; }

    public bool IsLeaderboardAvailable { get; set; }

    public int? MyRank { get; set; }

    public double? MyScore { get; set; }

    public double? MyInternalScore { get; set; }

    public List<StudentCompetitionLeaderboardItemDto>
        Rankings
    { get; set; } = new();
}

public class StudentCompetitionLeaderboardItemDto
{
    public int StudentRoleId { get; set; }

    public string StudentName { get; set; } = "";

    public string? AvatarUrl { get; set; }

    public int Rank { get; set; }

    public double FinalScore { get; set; }

    public double InternalScore { get; set; }

    public int CorrectAnswers { get; set; }

    public int TotalQuestions { get; set; }

    public int CompletionSeconds { get; set; }

    public DateTime SubmittedAt { get; set; }

    public bool IsCurrentStudent { get; set; }
}
