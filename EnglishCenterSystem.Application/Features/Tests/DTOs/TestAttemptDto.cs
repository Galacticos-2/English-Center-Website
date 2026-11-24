namespace EnglishCenterSystem.Application.Features.Tests.DTOs;

public class TestAttemptDto
{
    public int TestAttemptId { get; set; }


    public int TestAssignmentId { get; set; }

    public string TestTitle { get; set; } = null!;
    public string? SourceFileUrl { get; set; }

    public string? SourceFileType { get; set; }
    public DateTime StartedAt { get; set; }

    public int DurationMinutes { get; set; }



  

    public List<StudentTestQuestionDto> Questions { get; set; }
        = new();
}
