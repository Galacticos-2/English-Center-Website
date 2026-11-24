using System.Globalization;
using System.Text;
using EnglishCenterSystem.Application.Features.Flashcards.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities;
using EnglishCenterSystem.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application.Features.Flashcards.Services;

public class FlashcardService(IUnitOfWork uow)
{
    public async Task<List<FlashcardDto>> GetTeacherFlashcardsAsync(int teacherUserId)
    {
        var flashcards = await uow.VocabularyFlashcards
            .Where(x => x.TeacherUserId == teacherUserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return flashcards.Select(ToDto).ToList();
    }

    public async Task<List<FlashcardDto>> GetStudentFlashcardsAsync(int studentUserId)
    {
        var classRoomId = await GetStudentClassRoomIdAsync(studentUserId);

        if (classRoomId == null)
        {
            return [];
        }

        var flashcards = await uow.VocabularyFlashcards
            .Where(x => x.ClassRoomId == classRoomId.Value)
            .ToListAsync();

        return flashcards.Select(ToDto).ToList();
    }

    public async Task<FlashcardStudyOverviewDto> GetStudentStudyProgressAsync(int studentUserId)
    {
        var classRoomId = await GetStudentClassRoomIdAsync(studentUserId);

        return new FlashcardStudyOverviewDto
        {
            Practice = await GetStudyProgressAsync(studentUserId, classRoomId, FlashcardStudyMode.Practice),
            Quiz = await GetStudyProgressAsync(studentUserId, classRoomId, FlashcardStudyMode.Quiz)
        };
    }

    public async Task<FlashcardStudySessionDto> GetStudentStudySessionAsync(
        int studentUserId,
        FlashcardStudyMode mode)
    {
        var classRoomId = await GetStudentClassRoomIdAsync(studentUserId);
        if (classRoomId == null)
        {
            return new FlashcardStudySessionDto();
        }

        var completedFlashcardIds = uow.StudentFlashcardProgresses
            .Where(x => x.StudentUserId == studentUserId && x.Mode == mode)
            .Select(x => x.VocabularyFlashcardId);

        var remainingFlashcards = await uow.VocabularyFlashcards
            .Where(x => x.ClassRoomId == classRoomId.Value && !completedFlashcardIds.Contains(x.VocabularyFlashcardId))
            .ToListAsync();

        return new FlashcardStudySessionDto
        {
            Progress = await GetStudyProgressAsync(studentUserId, classRoomId, mode),
            RemainingFlashcards = remainingFlashcards.Select(ToDto).ToList()
        };
    }

    public async Task<FlashcardStudyProgressDto> CompleteStudentFlashcardAsync(
        int studentUserId,
        FlashcardStudyMode mode,
        int flashcardId)
    {
        var classRoomId = await GetStudentClassRoomIdAsync(studentUserId);
        if (classRoomId == null)
        {
            throw new Exception("You are not assigned to a class.");
        }

        var belongsToStudentClass = await uow.VocabularyFlashcards
            .AnyAsync(x => x.VocabularyFlashcardId == flashcardId && x.ClassRoomId == classRoomId.Value);

        if (!belongsToStudentClass)
        {
            throw new Exception("Flashcard not found in your class.");
        }

        var isCompleted = await uow.StudentFlashcardProgresses
            .AnyAsync(x => x.StudentUserId == studentUserId &&
                x.VocabularyFlashcardId == flashcardId &&
                x.Mode == mode);

        if (!isCompleted)
        {
            await uow.AddStudentFlashcardProgressAsync(new StudentFlashcardProgress
            {
                StudentUserId = studentUserId,
                VocabularyFlashcardId = flashcardId,
                Mode = mode
            });
            await uow.SaveChangesAsync();
        }

        return await GetStudyProgressAsync(studentUserId, classRoomId, mode);
    }

    public async Task<FlashcardStudyProgressDto> ResetStudentStudyProgressAsync(
        int studentUserId,
        FlashcardStudyMode mode)
    {
        var classRoomId = await GetStudentClassRoomIdAsync(studentUserId);
        if (classRoomId == null)
        {
            return new FlashcardStudyProgressDto();
        }

        var classFlashcardIds = uow.VocabularyFlashcards
            .Where(x => x.ClassRoomId == classRoomId.Value)
            .Select(x => x.VocabularyFlashcardId);

        var progressRecords = await uow.StudentFlashcardProgresses
            .Where(x => x.StudentUserId == studentUserId &&
                x.Mode == mode &&
                classFlashcardIds.Contains(x.VocabularyFlashcardId))
            .ToListAsync();

        if (progressRecords.Count > 0)
        {
            uow.RemoveStudentFlashcardProgresses(progressRecords);
            await uow.SaveChangesAsync();
        }

        return await GetStudyProgressAsync(studentUserId, classRoomId, mode);
    }

    public async Task<FlashcardDto> CreateAsync(int teacherUserId, SaveFlashcardDto dto)
    {
        Validate(dto);
        await EnsureTeacherHasClassAsync(teacherUserId, dto.ClassRoomId);

        var flashcard = new VocabularyFlashcard
        {
            TeacherUserId = teacherUserId,
            ClassRoomId = dto.ClassRoomId,
            Word = dto.Word.Trim(),
            Meaning = dto.Meaning.Trim(),
            ExampleSentence = dto.ExampleSentence.Trim()
        };

        await uow.AddVocabularyFlashcardAsync(flashcard);
        await uow.SaveChangesAsync();
        return await GetTeacherFlashcardAsync(teacherUserId, flashcard.VocabularyFlashcardId);
    }

    public async Task<FlashcardDto> UpdateAsync(int teacherUserId, int flashcardId, SaveFlashcardDto dto)
    {
        Validate(dto);
        await EnsureTeacherHasClassAsync(teacherUserId, dto.ClassRoomId);

        var flashcard = await GetTeacherEntityAsync(teacherUserId, flashcardId);
        flashcard.ClassRoomId = dto.ClassRoomId;
        flashcard.Word = dto.Word.Trim();
        flashcard.Meaning = dto.Meaning.Trim();
        flashcard.ExampleSentence = dto.ExampleSentence.Trim();
        flashcard.UpdatedAt = DateTime.UtcNow;

        await uow.SaveChangesAsync();
        return await GetTeacherFlashcardAsync(teacherUserId, flashcardId);
    }

    public async Task DeleteAsync(int teacherUserId, int flashcardId)
    {
        var flashcard = await GetTeacherEntityAsync(teacherUserId, flashcardId);
        flashcard.IsDeleted = true;
        flashcard.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync();
    }

    public async Task<int> BulkDeleteAsync(int teacherUserId, BulkDeleteFlashcardsDto dto)
    {
        var flashcardIds = dto.FlashcardIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (flashcardIds.Count == 0)
        {
            throw new Exception("Select at least one flashcard to delete.");
        }

        var flashcards = await uow.VocabularyFlashcards
            .Where(x => x.TeacherUserId == teacherUserId &&
                flashcardIds.Contains(x.VocabularyFlashcardId))
            .ToListAsync();

        if (flashcards.Count != flashcardIds.Count)
        {
            throw new Exception("One or more selected flashcards were not found.");
        }

        var deletedAt = DateTime.UtcNow;
        foreach (var flashcard in flashcards)
        {
            flashcard.IsDeleted = true;
            flashcard.UpdatedAt = deletedAt;
        }

        await uow.SaveChangesAsync();
        return flashcards.Count;
    }

    public async Task<FlashcardImportPreviewDto> PreviewImportAsync(
        int teacherUserId,
        int classRoomId,
        Stream fileStream,
        string fileName)
    {
        await EnsureImportClassAccessAsync(teacherUserId, classRoomId);
        return await ReadAndValidateImportAsync(classRoomId, fileStream, fileName);
    }

    public async Task<FlashcardImportResultDto> ImportAsync(
        int teacherUserId,
        int classRoomId,
        Stream fileStream,
        string fileName)
    {
        await EnsureImportClassAccessAsync(teacherUserId, classRoomId);

        var preview = await ReadAndValidateImportAsync(classRoomId, fileStream, fileName);
        if (!preview.CanImport)
        {
            throw new Exception(preview.IsValid
                ? "The file does not contain any new flashcards to import."
                : "The flashcard file contains invalid rows. Review the preview and upload it again.");
        }

        foreach (var row in preview.PreviewRows)
        {
            await uow.AddVocabularyFlashcardAsync(new VocabularyFlashcard
            {
                TeacherUserId = teacherUserId,
                ClassRoomId = classRoomId,
                Word = row.Word,
                Meaning = row.Meaning,
                ExampleSentence = row.ExampleSentence
            });
        }

        await uow.SaveChangesAsync();
        return new FlashcardImportResultDto { CreatedCount = preview.ImportableCount };
    }

    private async Task<VocabularyFlashcard> GetTeacherEntityAsync(int teacherUserId, int flashcardId)
    {
        var flashcard = await uow.VocabularyFlashcards
            .SingleOrDefaultAsync(x => x.VocabularyFlashcardId == flashcardId && x.TeacherUserId == teacherUserId);

        return flashcard ?? throw new Exception("Flashcard not found.");
    }

    private async Task<int?> GetStudentClassRoomIdAsync(int studentUserId) => await uow.StudentRoles
        .Where(x => x.UserId == studentUserId && !x.IsDeleted)
        .Select(x => x.ClassRoomId)
        .SingleOrDefaultAsync();

    private async Task<FlashcardStudyProgressDto> GetStudyProgressAsync(
        int studentUserId,
        int? classRoomId,
        FlashcardStudyMode mode)
    {
        if (classRoomId == null)
        {
            return new FlashcardStudyProgressDto();
        }

        var classFlashcards = uow.VocabularyFlashcards
            .Where(x => x.ClassRoomId == classRoomId.Value);

        return new FlashcardStudyProgressDto
        {
            TotalCount = await classFlashcards.CountAsync(),
            CompletedCount = await uow.StudentFlashcardProgresses
                .Where(x => x.StudentUserId == studentUserId &&
                    x.Mode == mode &&
                    classFlashcards.Select(card => card.VocabularyFlashcardId)
                        .Contains(x.VocabularyFlashcardId))
                .CountAsync()
        };
    }

    private async Task<FlashcardDto> GetTeacherFlashcardAsync(int teacherUserId, int flashcardId)
    {
        var flashcard = await uow.VocabularyFlashcards
            .SingleOrDefaultAsync(x => x.VocabularyFlashcardId == flashcardId && x.TeacherUserId == teacherUserId);

        return flashcard == null ? throw new Exception("Flashcard not found.") : ToDto(flashcard);
    }

    private async Task EnsureTeacherHasClassAsync(int teacherUserId, int classRoomId)
    {
        var hasClass = await uow.TeacherRoles
            .Where(x => x.UserId == teacherUserId && !x.IsDeleted)
            .SelectMany(x => x.TeacherClasses)
            .AnyAsync(x => x.ClassRoomId == classRoomId);

        if (!hasClass)
        {
            throw new Exception("You are not assigned to this class.");
        }
    }

    private async Task EnsureImportClassAccessAsync(int teacherUserId, int classRoomId)
    {
        if (classRoomId <= 0)
        {
            throw new Exception("A class is required for the flashcard import.");
        }

        await EnsureTeacherHasClassAsync(teacherUserId, classRoomId);
    }

    private async Task<FlashcardImportPreviewDto> ReadAndValidateImportAsync(
        int classRoomId,
        Stream fileStream,
        string fileName)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Upload an Excel .xlsx file.");
        }

