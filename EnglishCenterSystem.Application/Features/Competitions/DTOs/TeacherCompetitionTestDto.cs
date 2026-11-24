using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Competitions.DTOs;

public class TeacherCompetitionTestDto
{
    public int CompetitionTestId { get; set; }

    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public double TotalPoints { get; set; }

    public bool IsActive { get; set; }

    public int AssignmentCount { get; set; }

    public DateTime CreatedAt { get; set; }
}