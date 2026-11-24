using EnglishCenterSystem.Application
    .Features.Documents.DTOs;

namespace EnglishCenterSystem.Application
    .Interfaces.Services;

public interface IExerciseDocumentParserService
{
    Task<string> ExtractTextAsync(
        Stream stream,
        string fileName);

    Task<ParsedDocumentDto>
        ExtractStructuredAsync(
            Stream stream,
            string fileName);
}