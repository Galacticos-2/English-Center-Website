using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.API.Requests;

public class ImportTestRequest
{
    public IFormFile TestFile { get; set; } = null!;

    public List<IFormFile> AudioFiles { get; set; } = new();

    public string QuestionCountsJson { get; set; } = "[]";

    public string? Transcript { get; set; }

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
