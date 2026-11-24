using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Listening.DTOs;
using EnglishCenterSystem.Application.Features.Reading.DTOs;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Entities.Listening;
using EnglishCenterSystem.Domain.Entities.Reading;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IQuestionExplanationService
{
    Task AddReadingExplanationsAsync(
        AiParsedReadingTaskDto passage);

    Task AddListeningExplanationsAsync(
        string? transcript,
        List<AiParsedListeningQuestionDto> questions);

    Task AddExerciseExplanationsAsync(
        List<AiParsedQuestionDto> questions);

    Task AddReadingExplanationsAsync(ReadingTask task);

    Task AddListeningExplanationsAsync(ListeningTask task);

    Task AddExerciseExplanationsAsync(
        IEnumerable<Question> questions);
}
