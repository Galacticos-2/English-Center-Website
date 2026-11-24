using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Teacher;

public class TeacherCompetitionAssignmentOptionsDto
{
    public List<CompetitionTestOptionDto>
        CompetitionTests
    { get; set; } = new();

    public List<CompetitionClassOptionDto>
        Classes
    { get; set; } = new();

    public DateTime ServerTime { get; set; }
}

public class CompetitionTestOptionDto
{
    public int CompetitionTestId { get; set; }

    public string Title { get; set; } = "";

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public double TotalPoints { get; set; }
}

public class CompetitionClassOptionDto
{
    public int ClassRoomId { get; set; }

    public string ClassName { get; set; } = "";

    public string CourseName { get; set; } = "";

    public CourseType? CourseType { get; set; }
}