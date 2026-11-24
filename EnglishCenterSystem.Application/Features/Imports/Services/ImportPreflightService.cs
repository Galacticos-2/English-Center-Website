using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Imports.Services;

public class ImportPreflightService
{
    private readonly IToeicReadingImportService
        _toeicReadingImportService;

    public ImportPreflightService(
        IToeicReadingImportService toeicReadingImportService)
    {
        _toeicReadingImportService =
            toeicReadingImportService;
    }

    public void ValidateExerciseImport(
        string rawText,
        ImportAssignExerciseDto dto)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new Exception(
                "Preflight failed: exercise file has no readable text");
        }

        if (!Enum.IsDefined(typeof(CourseType), dto.CourseType))
        {
            throw new Exception(
                "Preflight failed: invalid exercise course type");
        }

        if (!Enum.IsDefined(typeof(SkillType), dto.Skill))
        {
            throw new Exception(
                "Preflight failed: invalid exercise skill type");
        }
    }

    public async Task ValidateTestImportAsync(
        TestDocumentSplitResultDto splitResult,
        TestType testType,
        string extension,
        byte[] sourceFileBytes,
        IReadOnlyList<ListeningAudioUploadItem> audioFiles,
        string questionCountsJson)
    {
        ValidateRequiredSections(
            splitResult,
            testType,
            "test");

        ValidateListeningInputs(
            splitResult,
            audioFiles,
            questionCountsJson,
            testType);

        await ValidateToeicStructureAsync(
            splitResult,
            testType,
            extension,
            sourceFileBytes,
            "test");
    }

    public async Task ValidateCompetitionTestImportAsync(
        TestDocumentSplitResultDto splitResult,
        TestType testType,
        string extension,
        byte[] sourceFileBytes,
        IReadOnlyList<ListeningAudioUploadItem> audioFiles,
        string questionCountsJson)
    {
        ValidateRequiredSections(
            splitResult,
            testType,
            "competition");

        ValidateListeningInputs(
            splitResult,
            audioFiles,
            questionCountsJson,
            testType);

        await ValidateToeicStructureAsync(
            splitResult,
            testType,
            extension,
            sourceFileBytes,
            "competition");
    }

    private async Task ValidateToeicStructureAsync(
        TestDocumentSplitResultDto splitResult,
        TestType testType,
        string extension,
        byte[] sourceFileBytes,
        string importKind)
    {
        if (!IsToeicTestType(testType))
        {
            return;
        }

        ValidateAnswerRange(
            splitResult.ListeningText,
            1,
            100,
            "TOEIC Listening");

        await ValidateToeicReadingAnswerKeyAsync(
            splitResult.ReadingText,
            extension,
            sourceFileBytes);

        if (testType != TestType.ToeicFourSkills)
        {
            return;
        }

        // Writing/Speaking marker gaps are recoverable parser errors.
        // Exact task counts are enforced after regex and AI fallback merge.
    }

    private static void ValidateRequiredSections(
        TestDocumentSplitResultDto splitResult,
        TestType testType,
        string importKind)
    {
        if (testType == TestType.ToeicTwoSkills)
        {
            if (string.IsNullOrWhiteSpace(
                    splitResult.ListeningText))
            {
                throw new Exception(
                    $"Preflight failed: TOEIC 2 skills {importKind} requires Listening section");
            }

            if (string.IsNullOrWhiteSpace(
                    splitResult.ReadingText))
            {
                throw new Exception(
                    $"Preflight failed: TOEIC 2 skills {importKind} requires Reading section");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(
                splitResult.ListeningText))
        {
            throw new Exception(
                $"Preflight failed: four skills {importKind} requires Listening section");
        }

        if (string.IsNullOrWhiteSpace(
                splitResult.ReadingText))
        {
            throw new Exception(
                $"Preflight failed: four skills {importKind} requires Reading section");
        }

        if (string.IsNullOrWhiteSpace(
                splitResult.WritingText))
        {
            throw new Exception(
                $"Preflight failed: four skills {importKind} requires Writing section");
        }

        if (string.IsNullOrWhiteSpace(
                splitResult.SpeakingText))
        {
            throw new Exception(
                $"Preflight failed: four skills {importKind} requires Speaking section");
        }
    }

    private static void ValidateListeningInputs(
        TestDocumentSplitResultDto splitResult,
        IReadOnlyList<ListeningAudioUploadItem> audioFiles,
        string questionCountsJson,
        TestType testType)
    {
        if (string.IsNullOrWhiteSpace(
                splitResult.ListeningText))
        {
            return;
        }

        if (audioFiles == null ||
            audioFiles.Count == 0)
        {
            throw new Exception(
                "Preflight failed: Listening audio files are required");
        }

        if (string.IsNullOrWhiteSpace(questionCountsJson) ||
            questionCountsJson == "[]")
        {
            throw new Exception(
                "Preflight failed: Listening question counts are required");
        }

        List<int> questionCounts;

        try
        {
            questionCounts =
                JsonSerializer.Deserialize<List<int>>(
                    questionCountsJson)
                ?? new List<int>();
        }
        catch
        {
            throw new Exception(
                "Preflight failed: Listening question counts JSON is invalid");
        }

        if (questionCounts.Count != audioFiles.Count)
        {
            throw new Exception(
                "Preflight failed: Listening question counts must match the number of audio files");
        }

        if (questionCounts.Any(x => x <= 0))
        {
            throw new Exception(
                "Preflight failed: Listening question count for each audio must be greater than 0");
        }

        if (IsToeicTestType(testType) &&
            questionCounts.Sum() != 100)
        {
            throw new Exception(
                $"Preflight failed: TOEIC Listening requires 100 questions but audio mapping sums to {questionCounts.Sum()}");
        }
    }

    private static void ValidateAnswerRange(
        string? text,
        int start,
        int end,
        string label)
    {
        var answerText = GetAnswerSection(text);

        if (string.IsNullOrWhiteSpace(answerText))
        {
            throw new Exception(
                $"Preflight failed: {label} answer key is missing");
        }

        var numbers = Regex.Matches(
                answerText,
                @"\b(\d{1,3})\s*[\.\):\-]?\s*([A-D])(?=\s|$|[A-Z][a-z])",
                RegexOptions.IgnoreCase)
            .Cast<Match>()
            .Select(match =>
                int.Parse(match.Groups[1].Value))
            .Where(number =>
                number >= start &&
                number <= end)
            .Distinct()
            .OrderBy(number => number)
            .ToList();

        var missing = Enumerable
            .Range(start, end - start + 1)
            .Where(number =>
                !numbers.Contains(number))
            .ToList();

        if (missing.Any())
        {
            throw new Exception(
                $"Preflight failed: {label} answer key is missing questions: " +
                string.Join(", ", missing));
        }
    }

    private async Task ValidateToeicReadingAnswerKeyAsync(
        string? readingText,
        string extension,
        byte[] sourceFileBytes)
    {
        var splitAnswerException =
            GetAnswerRangeException(
                readingText,
                101,
                200,
                "TOEIC Reading");

        if (!extension.Equals(
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            if (splitAnswerException != null)
            {
                throw splitAnswerException;
            }

            return;
        }

        try
        {
            await _toeicReadingImportService
                .ValidateReadingAnswerKeyAsync(
                    sourceFileBytes);

            return;
        }
        catch when (splitAnswerException == null)
        {
            return;
        }

        throw splitAnswerException;
    }

    private static Exception? GetAnswerRangeException(
        string? text,
        int start,
        int end,
        string label)
    {
        var answerText = GetAnswerSection(text);

        if (string.IsNullOrWhiteSpace(answerText))
        {
            return new Exception(
                $"Preflight failed: {label} answer key is missing");
        }

        var numbers = Regex.Matches(
                answerText,
                @"\b(\d{1,3})\s*[\.\):\-]?\s*([A-D])(?=\s|$|[A-Z][a-z])",
                RegexOptions.IgnoreCase)
            .Cast<Match>()
            .Select(match =>
                int.Parse(match.Groups[1].Value))
            .Where(number =>
                number >= start &&
                number <= end)
            .Distinct()
            .OrderBy(number => number)
            .ToList();

        var missing = Enumerable
            .Range(start, end - start + 1)
            .Where(number =>
                !numbers.Contains(number))
            .ToList();

        if (!missing.Any())
        {
            return null;
        }

        return new Exception(
            $"Preflight failed: {label} answer key is missing questions: " +
            string.Join(", ", missing));
    }

    private static string? GetAnswerSection(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = Regex.Match(
            text,
            @"(?is)\b(?:Answer\s+for\s+(?:Reading|Listening)|Answer\s+Key)\s*:\s*(.*)$");

        return match.Success
            ? match.Groups[1].Value
            : null;
    }

    private static bool IsToeicTestType(TestType testType)
    {
        return testType == TestType.ToeicTwoSkills ||
               testType == TestType.ToeicFourSkills;
    }
}
