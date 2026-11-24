namespace EnglishCenterSystem.Application.Features.Tests.DTOs.Teacher;

public class UpdateTeacherTestDto
{
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public int MaxAttempts { get; set; } = 1;

    public int DurationMinutes { get; set; } = 120;

    public DateTime? DeadlineAt { get; set; }

    public bool GenerateExplanations { get; set; }
}
