namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IToeicPdfPromptImageExtractorService
{
    Task<IReadOnlyDictionary<int, ToeicPdfPromptImage>>
        ExtractListeningPromptImagesAsync(byte[] pdfBytes);

    Task<IReadOnlyDictionary<int, ToeicPdfPromptImage>>
        ExtractWritingPromptImagesAsync(byte[] pdfBytes);

    Task<IReadOnlyDictionary<int, ToeicPdfPromptImage>>
        ExtractSpeakingPromptImagesAsync(byte[] pdfBytes);
}

public sealed record ToeicPdfPromptImage(
    byte[] Content,
    string ContentType,
    string FileExtension);
