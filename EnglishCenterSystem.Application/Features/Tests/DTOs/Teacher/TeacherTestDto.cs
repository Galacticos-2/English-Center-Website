using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Tests.DTOs.Teacher;

public class TeacherTestDto
{
    public int TestId { get; set; }

    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public double TotalPoints { get; set; }

    public bool IsActive { get; set; }
    public bool HasSourceFile { get; set; }
    public int MaxAttempts { get; set; }

    public int DurationMinutes { get; set; }

    public DateTime? DeadlineAt { get; set; }

    public int AssignmentCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
