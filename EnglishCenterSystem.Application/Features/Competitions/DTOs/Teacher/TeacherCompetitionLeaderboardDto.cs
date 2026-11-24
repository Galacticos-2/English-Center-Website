using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Teacher;

public class TeacherCompetitionLeaderboardDto
{
    public int CompetitionTestAssignmentId { get; set; }

    public string CompetitionTitle { get; set; } = "";

    public string CompetitionTestTitle { get; set; } = "";

    public string ClassRoomName { get; set; } = "";

    public DateTime StartsAt { get; set; }

    public DateTime DeadlineAt { get; set; }

    public CompetitionStatus Status { get; set; }

    public DateTime? RankedAt { get; set; }

    public bool IsLeaderboardAvailable { get; set; }

    public int TotalStudentsInClass { get; set; }

    public int ParticipantCount { get; set; }

    public int SubmittedCount { get; set; }

    public List<TeacherCompetitionLeaderboardItemDto>
        Rankings
    { get; set; } = new();
}

public class TeacherCompetitionLeaderboardItemDto
{
    public int CompetitionResultId { get; set; }

    public int CompetitionTestAttemptId { get; set; }

    public int StudentRoleId { get; set; }

    public string StudentName { get; set; } = "";

    public int Rank { get; set; }

    public double FinalScore { get; set; }

    public int CorrectAnswers { get; set; }

    public int TotalQuestions { get; set; }

    public int CompletionSeconds { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime SubmittedAt { get; set; }

    public string AttemptStatus { get; set; } = "";
}