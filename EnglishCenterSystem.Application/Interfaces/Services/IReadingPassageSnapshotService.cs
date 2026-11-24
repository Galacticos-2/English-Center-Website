using EnglishCenterSystem.Domain.Entities.Reading;

namespace EnglishCenterSystem.Application
    .Interfaces.Services;

public interface IReadingPassageSnapshotService
{
    Task GeneratePassageImagesAsync(
        Stream pdfStream,
        string pdfFileName,
        IReadOnlyList<ReadingTask> readingTasks,
        string storagePrefix);
}