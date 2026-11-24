using EnglishCenterSystem.Application.Features.Speaking.DTOs;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface ISpeakingAiParserService
{
    Task<AiParsedSpeakingExerciseDto> ParseSpeakingTasksAsync(
        string rawText);
}