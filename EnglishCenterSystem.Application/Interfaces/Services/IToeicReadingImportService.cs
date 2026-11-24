using EnglishCenterSystem.Domain.Entities.Reading;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IToeicReadingImportService
{
    Task ValidateReadingAnswerKeyAsync(
        byte[] pdfBytes);

    Task<List<ReadingTask>> CreateReadingTasksAsync(
        int teacherUserId,
        string title,
        string? description,
        CourseType courseType,
        byte[] pdfBytes,
        string pdfFileName,
        string storagePrefix);
}
