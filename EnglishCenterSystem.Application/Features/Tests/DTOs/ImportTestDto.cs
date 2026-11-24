using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Tests.DTOs;

public class ImportTestDto
{
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public int ClassRoomId { get; set; }

    public int MaxAttempts { get; set; } = 1;

    public int DurationMinutes { get; set; } = 120;

    public DateTime? DeadlineAt { get; set; }

    public bool GenerateExplanations { get; set; }
}