        if (!fileStream.CanRead)
        {
            throw new Exception("The flashcard file is empty.");
        }

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new Exception("The Excel file does not contain a worksheet.");

        var headerColumns = GetHeaderColumns(worksheet);
        var preview = new FlashcardImportPreviewDto();

        if (headerColumns.Count != 3)
        {
            preview.Errors.Add(new FlashcardImportErrorDto
            {
                RowNumber = 1,
                Message = "The first row must contain the headers: TỪ VỰNG, NGHĨA TIẾNG VIỆT, CÂU VÍ DỤ."
            });
            return preview;
        }

        var existingWords = (await uow.VocabularyFlashcards
                .Where(x => x.ClassRoomId == classRoomId)
                .Select(x => x.Word)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var importedWords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var word = worksheet.Cell(rowNumber, headerColumns["word"]).GetString().Trim();
            var meaning = worksheet.Cell(rowNumber, headerColumns["meaning"]).GetString().Trim();
            var exampleSentence = worksheet.Cell(rowNumber, headerColumns["sentence"]).GetString().Trim();

            if (string.IsNullOrWhiteSpace(word) && string.IsNullOrWhiteSpace(meaning) && string.IsNullOrWhiteSpace(exampleSentence))
            {
                continue;
            }

            preview.TotalRows++;
            var rowHasError = false;

            if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(meaning) || string.IsNullOrWhiteSpace(exampleSentence))
            {
                preview.Errors.Add(new FlashcardImportErrorDto
                {
                    RowNumber = rowNumber,
                    Message = "Word, meaning, and example sentence are all required."
                });
                rowHasError = true;
            }

