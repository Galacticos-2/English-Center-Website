using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.API.Requests;

public class ImportSpeakingExerciseRequest
{
    public IFormFile QuestionFile { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public CourseType CourseType { get; set; }

    

    public int ExerciseSectionId { get; set; }

    public int ClassRoomId { get; set; }

  

    public int MaxAttempts { get; set; } = 1;

    public DateTime? DeadlineAt { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public int? SpeakingTimeSeconds { get; set; }
}
