using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Teacher;

public class TeacherCompetitionAssignmentDetailDto
{
    public int CompetitionTestAssignmentId { get; set; }

    public int CompetitionTestId { get; set; }

    public string CompetitionTestTitle { get; set; } = "";

    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public int ClassRoomId { get; set; }

    public string ClassRoomName { get; set; } = "";

    public string CourseName { get; set; } = "";

    public string TeacherName { get; set; } = "";

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public double TotalPoints { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime DeadlineAt { get; set; }

    public int DurationMinutes { get; set; }

    public CompetitionStatus Status { get; set; }

    public int TotalStudentsInClass { get; set; }

    public int ParticipantCount { get; set; }

    public int SubmittedCount { get; set; }

    public int NotStartedCount { get; set; }

    public DateTime? RankedAt { get; set; }

    public string? RankingJobId { get; set; }

    public bool CanCancel { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ServerTime { get; set; }
}