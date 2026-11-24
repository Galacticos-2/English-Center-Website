using EnglishCenterSystem.Application.Features.Reading.DTOs;
using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Application.Features.Tests.DTOs.Teacher;
using EnglishCenterSystem.Application.Features.Tests.Mappers;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace EnglishCenterSystem.Application.Features.Tests.Services;

public class StudentTestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinioService _minioService;
    private readonly ITestAiGradingQueueService _testAiGradingQueueService;
    private readonly TestResultMapper _testResultMapper;
    private readonly IStudentGamificationService _studentGamificationService;
    public StudentTestService(
    IUnitOfWork unitOfWork,
    IMinioService minioService,
    ITestAiGradingQueueService testAiGradingQueueService,
    TestResultMapper testResultMapper,
    IStudentGamificationService studentGamificationService)
    {
        _unitOfWork = unitOfWork;
        _minioService = minioService;
        _testAiGradingQueueService = testAiGradingQueueService;
        _testResultMapper = testResultMapper;
        _studentGamificationService = studentGamificationService;
    }


    public async Task<List<StudentTestDto>> GetMyTestsAsync(
    int studentUserId)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x =>
                x.UserId == studentUserId &&
                !x.IsDeleted);

        if (studentRole == null ||
            studentRole.ClassRoomId == null)
        {
            return new List<StudentTestDto>();
        }

        var attempts = await _unitOfWork.TestAttempts
    .QueryBasic()
    .AsNoTracking()
    .Where(x =>
        x.StudentRoleId == studentRole.StudentRoleId)
    .Select(x => new
    {
        x.TestAttemptId,
        x.TestAssignmentId,
        x.StartedAt,
        x.SubmittedAt,
        x.Score,
        x.Status,

        ListeningRawScore = x.Answers
            .Where(a => a.ListeningQuestionId != null)
            .Sum(a => (double?)a.PointEarned) ?? 0,

        ReadingRawScore = x.Answers
            .Where(a => a.ReadingQuestionId != null)
            .Sum(a => (double?)a.PointEarned) ?? 0,

        ListeningCorrectCount = x.Answers
            .Count(a =>
                a.ListeningQuestionId != null &&
                a.IsCorrect),

        ReadingCorrectCount = x.Answers
            .Count(a =>
                a.ReadingQuestionId != null &&
                a.IsCorrect),

        WritingRawScore = x.Answers
            .Where(a => a.WritingTaskId != null)
            .Sum(a => (double?)a.PointEarned) ?? 0,

        SpeakingRawScore = x.Answers
            .Where(a => a.SpeakingTaskId != null)
            .Sum(a => (double?)a.PointEarned) ?? 0
    })
    .ToListAsync();

        var assignments = await _unitOfWork.TestAssignments
            .QueryBasic()
            .AsNoTracking()
            .Where(x =>
                x.ClassRoomId == studentRole.ClassRoomId.Value &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Test.IsActive &&
                !x.Test.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.TestAssignmentId,
                x.TestId,
                x.MaxAttempts,
                x.DurationMinutes,
                TestTitle = x.Test.Title,
                TestDescription = x.Test.Description,
                x.Test.TestType,
                x.Test.CourseType,
                x.Test.TotalPoints,

                ListeningCount = x.Test.ListeningTasks
                    .SelectMany(l => l.ListeningTask.Questions)
                    .Count(),

                ReadingCount = x.Test.ReadingTasks
                    .SelectMany(r => r.ReadingTask.Questions)
                    .Count(),

                WritingCount = x.Test.WritingTasks.Count,

                SpeakingCount = x.Test.SpeakingTasks.Count
            })
            .ToListAsync();

        return assignments.Select(assignment =>
        {
            var assignmentAttempts = attempts
                .Where(a =>
                    a.TestAssignmentId ==
                    assignment.TestAssignmentId)
                .ToList();

            var submittedAttempts = assignmentAttempts
                .Where(a =>
                    a.Status != TestAttemptStatus.InProgress)
                .ToList();

            return new StudentTestDto
            {
                TestAssignmentId = assignment.TestAssignmentId,
                TestId = assignment.TestId,
                Title = assignment.TestTitle,
                Description = assignment.TestDescription,
                TestType = assignment.TestType,
                CourseType = assignment.CourseType,
                MaxAttempts = assignment.MaxAttempts,
                DurationMinutes = assignment.DurationMinutes,
                AttemptCount = submittedAttempts.Count,
                CanStart =
                    assignmentAttempts.Any(a =>
                        a.Status == TestAttemptStatus.InProgress)
                    ||
                    submittedAttempts.Count < assignment.MaxAttempts,
                TotalQuestions =
                    assignment.ListeningCount +
                    assignment.ReadingCount +
                    assignment.WritingCount +
                    assignment.SpeakingCount,
                TotalPoints = assignment.TotalPoints,
                AttemptHistory = submittedAttempts
    .Where(a => a.SubmittedAt != null)
    .OrderByDescending(a => a.SubmittedAt)
    .Select(a => new StudentTestAttemptHistoryItemDto
    {
        TestAttemptId = a.TestAttemptId,
        StartedAt = a.StartedAt,
        SubmittedAt = a.SubmittedAt,
        Score = a.Score,
        InternalScore =
            Math.Round(
                a.ListeningRawScore +
                a.ReadingRawScore +
                a.WritingRawScore +
                a.SpeakingRawScore,
                2),

        ToeicListeningScore =
            ToeicScoreConversionHelper.IsToeic(
                assignment.TestType)
                ? ToeicScoreConversionHelper
                    .ConvertListeningScore(
                        a.ListeningCorrectCount)
                : null,

        ToeicReadingScore =
            ToeicScoreConversionHelper.IsToeic(
                assignment.TestType)
                ? ToeicScoreConversionHelper
                    .ConvertReadingScore(
                        a.ReadingCorrectCount)
                : null,

        ToeicTotalScore =
            ToeicScoreConversionHelper.IsToeic(
                assignment.TestType)
                ? Convert.ToInt32(
                    Math.Round(a.Score))
                : null,

        ListeningScore = assignment.ListeningCount > 0
            ? ConvertSkillScoreTo10(
                a.ListeningRawScore,
                GetSkillTotalPoint(
                    assignment.TestType,
                    SkillType.Listening))
            : null,

        ReadingScore = assignment.ReadingCount > 0
            ? ConvertSkillScoreTo10(
                a.ReadingRawScore,
                GetSkillTotalPoint(
                    assignment.TestType,
                    SkillType.Reading))
            : null,

        WritingScore = assignment.WritingCount > 0
            ? ConvertSkillScoreTo10(
                a.WritingRawScore,
                GetSkillTotalPoint(
                    assignment.TestType,
                    SkillType.Writing))
            : null,

        SpeakingScore = assignment.SpeakingCount > 0
            ? ConvertSkillScoreTo10(
                a.SpeakingRawScore,
                GetSkillTotalPoint(
                    assignment.TestType,
                    SkillType.Speaking))
            : null,

        Status = a.Status.ToString()
    })
    .ToList()
            };
        }).ToList();
    }

    public async Task<TestAttemptDto> StartAttemptAsync(
        int studentUserId,
        StartTestAttemptDto dto)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null ||
            studentRole.ClassRoomId == null)
        {
            throw new Exception("Student has no class");
        }

        var assignment = await _unitOfWork.TestAssignments
    .GetForStartAttemptAsync(dto.TestAssignmentId);

        if (assignment == null ||
            !assignment.IsActive ||
            !assignment.Test.IsActive)
        {
            throw new Exception("Test assignment not found");
        }

        if (assignment.ClassRoomId != studentRole.ClassRoomId.Value)
        {
            throw new Exception("You cannot access this test");
        }

        var inProgressAttempt = await _unitOfWork.TestAttempts
    .QueryBasic()
    .AsNoTracking()
    .FirstOrDefaultAsync(x =>
        x.TestAssignmentId == dto.TestAssignmentId &&
        x.StudentRoleId == studentRole.StudentRoleId &&
        x.Status == TestAttemptStatus.InProgress);
        if (inProgressAttempt != null)
        {
            return BuildTestAttemptDto(
                inProgressAttempt,
                assignment);
        }
        var usedAttemptCount = await _unitOfWork.TestAttempts
    .QueryBasic()
    .AsNoTracking()
    .CountAsync(x =>
        x.TestAssignmentId == dto.TestAssignmentId &&
        x.StudentRoleId == studentRole.StudentRoleId &&
        x.Status != TestAttemptStatus.InProgress);

        if (usedAttemptCount >= assignment.MaxAttempts)
        {
            throw new Exception("Maximum attempts reached");
        }

        var attempt = new TestAttempt
        {
            TestAssignmentId = dto.TestAssignmentId,
            StudentRoleId = studentRole.StudentRoleId,
            AttemptNumber = usedAttemptCount + 1,
            StartedAt = DateTime.UtcNow,
            Status = TestAttemptStatus.InProgress,
            TotalPoints = assignment.Test.TotalPoints
        };

        await _unitOfWork.TestAttempts.AddAsync(attempt);
        await _unitOfWork.SaveChangesAsync();

        return BuildTestAttemptDto(
            attempt,
            assignment);
    }

    public async Task<TestAnswerResultDto> SubmitSpeakingAnswerAsync(
        int studentUserId,
        int testAttemptId,
        int speakingTaskId,
        Stream audioStream,
        string fileName,
        string contentType)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null)
        {
            throw new Exception("Student not found");
        }

        var attempt = await _unitOfWork.TestAttempts
    .GetForAiGradingAsync(testAttemptId);

        if (attempt == null ||
            attempt.StudentRoleId != studentRole.StudentRoleId)
        {
            throw new Exception("Test attempt not found");
        }

        if (attempt.Status != TestAttemptStatus.InProgress)
        {
            throw new Exception("Only in-progress attempt can submit speaking audio");
        }

        var speakingLink = attempt.TestAssignment.Test
            .SpeakingTasks
            .FirstOrDefault(x => x.SpeakingTaskId == speakingTaskId);

        if (speakingLink == null)
        {
            throw new Exception("Speaking task not found in this test");
        }

        var objectName =
            $"tests/speaking/answers/{studentRole.StudentRoleId}/{Guid.NewGuid()}_{fileName}";

        var memoryStream = new MemoryStream();

        await audioStream.CopyToAsync(memoryStream);

        memoryStream.Position = 0;

        var audioUrl = await _minioService.UploadFileAsync(
            memoryStream,
            objectName,
            contentType);

        var oldAnswer = attempt.Answers
            .FirstOrDefault(x => x.SpeakingTaskId == speakingTaskId);

        if (oldAnswer != null)
        {
            oldAnswer.SpeakingAudioUrl = audioUrl;
            oldAnswer.AudioAnswerUrl = audioUrl;
            oldAnswer.SpeakingAudioObjectName = objectName;
            oldAnswer.PointEarned = 0;
            oldAnswer.IsCorrect = false;
            oldAnswer.TeacherFeedback =
                "Speaking audio uploaded. Waiting for final submission.";
            oldAnswer.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            attempt.Answers.Add(new TestAnswer
            {
                SpeakingTaskId = speakingTaskId,
                SpeakingAudioUrl = audioUrl,
                AudioAnswerUrl = audioUrl,
                SpeakingAudioObjectName = objectName,
                IsCorrect = false,
                PointEarned = 0,
                TeacherFeedback =
                    "Speaking audio uploaded. Waiting for final submission."
            });
        }

        await _unitOfWork.SaveChangesAsync();

        return new TestAnswerResultDto
        {
            QuestionId = 0,
            SpeakingTaskId = speakingTaskId,
            SpeakingTaskTitle = speakingLink.SpeakingTask.Title,
            AudioAnswerUrl = audioUrl,
            IsCorrect = false,
            PointEarned = 0,
            TeacherFeedback =
                "Đã lưu audio. Hệ thống sẽ chấm khi bạn bấm Nộp bài."
        };
    }

    public async Task<TestResultDto> SubmitAttemptAsync(
        int studentUserId,
        SubmitTestAttemptDto dto)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null)
        {
            throw new Exception("Student not found");
        }

        var attempt = await _unitOfWork.TestAttempts
    .GetForSubmitAsync(dto.TestAttemptId);

        if (attempt == null ||
            attempt.StudentRoleId != studentRole.StudentRoleId)
        {
            throw new Exception("Test attempt not found");
        }

        if (attempt.Status != TestAttemptStatus.InProgress)
        {
            throw new Exception("Only in-progress attempt can be submitted");
        }

        var test = attempt.TestAssignment.Test;

        var listeningQuestions = test.ListeningTasks
    .OrderBy(x => x.OrderIndex)
    .SelectMany(x => x.ListeningTask.Questions
        .OrderBy(q => q.OrderIndex))
    .ToList();

        var readingQuestions = test.ReadingTasks
            .OrderBy(x => x.OrderIndex)
            .SelectMany(x => x.ReadingTask.Questions
                .OrderBy(q => q.OrderIndex))
            .ToList();

        var writingLinks = test.WritingTasks
            .OrderBy(x => x.OrderIndex)
            .ToList();

        var speakingLinks = test.SpeakingTasks
            .OrderBy(x => x.OrderIndex)
            .ToList();

        if (!listeningQuestions.Any() &&
            !readingQuestions.Any() &&
            !writingLinks.Any() &&
            !speakingLinks.Any())
        {
            throw new Exception("Test has no questions");
        }

        var existingSpeakingAnswers = attempt.Answers
            .Where(x => x.SpeakingTaskId != null)
            .ToList();

        attempt.Answers.Clear();

        foreach (var speakingAnswer in existingSpeakingAnswers)
        {
            attempt.Answers.Add(speakingAnswer);
        }

        var correctAnswers = 0;
        var score = 0.0;

        GradeListeningAnswers(
            dto,
            attempt,
            listeningQuestions,
            test,
            ref correctAnswers,
            ref score);

        GradeReadingAnswers(
            dto,
            attempt,
            readingQuestions,
            test,
            ref correctAnswers,
            ref score);

        SaveWritingAnswers(
            dto,
            attempt,
            writingLinks);

        EnsureSpeakingAnswers(
            attempt,
            speakingLinks);

        attempt.TotalQuestions =
            listeningQuestions.Count +
            readingQuestions.Count +
            writingLinks.Count +
            speakingLinks.Count;

        attempt.CorrectAnswers = correctAnswers;
        attempt.TotalPoints = test.TotalPoints;
        attempt.Score = ToeicScoreConversionHelper
            .CalculateTestScore(
                test,
                attempt.Answers)
            .ScoreForRanking;
        attempt.SubmittedAt = DateTime.UtcNow;

        var needAiGrading =
            writingLinks.Any() ||
            speakingLinks.Any();

        attempt.Status = needAiGrading
            ? TestAttemptStatus.PendingAiGrading
            : TestAttemptStatus.Submitted;

        attempt.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        if (!needAiGrading)
        {
            await _studentGamificationService.AwardTestSubmissionAsync(attempt.TestAttemptId);
        }
        else
        {
            await _studentGamificationService.RecordDailyActivityAsync(
                attempt.StudentRoleId,
                attempt.SubmittedAt!.Value);
        }

        if (needAiGrading)
        {
            await _testAiGradingQueueService.EnqueueAsync(
                attempt.TestAttemptId);
        }

        return _testResultMapper.BuildResultDto(attempt);
    }

    public async Task<List<TestResultDto>> GetMyHistoryAsync(
        int studentUserId)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null)
        {
            return new List<TestResultDto>();
        }

        var attempts = await _unitOfWork.TestAttempts
            .Query()
            .Where(x => x.StudentRoleId == studentRole.StudentRoleId)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync();

        return attempts
    .Select(x => _testResultMapper.BuildResultDto(x))
    .ToList();
    }

    public async Task<TestResultDto> GetAttemptResultAsync(
        int studentUserId,
        int testAttemptId)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null)
        {
            throw new Exception("Student not found");
        }

        var attempt = await _unitOfWork.TestAttempts
    .GetResultByIdAsync(testAttemptId);

        if (attempt == null ||
            attempt.StudentRoleId != studentRole.StudentRoleId)
        {
            throw new Exception("Test attempt not found");
        }

        return _testResultMapper.BuildResultDto(attempt);
    }

    private void GradeListeningAnswers(
        SubmitTestAttemptDto dto,
        TestAttempt attempt,
        List<Domain.Entities.Listening.ListeningQuestion> listeningQuestions,
        Test test,
        ref int correctAnswers,
        ref double score)
    {
        var listeningRawPoint = listeningQuestions.Sum(x => x.Point);

        var listeningTotalPoint = GetSkillTotalPoint(
            test.TestType,
            SkillType.Listening);

        foreach (var question in listeningQuestions)
        {
            var submittedAnswer = dto.Answers
                .FirstOrDefault(x =>
                    x.ListeningQuestionId ==
                    question.ListeningQuestionId);

            var selectedOptionId =
                submittedAnswer?.SelectedListeningOptionId;

            var correctOption = question.Options
                .FirstOrDefault(x => x.IsCorrect);

            var selectedOption = question.Options
                .FirstOrDefault(x =>
                    x.ListeningOptionId == selectedOptionId);

            var isCorrect =
                selectedOption != null &&
                correctOption != null &&
                selectedOption.ListeningOptionId ==
                correctOption.ListeningOptionId;

            var pointEarned = isCorrect && listeningRawPoint > 0
    ? question.Point / listeningRawPoint * listeningTotalPoint
    : 0;

            if (isCorrect)
            {
                correctAnswers++;
            }

            score += pointEarned;

            attempt.Answers.Add(new TestAnswer
            {
                ListeningQuestionId = question.ListeningQuestionId,
                SelectedListeningOptionId = selectedOptionId,
                IsCorrect = isCorrect,
                PointEarned = pointEarned
            });
        }
    }

    private void GradeReadingAnswers(
        SubmitTestAttemptDto dto,
        TestAttempt attempt,
        List<Domain.Entities.Reading.ReadingQuestion> readingQuestions,
        Test test,
        ref int correctAnswers,
        ref double score)
    {
        var readingRawPoint = readingQuestions.Sum(x => x.Point);

        var readingTotalPoint = GetSkillTotalPoint(
            test.TestType,
            SkillType.Reading);

        foreach (var question in readingQuestions)
        {
            var submittedAnswer = dto.Answers
                .FirstOrDefault(x =>
                    x.ReadingQuestionId ==
                    question.ReadingQuestionId);

            var selectedOptionId =
                submittedAnswer?.SelectedReadingOptionId;

            var correctOption = question.Options
                .FirstOrDefault(x => x.IsCorrect);

            var selectedOption = question.Options
                .FirstOrDefault(x =>
                    x.ReadingOptionId == selectedOptionId);

            var isCorrect =
                selectedOption != null &&
                correctOption != null &&
                selectedOption.ReadingOptionId ==
                correctOption.ReadingOptionId;

            var pointEarned = isCorrect && readingRawPoint > 0
    ? question.Point / readingRawPoint * readingTotalPoint
    : 0;

            if (isCorrect)
            {
                correctAnswers++;
            }

            score += pointEarned;

            attempt.Answers.Add(new TestAnswer
            {
                ReadingQuestionId = question.ReadingQuestionId,
                SelectedReadingOptionId = selectedOptionId,
                IsCorrect = isCorrect,
                PointEarned = pointEarned
            });
        }
    }
    private double ConvertSkillScoreTo10(
    double earnedPoint,
    double maxSkillPoint)
    {
        if (maxSkillPoint <= 0)
        {
            return 0;
        }

        return Math.Round(earnedPoint / maxSkillPoint * 10, 2);
    }
    private void SaveWritingAnswers(
        SubmitTestAttemptDto dto,
        TestAttempt attempt,
        List<TestWritingTask> writingLinks)
    {
        foreach (var writingLink in writingLinks)
        {
            var submittedAnswer = dto.Answers
                .FirstOrDefault(x =>
                    x.WritingTaskId ==
                    writingLink.WritingTaskId);

            attempt.Answers.Add(new TestAnswer
            {
                WritingTaskId = writingLink.WritingTaskId,
                TextAnswer = submittedAnswer?.TextAnswer ?? "",
                IsCorrect = false,
                PointEarned = 0
            });
        }
    }

    private void EnsureSpeakingAnswers(
        TestAttempt attempt,
        List<TestSpeakingTask> speakingLinks)
    {
        foreach (var speakingLink in speakingLinks)
        {
            var existingAnswer = attempt.Answers
                .FirstOrDefault(x =>
                    x.SpeakingTaskId ==
                    speakingLink.SpeakingTaskId);

            if (existingAnswer == null)
            {
                attempt.Answers.Add(new TestAnswer
                {
                    SpeakingTaskId = speakingLink.SpeakingTaskId,
                    IsCorrect = false,
                    PointEarned = 0,
                    TeacherFeedback =
                        "Student did not submit speaking audio"
                });
            }
        }
    }

    private TestAttemptDto BuildTestAttemptDto(
        TestAttempt attempt,
        TestAssignment assignment)
    {
        var questions = new List<StudentTestQuestionDto>();

        var globalOrderIndex = 0;

        foreach (var listeningLink in assignment.Test.ListeningTasks
            .OrderBy(x => x.OrderIndex))
        {
            foreach (var question in listeningLink.ListeningTask.Questions
                .OrderBy(x => x.OrderIndex))
            {
                globalOrderIndex++;

                questions.Add(new StudentTestQuestionDto
                {
                    QuestionId = 0,
                    ListeningQuestionId = question.ListeningQuestionId,
                    ListeningTaskId = listeningLink.ListeningTaskId,
                    ListeningCourseType =
    listeningLink.ListeningTask.CourseType,
                    ListeningPartTitle =
    listeningLink.ListeningTask.PartTitle,

                    ListeningDirection =
    listeningLink.ListeningTask.Direction,
                    ListeningLittlePart =
    listeningLink.ListeningTask.LittlePart,
                    Content = question.Content,
                    QuestionType = QuestionType.SingleChoice,
                    OrderIndex = globalOrderIndex,
                    Point = question.Point,
                    IsListeningQuestion = true,
                    AudioUrl = listeningLink.ListeningTask.AudioUrl,
                    ListeningPromptImageUrl =
                        question.PromptImageUrl,
                    Transcript = listeningLink.ListeningTask.Transcript,
                    Options = question.Options
                        .OrderBy(o => o.OrderIndex)
                        .Select(o => new StudentTestQuestionOptionDto
                        {
                            QuestionOptionId = 0,
                            ListeningOptionId = o.ListeningOptionId,
                            Content = o.Content,
                            OrderIndex = o.OrderIndex
                        })
                        .ToList()
                });
            }
        }

        foreach (var readingLink in assignment.Test.ReadingTasks
            .OrderBy(x => x.OrderIndex))
        {
            foreach (var question in readingLink.ReadingTask.Questions
                .OrderBy(x => x.OrderIndex))
            {
                globalOrderIndex++;

                questions.Add(new StudentTestQuestionDto
                {
                    QuestionId = 0,
                    ReadingQuestionId = question.ReadingQuestionId,
                    ReadingTaskId = readingLink.ReadingTaskId,
                    Content = question.Content,
                    QuestionType = QuestionType.SingleChoice,
                    OrderIndex = globalOrderIndex,
                    Point = question.Point,
                    IsReadingQuestion = true,
                    ReadingPassageTitle = readingLink.ReadingTask.Title,
                    ReadingPassageText = readingLink.ReadingTask.PassageText,
                    ReadingInstruction = readingLink.ReadingTask.Instruction,
                    ReadingPassageImages =
    readingLink.ReadingTask
        .PassageImages
        .OrderBy(image =>
            image.OrderIndex)
        .Select(image =>
            new ReadingPassageImageDto
            {
                ReadingPassageImageId =
                    image.ReadingPassageImageId,

                ImageUrl =
                    image.ImageUrl,

                PageNumber =
                    image.PageNumber,

                OrderIndex =
                    image.OrderIndex
            })
        .ToList(),
                    Options = question.Options
                        .OrderBy(o => o.OrderIndex)
                        .Select(o => new StudentTestQuestionOptionDto
                        {
                            QuestionOptionId = 0,
                            ReadingOptionId = o.ReadingOptionId,
                            Content = o.Content,
                            OrderIndex = o.OrderIndex
                        })
                        .ToList()
                });
            }
        }

        foreach (var writingLink in assignment.Test.WritingTasks
            .OrderBy(x => x.OrderIndex))
        {
            globalOrderIndex++;

            questions.Add(new StudentTestQuestionDto
            {
                QuestionId = 0,
                WritingTaskId = writingLink.WritingTaskId,
                Content = writingLink.WritingTask.Title,
                QuestionType = QuestionType.TextAnswer,
                OrderIndex = globalOrderIndex,
                Point = writingLink.Point,
                IsWritingQuestion = true,
                WritingInstruction = writingLink.WritingTask.Instruction,
                WritingPromptText = writingLink.WritingTask.PromptText,
                WritingPromptImageUrl =
                    writingLink.WritingTask.PromptImageUrl,
                WritingReferenceText = writingLink.WritingTask.ReferenceText,
                WritingSourceText = writingLink.WritingTask.SourceText,
                WritingRequirements = writingLink.WritingTask.Requirements,
                WritingScoringCriteria =
                    writingLink.WritingTask.ScoringCriteria,
                WritingMinimumWords = writingLink.WritingTask.MinimumWords,
                WritingTaskType =
                    writingLink.WritingTask.TaskType.ToString(),
                Options = new List<StudentTestQuestionOptionDto>()
            });
        }

        foreach (var speakingLink in assignment.Test.SpeakingTasks
            .OrderBy(x => x.OrderIndex))
        {
            globalOrderIndex++;

            questions.Add(new StudentTestQuestionDto
            {
                QuestionId = 0,
                SpeakingTaskId = speakingLink.SpeakingTaskId,
                Content = speakingLink.SpeakingTask.Title,
                QuestionType = QuestionType.AudioAnswer,
                OrderIndex = globalOrderIndex,
                Point = speakingLink.Point,
                IsSpeakingQuestion = true,
                SpeakingPdfUrl = speakingLink.SpeakingTask.PdfUrl,
                SpeakingInstruction = speakingLink.SpeakingTask.Instruction,
                SpeakingPromptText = speakingLink.SpeakingTask.PromptText,
                SpeakingPromptImageUrl =
                    speakingLink.SpeakingTask.PromptImageUrl,
                SpeakingReferenceText =
                    speakingLink.SpeakingTask.ReferenceText,
                PreparationTimeSeconds =
                    speakingLink.SpeakingTask.PreparationTimeSeconds,
                SpeakingTimeSeconds =
                    speakingLink.SpeakingTask.SpeakingTimeSeconds,
                Options = new List<StudentTestQuestionOptionDto>()
            });
        }

        return new TestAttemptDto
        {
            TestAttemptId = attempt.TestAttemptId,

            TestAssignmentId =
                assignment.TestAssignmentId,

            TestTitle =
                assignment.Test.Title,

            SourceFileUrl =
                assignment.Test.SourceFileUrl,

            SourceFileType =
                GetFileExtensionFromUrl(
                    assignment.Test.SourceFileUrl),

            StartedAt =
                attempt.StartedAt,

            DurationMinutes =
                assignment.DurationMinutes,

            Questions = questions
                .OrderBy(x => x.OrderIndex)
                .ToList()
        };
    }



    private static string? GetFileExtensionFromUrl(
    string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return null;
        }

        if (Uri.TryCreate(
                fileUrl,
                UriKind.Absolute,
                out var uri))
        {
            return Path
                .GetExtension(uri.AbsolutePath)
                .ToLowerInvariant();
        }

        return Path
            .GetExtension(fileUrl)
            .ToLowerInvariant();
    }

    private int CountTotalQuestions(Test test)
    {
        return test.ListeningTasks
            .SelectMany(x => x.ListeningTask.Questions)
            .Count()
            +
            test.ReadingTasks
            .SelectMany(x => x.ReadingTask.Questions)
            .Count()
            +
            test.WritingTasks.Count
            +
            test.SpeakingTasks.Count;
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
}
