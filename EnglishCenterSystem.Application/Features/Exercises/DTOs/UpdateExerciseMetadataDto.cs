namespace EnglishCenterSystem.Application
    .Features.Exercises.DTOs;

public class UpdateExerciseMetadataDto
{
    public string Title { get; set; } =
        string.Empty;

    public string? Description { get; set; }

    public int MaxAttempts { get; set; }

    public bool GenerateExplanations { get; set; }
}
