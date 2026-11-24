using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Imports.Services;
using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Domain.Entities.Writing;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text;

using EnglishCenterSystem.Application.Features.Listening.Services;
using EnglishCenterSystem.Application.Features.Reading.Services;
using EnglishCenterSystem.Application.Features.Speaking.Services;
using EnglishCenterSystem.Application.Features.Writing.Services;
using EnglishCenterSystem.Application
    .Features.Notifications.DTOs;
using EnglishCenterSystem.Application.Common.Enums;

using EnglishCenterSystem.Application
    .Features.Notifications.Services;

using Microsoft.Extensions.Logging;
namespace EnglishCenterSystem.Application.Features.Tests.Services;

public class TestImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinioService _minioService;
    private readonly IExerciseDocumentParserService _documentParserService;
    private readonly ITestDocumentSplitService _testDocumentSplitService;
    private readonly ListeningCoreImportService _listeningCoreImportService;
    private readonly
    IReadingPassageSnapshotService
    _readingPassageSnapshotService;
    private readonly ReadingCoreImportService _readingCoreImportService;
    private readonly WritingCoreImportService _writingCoreImportService;
    private readonly SpeakingCoreImportService _speakingCoreImportService;
    private readonly NotificationService
    _notificationService;
    private readonly ImportPreflightService
        _importPreflightService;
    private readonly IQuestionExplanationService
        _questionExplanationService;

    private readonly ILogger<TestImportService>
        _logger;
    public TestImportService(
    IUnitOfWork unitOfWork,
    IMinioService minioService,
    IExerciseDocumentParserService
        documentParserService,
    ITestDocumentSplitService
        testDocumentSplitService,
    IReadingPassageSnapshotService
    readingPassageSnapshotService,
    ListeningCoreImportService
        listeningCoreImportService,
    ReadingCoreImportService
        readingCoreImportService,
    WritingCoreImportService
        writingCoreImportService,
    SpeakingCoreImportService
        speakingCoreImportService,
    NotificationService
        notificationService,
    ImportPreflightService
        importPreflightService,
    IQuestionExplanationService
        questionExplanationService,
    ILogger<TestImportService> logger)
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
        _readingPassageSnapshotService =
    readingPassageSnapshotService;
        _speakingCoreImportService =
            speakingCoreImportService;
        _notificationService =
            notificationService;
        _importPreflightService =
            importPreflightService;
        _questionExplanationService =
            questionExplanationService;
        _logger = logger;
    }
    private MemoryStream CreateTextStream(string text)
    {
        return new MemoryStream(
            Encoding.UTF8.GetBytes(text));
    }

    public async Task<int> ImportTestAsync(
    int teacherUserId,
    Stream testFileStream,
    string testFileName,
    string contentType,
    string questionCountsJson,
    string? transcript,
    List<ListeningAudioUploadItem> audioFiles,
    ImportTestDto dto)
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
        "test-set");
        var classRoom =
    await _unitOfWork.ClassRooms
        .Query()
        .Include(x => x.Course)
        .AsNoTracking()
        .FirstOrDefaultAsync(x =>
            x.ClassRoomId ==
                dto.ClassRoomId &&
            !x.IsDeleted);

        if (classRoom == null)
        {
            throw new Exception(
                "Class room not found");
        }

        ValidateClassCourseType(
            classRoom.Course.CourseType,
            dto.CourseType);

        ValidateTestTypeMatchesCourseType(
            dto.TestType,
            dto.CourseType);

        var teacherHasClass =
            await _unitOfWork
                .TeacherRoleRepository
                .Query()
                .Where(x =>
                    x.UserId ==
                        teacherUserId &&
                    !x.IsDeleted)
                .SelectMany(x =>
                    x.TeacherClasses)
                .AnyAsync(x =>
                    x.ClassRoomId ==
                        dto.ClassRoomId);

        if (!teacherHasClass)
        {
            throw new Exception(
                "Teacher is not assigned to this class");
        }

        var extension = Path.GetExtension(testFileName).ToLower();

        if (extension != ".pdf" &&
            extension != ".docx" &&
            extension != ".txt")
        {
            throw new Exception("Only PDF, Word or TXT test files are allowed");
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
            $"tests/source/" +
            $"{storageResourceFolder}/" +
            $"{Guid.NewGuid():N}_" +
            $"{safeSourceFileName}";

        await using var uploadStream = new MemoryStream(testFileBytes);
        var resolvedContentType =
    ResolveDocumentContentType(
        testFileName,
        contentType);
        var sourceFileUrl =
    await _minioService.UploadFileAsync(
        uploadStream,
        objectName,
        resolvedContentType);

        await using var extractStream = new MemoryStream(testFileBytes);
        Console.WriteLine("TEST IMPORT: extracting source file");
        var rawText = await _documentParserService.ExtractTextAsync(
            extractStream,
            testFileName);
        Console.WriteLine("TEST IMPORT: splitting document");
        var splitResult = await _testDocumentSplitService.SplitAsync(
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

        await _importPreflightService.ValidateTestImportAsync(
            splitResult,
            dto.TestType,
            extension,
            testFileBytes,
            audioFiles,
            questionCountsJson);

        ValidateRequiredSections(splitResult, dto.TestType);
        ValidateListeningInputs(
    splitResult,
    audioFiles,
    questionCountsJson);
        var listeningTasks =
    new List<EnglishCenterSystem.Domain.Entities.Listening.ListeningTask>();

        var readingTasks =
            new List<EnglishCenterSystem.Domain.Entities.Reading.ReadingTask>();

        var writingTasks =
            new List<EnglishCenterSystem.Domain.Entities.Writing.WritingTask>();

        var speakingTasks =
            new List<EnglishCenterSystem.Domain.Entities.Speaking.SpeakingTask>();
        Console.WriteLine("TEST IMPORT: parsing listening");
        if (!string.IsNullOrWhiteSpace(splitResult.ListeningText))
        {
            using var listeningStream =
                CreateTextStream(splitResult.ListeningText);

            listeningTasks =
    await _listeningCoreImportService
        .CreateListeningTasksAsync(
            teacherUserId,
            ImportStorageModule.Test,
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
        Console.WriteLine("TEST IMPORT: parsing reading");
        if (!string.IsNullOrWhiteSpace(splitResult.ReadingText))
        {
            using var readingStream =
                CreateTextStream(splitResult.ReadingText);

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
        if (
    readingTasks.Count > 0 &&
    extension == ".pdf"
)
        {
            await using var snapshotPdfStream =
                new MemoryStream(
                    testFileBytes);

            await _readingPassageSnapshotService
                .GeneratePassageImagesAsync(
                    snapshotPdfStream,
                    testFileName,
                    readingTasks,
                    $"tests/reading-passages/" +
                    $"{storageResourceFolder}");
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
        Console.WriteLine("TEST IMPORT: parsing writing");
        if (!string.IsNullOrWhiteSpace(splitResult.WritingText))
        {
            using var writingStream =
                CreateTextStream(splitResult.WritingText);

            writingTasks =
                await _writingCoreImportService.CreateWritingTasksAsync(
                    teacherUserId,
                    dto.CourseType,
                    writingStream,
                    $"{SafeFileName(dto.Title)}_writing.txt",
                    testFileBytes,
                    testFileName,
                    dto.TestType == TestType.ToeicFourSkills
                        ? 8
                        : dto.TestType == TestType.VstepFourSkills
                            ? 2
                            : null);
        }
        Console.WriteLine("TEST IMPORT: parsing speaking");
        if (!string.IsNullOrWhiteSpace(splitResult.SpeakingText))
        {
            using var speakingStream =
                CreateTextStream(splitResult.SpeakingText);

            speakingTasks =
    await _speakingCoreImportService
        .CreateSpeakingTasksAsync(
            teacherUserId,
            ImportStorageModule.Test,
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
                : dto.TestType == TestType.VstepFourSkills
                    ? 3
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

        var test = new Test
        {
            TeacherUserId = teacherUserId,
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description)
        ? null
        : dto.Description.Trim(),
            TestType = dto.TestType,
            CourseType = dto.CourseType,
            SourceFileUrl = sourceFileUrl,
            SourceFileObjectName = objectName,
            TotalPoints = 10,
            IsActive = true,

            ListeningTasks = listeningTasks
    .Select((task, index) => new TestListeningTask
    {
        ListeningTask = task,
        OrderIndex = index + 1,
        Point = listeningTasks.Count == 0
            ? 0
            : Math.Round(
                GetSkillTotalPoint(dto.TestType, SkillType.Listening)
                / listeningTasks.Count,
                2)
    })
    .ToList(),

            ReadingTasks = readingTasks
    .Select((task, index) => new TestReadingTask
    {
        ReadingTask = task,
        OrderIndex = index + 1,
        Point = readingTasks.Count == 0
            ? 0
            : Math.Round(
                GetSkillTotalPoint(dto.TestType, SkillType.Reading)
                / readingTasks.Count,
                2)
    })
    .ToList(),

            WritingTasks = writingTasks
        .Select((task, index) => new TestWritingTask
        {
            WritingTask = task,
            OrderIndex = index + 1,
            Point = writingTasks.Count == 0
                ? 0
                : Math.Round(
                    GetSkillTotalPoint(dto.TestType, SkillType.Writing)
                    / writingTasks.Count,
                    2)
        })
        .ToList(),

            SpeakingTasks = speakingTasks
        .Select((task, index) => new TestSpeakingTask
        {
            SpeakingTask = task,
            OrderIndex = index + 1,
            Point = speakingTasks.Count == 0
                ? 0
                : Math.Round(
                    GetSkillTotalPoint(dto.TestType, SkillType.Speaking)
                    / speakingTasks.Count,
                    2)
        })
        .ToList()
        };
        Console.WriteLine("TEST IMPORT: saving test");
        await _unitOfWork.Tests.AddAsync(test);
        await _unitOfWork.SaveChangesAsync();

        var assignment = new TestAssignment
        {
            TestId = test.TestId,
            ClassRoomId = dto.ClassRoomId,
            MaxAttempts = dto.MaxAttempts,
            DurationMinutes = dto.DurationMinutes,
            DeadlineAt = dto.DeadlineAt,
            IsActive = true
        };

        await _unitOfWork.TestAssignments
    .AddAsync(assignment);

        await _unitOfWork.SaveChangesAsync();

        await CreateTestNotificationSafelyAsync(
            teacherUserId,
            classRoom.ClassRoomId,
            classRoom.ClassName,
            test.Title,
            assignment.TestAssignmentId);

        Console.WriteLine(
            "TEST IMPORT: done");

        return test.TestId;
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

        var extension = Path
            .GetExtension(fileName)
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
    private async Task
    CreateTestNotificationSafelyAsync(
        int teacherUserId,
        int classRoomId,
        string classRoomName,
        string testTitle,
        int testAssignmentId)
    {
        try
        {
            await _notificationService
                .CreateForClassAsync(
                    new CreateClassNotificationDto
                    {
                        CreatedByUserId =
                            teacherUserId,

                        ClassRoomId =
                            classRoomId,

                        Type =
                            NotificationType
                                .TestAssigned,

                        RelatedEntityId =
                            testAssignmentId,

                        Title =
                            "New test assigned",

                        Message =
                            $"Test \"{testTitle}\" " +
                            $"has been assigned to class " +
                            $"{classRoomName}.",

                        TeacherActionUrl =
                            "/teacher/exams",

                        StudentActionUrl =
                            "/student/tests"
                    });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not create test notification for TestAssignmentId {TestAssignmentId}",
                testAssignmentId);
        }
    }
    private string SafeFileName(string value)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return value.Trim();
    }
    private void ValidateParsedTasks(
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
            throw new Exception("Listening section could not be parsed");
        }

        if (readingTaskCount <= 0)
        {
            throw new Exception("Reading section could not be parsed");
        }

        if (testType == TestType.ToeicTwoSkills)
        {
            ValidateToeicObjectiveQuestionCounts(
                listeningQuestionCount,
                readingQuestionCount);

            return;
        }

        if (testType == TestType.ToeicFourSkills)
        {
            ValidateToeicObjectiveQuestionCounts(
                listeningQuestionCount,
                readingQuestionCount);

            if (writingTaskCount != 8)
            {
                throw new Exception(
                    $"TOEIC 4 skills test requires 8 Writing tasks but parsed {writingTaskCount}. " +
                    BuildToeicWritingParseSummary(writingTasks));
            }

            if (speakingTaskCount != 11)
            {
                throw new Exception(
                    $"TOEIC 4 skills test requires 11 Speaking tasks but parsed {speakingTaskCount}");
            }

            return;
        }

        if (writingTaskCount <= 0)
        {
            throw new Exception("Writing section could not be parsed");
        }

        if (speakingTaskCount <= 0)
        {
            throw new Exception("Speaking section could not be parsed");
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

    private void ValidateToeicObjectiveQuestionCounts(
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
    private double GetSkillTotalPoint(
    TestType testType,
    SkillType skillType)
    {
        if (testType == TestType.ToeicTwoSkills)
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
    private void ValidateListeningInputs(
    TestDocumentSplitResultDto splitResult,
    List<ListeningAudioUploadItem> audioFiles,
    string questionCountsJson)
    {
        if (string.IsNullOrWhiteSpace(splitResult.ListeningText))
        {
            return;
        }

        if (audioFiles == null || audioFiles.Count == 0)
        {
            throw new Exception("Listening audio files are required");
        }

        if (string.IsNullOrWhiteSpace(questionCountsJson) ||
            questionCountsJson == "[]")
        {
            throw new Exception("Listening question counts are required");
        }
    }
    private void ValidateDto(ImportTestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new Exception("Test title is required");
        }

        if (dto.MaxAttempts <= 0)
        {
            throw new Exception("Max attempts must be greater than 0");
        }

        if (dto.DurationMinutes <= 0)
        {
            throw new Exception("Duration must be greater than 0");
        }

        if (dto.DurationMinutes > 24 * 60)
        {
            throw new Exception("Duration cannot exceed 24 hours");
        }

        if (!Enum.IsDefined(typeof(TestType), dto.TestType))
        {
            throw new Exception("Invalid test type");
        }

        if (!Enum.IsDefined(typeof(CourseType), dto.CourseType))
        {
            throw new Exception("Invalid course type");
        }

        ValidateTestTypeMatchesCourseType(
            dto.TestType,
            dto.CourseType);
    }

    private static void ValidateClassCourseType(
        string classCourseType,
        CourseType importCourseType)
    {
        if (!Enum.TryParse<CourseType>(
            classCourseType,
            true,
            out var parsedClassCourseType))
        {
            throw new Exception(
                "Class course type is invalid");
        }

        if (parsedClassCourseType != importCourseType)
        {
            throw new Exception(
                "Test course type does not match the class course type");
        }
    }

    private static void ValidateTestTypeMatchesCourseType(
        TestType testType,
        CourseType courseType)
    {
        var expectedCourseType =
            testType == TestType.VstepFourSkills
                ? CourseType.VSTEP
                : CourseType.TOEIC;

        if (courseType != expectedCourseType)
        {
            throw new Exception(
                "Test type does not match the selected course type");
        }
    }

    private void ValidateRequiredSections(
        TestDocumentSplitResultDto splitResult,
        TestType testType)
    {
        if (testType == TestType.ToeicTwoSkills)
        {
            if (string.IsNullOrWhiteSpace(splitResult.ListeningText))
            {
                throw new Exception("TOEIC 2 skills test requires Listening section");
            }

            if (string.IsNullOrWhiteSpace(splitResult.ReadingText))
            {
                throw new Exception("TOEIC 2 skills test requires Reading section");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(splitResult.ListeningText))
        {
            throw new Exception("Four skills test requires Listening section");
        }

        if (string.IsNullOrWhiteSpace(splitResult.ReadingText))
        {
            throw new Exception("Four skills test requires Reading section");
        }

        if (string.IsNullOrWhiteSpace(splitResult.WritingText))
        {
            throw new Exception("Four skills test requires Writing section");
        }

        if (string.IsNullOrWhiteSpace(splitResult.SpeakingText))
        {
            throw new Exception("Four skills test requires Speaking section");
        }
    }

    private static bool IsToeicTestType(TestType testType)
    {
        return testType == TestType.ToeicTwoSkills ||
               testType == TestType.ToeicFourSkills;
    }
}
