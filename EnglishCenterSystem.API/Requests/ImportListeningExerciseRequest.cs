using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.API.Requests;

public class ImportListeningExerciseRequest
{
    public List<IFormFile> AudioFiles { get; set; } = new();

    public IFormFile QuestionFile { get; set; } = null!;

    public string QuestionCountsJson { get; set; } = "[]";

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Transcript { get; set; }

    public CourseType CourseType { get; set; }

   

    public int ExerciseSectionId { get; set; }

    public int ClassRoomId { get; set; }

 

    public int MaxAttempts { get; set; } = 1;

    public DateTime? DeadlineAt { get; set; }

    public bool GenerateExplanations { get; set; }
}
