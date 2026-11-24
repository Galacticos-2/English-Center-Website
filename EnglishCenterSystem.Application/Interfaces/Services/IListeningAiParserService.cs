using EnglishCenterSystem.Application.Features.Listening.DTOs;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IListeningAiParserService
{
    Task<AiParsedListeningExerciseDto> ParseListeningQuestionsAsync(
        string rawText,
        int expectedQuestionCount);

    Task<AiParsedListeningExerciseDto> ParseListeningQuestionBlocksAsync(
        List<ListeningQuestionBlockDto> failedBlocks);

    Task<AiParsedListeningExerciseDto> ParseListeningQuestionBlockGroupsAsync(
        List<List<ListeningQuestionBlockDto>> blockGroups);
}