            if (word.Length > 150 || meaning.Length > 500 || exampleSentence.Length > 1000)
            {
                preview.Errors.Add(new FlashcardImportErrorDto
                {
                    RowNumber = rowNumber,
                    Message = "Word, meaning, or example sentence exceeds the permitted length."
                });
                rowHasError = true;
            }

            if (rowHasError)
            {
                continue;
            }

            if (importedWords.TryGetValue(word, out var originalRow))
            {
                preview.SkippedRows.Add(new FlashcardImportSkippedRowDto
                {
                    RowNumber = rowNumber,
                    Message = $"Duplicate of row {originalRow} in this file."
                });
                continue;
            }

            if (existingWords.Contains(word))
            {
                preview.SkippedRows.Add(new FlashcardImportSkippedRowDto
                {
                    RowNumber = rowNumber,
                    Message = "Word already exists in the selected class."
                });
                continue;
            }

            importedWords.Add(word, rowNumber);
            preview.PreviewRows.Add(new FlashcardImportRowDto
            {
                RowNumber = rowNumber,
                Word = word,
                Meaning = meaning,
                ExampleSentence = exampleSentence
            });
        }

        if (preview.TotalRows == 0)
        {
            preview.Errors.Add(new FlashcardImportErrorDto
            {
                Message = "The Excel file does not contain any flashcard rows."
            });
        }

        return preview;
    }

    private static Dictionary<string, int> GetHeaderColumns(IXLWorksheet worksheet)
    {
        var headerColumns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastColumn = worksheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (var column = 1; column <= lastColumn; column++)
        {
            var normalizedHeader = NormalizeHeader(worksheet.Cell(1, column).GetString());
            var fieldName = normalizedHeader switch
            {
                "TUVUNG" or "WORD" or "VOCABULARY" => "word",
                "NGHIATIENGVIET" or "MEANING" => "meaning",
                "CAUVIDU" or "EXAMPLESENTENCE" or "SENTENCE" => "sentence",
                _ => null
            };

            if (fieldName != null && !headerColumns.TryAdd(fieldName, column))
            {
                return [];
            }
        }

        return headerColumns;
    }

    private static string NormalizeHeader(string header)
    {
        var normalized = header.Normalize(NormalizationForm.FormD);
        return string.Concat(normalized.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark))
            .Replace("Đ", "D", StringComparison.OrdinalIgnoreCase)
            .Where(char.IsLetterOrDigit)
            .Aggregate(string.Empty, (current, character) => current + char.ToUpperInvariant(character));
    }

    private static void Validate(SaveFlashcardDto dto)
    {
        if (dto.ClassRoomId <= 0 || string.IsNullOrWhiteSpace(dto.Word) ||
            string.IsNullOrWhiteSpace(dto.Meaning) || string.IsNullOrWhiteSpace(dto.ExampleSentence))
        {
            throw new Exception("Class, word, meaning, and example sentence are required.");
        }

        if (dto.Word.Trim().Length > 150 || dto.Meaning.Trim().Length > 500 || dto.ExampleSentence.Trim().Length > 1000)
        {
            throw new Exception("Flashcard content is too long.");
        }
    }

    private static FlashcardDto ToDto(VocabularyFlashcard flashcard) => new()
    {
        VocabularyFlashcardId = flashcard.VocabularyFlashcardId,
        ClassRoomId = flashcard.ClassRoomId,
        ClassRoomName = flashcard.ClassRoom.ClassName,
        Word = flashcard.Word,
        Meaning = flashcard.Meaning,
        ExampleSentence = flashcard.ExampleSentence
    };
}
