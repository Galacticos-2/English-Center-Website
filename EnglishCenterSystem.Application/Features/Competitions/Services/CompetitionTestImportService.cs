using System.Text;
using EnglishCenterSystem.Application.Features.Competitions.DTOs;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Imports.Services;
using EnglishCenterSystem.Application.Features.Listening.Services;
using EnglishCenterSystem.Application.Features.Reading.Services;
using EnglishCenterSystem.Application.Features.Speaking.Services;
using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Application.Features.Writing.Services;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Domain.Entities.Listening;
using EnglishCenterSystem.Domain.Entities.Reading;
using EnglishCenterSystem.Domain.Entities.Speaking;
using EnglishCenterSystem.Domain.Entities.Writing;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Application.Common.Enums;
using EnglishCenterSystem.Application.Common.Helpers;

namespace EnglishCenterSystem.Application.Features.Competitions.Services;

public class CompetitionTestImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinioService _minioService;
    private readonly IExerciseDocumentParserService
        _documentParserService;
    private readonly ITestDocumentSplitService
        _testDocumentSplitService;
    private readonly ListeningCoreImportService
        _listeningCoreImportService;
    private readonly ReadingCoreImportService
        _readingCoreImportService;
    private readonly WritingCoreImportService
        _writingCoreImportService;
    private readonly SpeakingCoreImportService
        _speakingCoreImportService;
    private readonly ImportPreflightService
        _importPreflightService;
    private readonly IQuestionExplanationService
        _questionExplanationService;

    public CompetitionTestImportService(
        IUnitOfWork unitOfWork,
        IMinioService minioService,
        IExerciseDocumentParserService
            documentParserService,
        ITestDocumentSplitService
            testDocumentSplitService,
        ListeningCoreImportService
            listeningCoreImportService,
        ReadingCoreImportService
            readingCoreImportService,
        WritingCoreImportService
            writingCoreImportService,
        SpeakingCoreImportService
            speakingCoreImportService,
        ImportPreflightService
            importPreflightService,
        IQuestionExplanationService
            questionExplanationService)
    {
        _unitOfWork = unitOfWork;
        _minioService = minioService;
        _documentParserService =
            documentParserService;
        _testDocumentSplitService =
            testDocumentSplitService;
        _listeningCoreImportService =
            listeningCoreImportService;
        _readingCoreImportService =
            readingCoreImportService;
        _writingCoreImportService =
            writingCoreImportService;
        _speakingCoreImportService =
            speakingCoreImportService;
        _importPreflightService =
            importPreflightService;
        _questionExplanationService =
            questionExplanationService;
    }

    public async Task<int> ImportAsync(
        int teacherUserId,
        Stream testFileStream,
        string testFileName,
        string contentType,
        string questionCountsJson,
        string? transcript,
        List<ListeningAudioUploadItem> audioFiles,
        ImportCompetitionTestDto dto)
    {
        ValidateDto(dto);
        var importCode =
    Guid.NewGuid()
        .ToString("N")[..8];

        var storageFolderName =
            $"{dto.Title}-{importCode}";
        var storageResourceFolder =
    StoragePathHelper.BuildResourceFolder(
        storageFolderName,
        "competition-set");
        var extension =
            Path.GetExtension(testFileName)
                .ToLowerInvariant();

        if (extension != ".pdf" &&
            extension != ".docx" &&
            extension != ".txt")
        {
            throw new Exception(
                "Only PDF, Word or TXT test files are allowed");
        }

        byte[] testFileBytes;

        using (var tempStream = new MemoryStream())
        {
            await testFileStream.CopyToAsync(tempStream);
            testFileBytes = tempStream.ToArray();
        }

        var safeSourceFileName =
    StoragePathHelper.SanitizeFileName(
        testFileName);

        var objectName =
            $"competitions/source/" +
            $"{storageResourceFolder}/" +
            $"{Guid.NewGuid():N}_" +
            $"{safeSourceFileName}";

        await using var uploadStream =
            new MemoryStream(testFileBytes);

        var resolvedContentType =
    ResolveDocumentContentType(
        testFileName,
        contentType);

        var sourceFileUrl =
            await _minioService.UploadFileAsync(
                uploadStream,
                objectName,
                resolvedContentType);

        await using var extractStream =
            new MemoryStream(testFileBytes);

        var rawText =
            await _documentParserService
                .ExtractTextAsync(
                    extractStream,
                    testFileName);

        var splitResult =
            await _testDocumentSplitService
                .SplitAsync(
                    rawText,
                    dto.TestType);

        var useToeicPdfObjectiveImport =
            IsToeicTestType(dto.TestType) &&
            extension == ".pdf";

        var useToeicReadingFallback =
            useToeicPdfObjectiveImport &&
            string.IsNullOrWhiteSpace(splitResult.ReadingText);

        if (useToeicReadingFallback)
        {
            splitResult.ReadingText =
                "TOEIC Reading section is image-only in the source PDF.";
        }

        await _importPreflightService
            .ValidateCompetitionTestImportAsync(
                splitResult,
                dto.TestType,
                extension,
                testFileBytes,
                audioFiles,
                questionCountsJson);

        ValidateRequiredSections(
            splitResult,
            dto.TestType);

        ValidateListeningInputs(
            splitResult,
            audioFiles,
            questionCountsJson);

        var listeningTasks =
            new List<ListeningTask>();

        var readingTasks =
            new List<ReadingTask>();

        var writingTasks =
            new List<WritingTask>();

        var speakingTasks =
            new List<SpeakingTask>();

        if (!string.IsNullOrWhiteSpace(
            splitResult.ListeningText))
        {
            using var listeningStream =
                CreateTextStream(
                    splitResult.ListeningText);

            listeningTasks =
    await _listeningCoreImportService
        .CreateListeningTasksAsync(
            teacherUserId,
            ImportStorageModule.Competition,
            storageFolderName,
            dto.Title,
            dto.Description,
            transcript,
            dto.CourseType,
            audioFiles,
            listeningStream,
            $"{SafeFileName(dto.Title)}_listening.txt",
            questionCountsJson,
            IsToeicTestType(dto.TestType),
            testFileBytes,
            testFileName,
            splitResult.ListeningText);
        }

        if (!string.IsNullOrWhiteSpace(
            splitResult.ReadingText))
        {
            using var readingStream =
                CreateTextStream(
                    splitResult.ReadingText);

            var expectedReadingQuestionCount =
                useToeicPdfObjectiveImport
                    ? null
                    : ReadingQuestionCountHelper
                        .DetectExpectedQuestionCount(
                            splitResult.ReadingText);

            readingTasks =
                await _readingCoreImportService
                    .CreateReadingTasksAsync(
                        teacherUserId,
                        dto.Title,
                        dto.Description,
                        dto.CourseType,
                        readingStream,
                        $"{SafeFileName(dto.Title)}_reading.txt",
                        expectedReadingQuestionCount);
        }

        if (dto.GenerateExplanations)
        {
            foreach (var listeningTask in listeningTasks)
            {
                await _questionExplanationService
                    .AddListeningExplanationsAsync(listeningTask);
            }

            foreach (var readingTask in readingTasks)
            {
                await _questionExplanationService
                    .AddReadingExplanationsAsync(readingTask);
            }
        }

        if (!string.IsNullOrWhiteSpace(
            splitResult.WritingText))
        {
            using var writingStream =
                CreateTextStream(
                    splitResult.WritingText);

            writingTasks =
                await _writingCoreImportService
                    .CreateWritingTasksAsync(
                        teacherUserId,
                        dto.CourseType,
                        writingStream,
                        $"{SafeFileName(dto.Title)}_writing.txt",
                        testFileBytes,
                        testFileName,
                        dto.TestType == TestType.ToeicFourSkills
                            ? 8
                            : null);
        }

        if (!string.IsNullOrWhiteSpace(
            splitResult.SpeakingText))
        {
            using var speakingStream =
                CreateTextStream(
                    splitResult.SpeakingText);

            speakingTasks =
    await _speakingCoreImportService
        .CreateSpeakingTasksAsync(
            teacherUserId,
            ImportStorageModule.Competition,
            storageFolderName,
            speakingStream,
            $"{SafeFileName(dto.Title)}_speaking.txt",
            "text/plain",
            dto.Description,
            dto.CourseType,
            60,
            720,
            dto.TestType == TestType.ToeicFourSkills
                ? 11
                : null,
            testFileBytes,
            testFileName,
            resolvedContentType);
        }

        ValidateParsedTasks(
            dto.TestType,
            listeningTasks.Count,
            listeningTasks.Sum(x => x.Questions.Count),
            readingTasks.Count,
            readingTasks.Sum(x => x.Questions.Count),
            writingTasks,
            speakingTasks.Count);

        var competitionTest =
            new CompetitionTest
            {
                TeacherUserId = teacherUserId,
                Title = dto.Title.Trim(),
                Description =
                    string.IsNullOrWhiteSpace(
                        dto.Description)
                        ? null
                        : dto.Description.Trim(),
                TestType = dto.TestType,
                CourseType = dto.CourseType,
                SourceFileUrl = sourceFileUrl,
                SourceFileObjectName = objectName,
                TotalPoints = 10,
                IsActive = true,

                ListeningTasks = listeningTasks
                    .Select((task, index) =>
                        new CompetitionTestListeningTask
                        {
                            ListeningTask = task,
                            OrderIndex = index + 1,
                            Point =
                                listeningTasks.Count == 0
                                    ? 0
                                    : Math.Round(
                                        GetSkillTotalPoint(
                                            dto.TestType,
                                            SkillType.Listening)
                                        / listeningTasks.Count,
                                        2)
                        })
                    .ToList(),

                ReadingTasks = readingTasks
                    .Select((task, index) =>
                        new CompetitionTestReadingTask
                        {
                            ReadingTask = task,
                            OrderIndex = index + 1,
                            Point =
                                readingTasks.Count == 0
                                    ? 0
                                    : Math.Round(
                                        GetSkillTotalPoint(
                                            dto.TestType,
                                            SkillType.Reading)
                                        / readingTasks.Count,
                                        2)
                        })
                    .ToList(),

                WritingTasks = writingTasks
                    .Select((task, index) =>
                        new CompetitionTestWritingTask
                        {
                            WritingTask = task,
                            OrderIndex = index + 1,
                            Point =
                                writingTasks.Count == 0
                                    ? 0
                                    : Math.Round(
                                        GetSkillTotalPoint(
                                            dto.TestType,
                                            SkillType.Writing)
                                        / writingTasks.Count,
                                        2)
                        })
                    .ToList(),

                SpeakingTasks = speakingTasks
                    .Select((task, index) =>
                        new CompetitionTestSpeakingTask
                        {
                            SpeakingTask = task,
                            OrderIndex = index + 1,
                            Point =
                                speakingTasks.Count == 0
                                    ? 0
                                    : Math.Round(
                                        GetSkillTotalPoint(
                                            dto.TestType,
                                            SkillType.Speaking)
                                        / speakingTasks.Count,
                                        2)
                        })
                    .ToList()
            };

        await _unitOfWork.CompetitionTests
            .AddAsync(competitionTest);

        await _unitOfWork.SaveChangesAsync();

        return competitionTest.CompetitionTestId;
    }
    private static string ResolveDocumentContentType(
    string fileName,
    string? suppliedContentType)
    {
        if (!string.IsNullOrWhiteSpace(
                suppliedContentType) &&
            suppliedContentType !=
                "application/octet-stream")
        {
            return suppliedContentType;
        }

        var extension =
            Path.GetExtension(fileName)
                .ToLowerInvariant();

        return extension switch
        {
            ".pdf" =>
                "application/pdf",

            ".docx" =>
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

            ".txt" =>
                "text/plain",

            _ =>
                "application/octet-stream"
        };
    }
    private static MemoryStream CreateTextStream(
        string text)
    {
        return new MemoryStream(
            Encoding.UTF8.GetBytes(text));
    }

    private static string SafeFileName(
        string value)
    {
        foreach (var invalidChar in
            Path.GetInvalidFileNameChars())
        {
            value = value.Replace(
                invalidChar,
                '_');
        }

        return value.Trim();
    }

    private static double GetSkillTotalPoint(
        TestType testType,
        SkillType skillType)
    {
        if (testType ==
            TestType.ToeicTwoSkills)
        {
            return skillType switch
            {
                SkillType.Listening => 5,
                SkillType.Reading => 5,
                _ => 0
            };
        }

        return skillType switch
        {
            SkillType.Listening => 2.5,
            SkillType.Reading => 2.5,
            SkillType.Writing => 2.5,
            SkillType.Speaking => 2.5,
            _ => 0
        };
    }

    private static void ValidateDto(
        ImportCompetitionTestDto dto)
    {
        if (string.IsNullOrWhiteSpace(
            dto.Title))
        {
            throw new Exception(
                "Competition test title is required");
        }

        if (!Enum.IsDefined(
            typeof(TestType),
            dto.TestType))
        {
            throw new Exception(
                "Invalid test type");
        }

        if (!Enum.IsDefined(
            typeof(CourseType),
            dto.CourseType))
        {
            throw new Exception(
                "Invalid course type");
        }
    }

    private static void ValidateListeningInputs(
        TestDocumentSplitResultDto splitResult,
        List<ListeningAudioUploadItem> audioFiles,
        string questionCountsJson)
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
                "Listening audio files are required");
        }

        if (string.IsNullOrWhiteSpace(
                questionCountsJson) ||
            questionCountsJson == "[]")
        {
            throw new Exception(
                "Listening question counts are required");
        }
    }

    private static void ValidateParsedTasks(
        TestType testType,
        int listeningTaskCount,
        int listeningQuestionCount,
        int readingTaskCount,
        int readingQuestionCount,
        IReadOnlyCollection<WritingTask> writingTasks,
        int speakingTaskCount)
    {
        var writingTaskCount = writingTasks.Count;

        if (listeningTaskCount <= 0)
        {
            throw new Exception(
                "Listening section could not be parsed");
        }

        if (readingTaskCount <= 0)
        {
            throw new Exception(
                "Reading section could not be parsed");
        }

        if (testType ==
            TestType.ToeicTwoSkills)
        {
            ValidateToeicObjectiveQuestionCounts(
                listeningQuestionCount,
                readingQuestionCount);

            return;
        }

        if (testType ==
            TestType.ToeicFourSkills)
        {
            ValidateToeicObjectiveQuestionCounts(
                listeningQuestionCount,
                readingQuestionCount);

            if (writingTaskCount != 8)
            {
                throw new Exception(
                    $"TOEIC 4 skills competition requires 8 Writing tasks but parsed {writingTaskCount}. " +
                    BuildToeicWritingParseSummary(writingTasks));
            }

            if (speakingTaskCount != 11)
            {
                throw new Exception(
                    $"TOEIC 4 skills competition requires 11 Speaking tasks but parsed {speakingTaskCount}");
            }

            return;
        }

        if (writingTaskCount <= 0)
        {
            throw new Exception(
                "Writing section could not be parsed");
        }

        if (speakingTaskCount <= 0)
        {
            throw new Exception(
                "Speaking section could not be parsed");
        }
    }

    private static string BuildToeicWritingParseSummary(
        IReadOnlyCollection<WritingTask> writingTasks)
    {
        var parsedNumbers = writingTasks
            .Select(task => task.OrderIndex)
            .Where(orderIndex =>
                orderIndex >= 1 &&
                orderIndex <= 8)
            .Distinct()
            .OrderBy(orderIndex => orderIndex)
            .ToList();

        var missingNumbers = Enumerable
            .Range(1, 8)
            .Where(number =>
                !parsedNumbers.Contains(number))
            .ToList();

        var parsedText = parsedNumbers.Any()
            ? string.Join(", ", parsedNumbers)
            : "none";

        var missingText = missingNumbers.Any()
            ? string.Join(", ", missingNumbers)
            : "none";

        return $"Parsed questions: {parsedText}. Missing questions: {missingText}.";
    }

    private static void ValidateToeicObjectiveQuestionCounts(
        int listeningQuestionCount,
        int readingQuestionCount)
    {
        if (listeningQuestionCount != 100)
        {
            throw new Exception(
                $"TOEIC test requires 100 Listening questions but parsed {listeningQuestionCount}");
        }

        if (readingQuestionCount != 100)
        {
            throw new Exception(
                $"TOEIC test requires 100 Reading questions but parsed {readingQuestionCount}");
        }
    }

    private static void ValidateRequiredSections(
        TestDocumentSplitResultDto splitResult,
        TestType testType)
    {
        if (testType ==
            TestType.ToeicTwoSkills)
        {
            if (string.IsNullOrWhiteSpace(
                splitResult.ListeningText))
            {
                throw new Exception(
                    "TOEIC 2 skills competition requires Listening section");
            }

            if (string.IsNullOrWhiteSpace(
                splitResult.ReadingText))
            {
                throw new Exception(
                    "TOEIC 2 skills competition requires Reading section");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(
            splitResult.ListeningText))
        {
            throw new Exception(
                "Four skills competition requires Listening section");
        }

        if (string.IsNullOrWhiteSpace(
            splitResult.ReadingText))
        {
            throw new Exception(
                "Four skills competition requires Reading section");
        }

        if (string.IsNullOrWhiteSpace(
            splitResult.WritingText))
        {
            throw new Exception(
                "Four skills competition requires Writing section");
        }

        if (string.IsNullOrWhiteSpace(
            splitResult.SpeakingText))
        {
            throw new Exception(
                "Four skills competition requires Speaking section");
        }
    }

    private static bool IsToeicTestType(TestType testType)
    {
        return testType == TestType.ToeicTwoSkills ||
               testType == TestType.ToeicFourSkills;
    }
}
