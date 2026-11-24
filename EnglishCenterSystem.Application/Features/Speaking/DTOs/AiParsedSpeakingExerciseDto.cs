namespace EnglishCenterSystem.Application.Features.Speaking.DTOs;

public class AiParsedSpeakingExerciseDto
{
    public List<AiParsedSpeakingTaskDto> Tasks { get; set; } = new();
}

public class AiParsedSpeakingTaskDto
{
    public int TaskNumber { get; set; }

    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string PromptText { get; set; } = null!;

    public string? Rubric { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public int? SpeakingTimeSeconds { get; set; }
}