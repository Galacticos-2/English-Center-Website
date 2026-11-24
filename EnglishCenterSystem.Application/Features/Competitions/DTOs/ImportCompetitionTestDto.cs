using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Competitions.DTOs;

public class ImportCompetitionTestDto
{
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public bool GenerateExplanations { get; set; }
}
