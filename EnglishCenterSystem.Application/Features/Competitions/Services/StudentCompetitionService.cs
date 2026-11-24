using EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;
using EnglishCenterSystem.Application
    .Features.Competitions.Mappers;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application
    .Features.Competitions.Services;

public class StudentCompetitionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinioService _minioService;
    private readonly ICompetitionAiGradingQueueService
    _competitionAiGradingQueueService;
    private readonly CompetitionAttemptBuilder
        _attemptBuilder;
    private readonly CompetitionResultMapper
        _resultMapper;

    public StudentCompetitionService(
     IUnitOfWork unitOfWork,
     IMinioService minioService,
     CompetitionAttemptBuilder attemptBuilder,
     CompetitionResultMapper resultMapper,
     ICompetitionAiGradingQueueService
         competitionAiGradingQueueService)
    {
        _unitOfWork =
            unitOfWork;

        _minioService =
            minioService;

        _attemptBuilder =
            attemptBuilder;

        _resultMapper =
            resultMapper;

        _competitionAiGradingQueueService =
            competitionAiGradingQueueService;
    }
    public async Task<List<StudentCompetitionDto>>
    GetMyCompetitionsAsync(
        int studentUserId)
    {
        var studentRole =
            await _unitOfWork.StudentRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId ==
                        studentUserId &&
                    !x.IsDeleted);

        if (studentRole == null ||
            studentRole.ClassRoomId == null)
        {
            return new List<StudentCompetitionDto>();
        }

        var now =
            DateTime.UtcNow;

        var assignments =
            await _unitOfWork
                .CompetitionTestAssignments
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.ClassRoomId ==
                        studentRole.ClassRoomId.Value &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.Status !=
                        CompetitionStatus.Cancelled)
                .OrderByDescending(x =>
                    x.StartsAt)
                .ToListAsync();

        return assignments
            .Select(assignment =>
            {
                var attempt =
                    assignment.Attempts
                        .FirstOrDefault(x =>
                            x.StudentRoleId ==
                            studentRole.StudentRoleId);

                var resolvedStatus =
                    ResolveCompetitionStatus(
                        assignment.Status,
                        assignment.StartsAt,
                        assignment.DeadlineAt,
                        now);

                var attemptInProgress =
                    attempt?.Status ==
                    CompetitionAttemptStatus.InProgress;

                var attemptStillValid =
                    attemptInProgress &&
                    attempt!.ExpiresAt > now &&
                    assignment.DeadlineAt > now;

                var totalQuestions =
                    GetTotalQuestionCount(
                        assignment.CompetitionTest);

                var scoreBreakdown =
                    attempt == null
                        ? null
                        : ToeicScoreConversionHelper
                            .CalculateCompetitionScore(
                                assignment.CompetitionTest,
                                attempt.Answers);

                return new StudentCompetitionDto
                {
                    CompetitionTestAssignmentId =
                        assignment
                            .CompetitionTestAssignmentId,

                    CompetitionTestId =
                        assignment.CompetitionTestId,

                    Title =
                        assignment.Title,

                    Description =
                        assignment.Description,

                    CompetitionTestTitle =
                        assignment.CompetitionTest.Title,

                    ClassRoomName =
                        assignment.ClassRoom.ClassName,

                    TestType =
                        assignment.CompetitionTest.TestType,

                    CourseType =
                        assignment.CompetitionTest.CourseType,

                    StartsAt =
                        assignment.StartsAt,

                    DeadlineAt =
                        assignment.DeadlineAt,

                    DurationMinutes =
                        assignment.DurationMinutes,

                    Status =
                        resolvedStatus,

                    HasAttempt =
                        attempt != null,

                    CompetitionTestAttemptId =
                        attempt?.CompetitionTestAttemptId,

                    AttemptStartedAt =
                        attempt?.StartedAt,

                    AttemptExpiresAt =
                        attempt?.ExpiresAt,

                    SubmittedAt =
                        attempt?.SubmittedAt,

                    AttemptStatus =
                        ResolveAttemptStatus(
                            attempt,
                            assignment,
                            now),

                    CanStart =
                        attempt == null &&
                        resolvedStatus ==
                            CompetitionStatus.Open &&
                        now < assignment.DeadlineAt,

                    CanContinue =
                        attemptStillValid,

                    CanViewResult =
    attempt?.SubmittedAt != null &&
    (
        now >= assignment.DeadlineAt ||
        resolvedStatus ==
            CompetitionStatus.Ranked
    ),

                    Score =
                        attempt?.Score,

                    InternalScore =
                        scoreBreakdown?.InternalScore,

                    ToeicListeningScore =
                        scoreBreakdown?.ToeicListeningScore,

                    ToeicReadingScore =
                        scoreBreakdown?.ToeicReadingScore,

                    ToeicSpeakingScore =
                        scoreBreakdown?.ToeicSpeakingScore,

                    ToeicWritingScore =
                        scoreBreakdown?.ToeicWritingScore,

                    ToeicTotalScore =
                        scoreBreakdown?.ToeicTotalScore,

                    TotalQuestions =
                        totalQuestions,

                    TotalPoints =
                        assignment
                            .CompetitionTest
                            .TotalPoints,

                    ServerTime =
                        now
                };
            })
            .ToList();
    }
    public async Task<CompetitionAttemptDto>
    StartAsync(
        int studentUserId,
        int competitionTestAssignmentId)
    {
        var studentRole =
            await _unitOfWork.StudentRoles
                .FirstOrDefaultAsync(x =>
                    x.UserId ==
                        studentUserId &&
                    !x.IsDeleted);

        if (studentRole == null ||
            studentRole.ClassRoomId == null)
        {
            throw new Exception(
                "Student has no class");
        }

        var assignment =
            await _unitOfWork
                .CompetitionTestAssignments
                .GetForStudentStartAsync(
                    competitionTestAssignmentId);

        if (assignment == null)
        {
            throw new Exception(
                "Competition not found");
        }

        if (assignment.ClassRoomId !=
            studentRole.ClassRoomId.Value)
        {
            throw new Exception(
                "You cannot access this competition");
        }

        if (assignment.Status ==
            CompetitionStatus.Cancelled)
        {
            throw new Exception(
                "This competition has been cancelled");
        }

        var now =
            DateTime.UtcNow;

        if (now < assignment.StartsAt)
        {
            throw new Exception(
                "The competition has not started yet");
        }

        if (now >= assignment.DeadlineAt)
        {
            throw new Exception(
                "The competition deadline has expired");
        }

        var existingAttempt =
            await _unitOfWork
                .CompetitionTestAttempts
                .GetByAssignmentAndStudentAsync(
                    assignment
                        .CompetitionTestAssignmentId,
                    studentRole.StudentRoleId);

        if (existingAttempt != null)
        {
            if (existingAttempt.SubmittedAt != null)
            {
                throw new Exception(
                    "This competition attempt has already been submitted");
            }

            if (existingAttempt.Status ==
                CompetitionAttemptStatus.Expired)
            {
                throw new Exception(
                    "Your competition attempt time has expired");
            }

            if (existingAttempt.Status !=
                CompetitionAttemptStatus.InProgress)
            {
                throw new Exception(
                    "This competition attempt is no longer in progress");
            }

            if (now >= existingAttempt.ExpiresAt)
            {
                existingAttempt.Status =
                    CompetitionAttemptStatus.Expired;

                existingAttempt.UpdatedAt =
                    now;

                await _unitOfWork.SaveChangesAsync();

                throw new Exception(
                    "Your competition attempt time has expired");
            }

            return _attemptBuilder.Build(
                existingAttempt);
        }

        var durationExpiresAt =
            now.AddMinutes(
                assignment.DurationMinutes);

        var expiresAt =
            durationExpiresAt <
            assignment.DeadlineAt
                ? durationExpiresAt
                : assignment.DeadlineAt;

        var attempt =
            new CompetitionTestAttempt
            {
                CompetitionTestAssignmentId =
                    assignment
                        .CompetitionTestAssignmentId,

                StudentRoleId =
                    studentRole.StudentRoleId,

                AttemptNumber =
                    1,

                StartedAt =
                    now,

                ExpiresAt =
                    expiresAt,

                Status =
                    CompetitionAttemptStatus.InProgress,

                TotalPoints =
                    assignment.CompetitionTest.TotalPoints,

                TotalQuestions =
                    GetTotalQuestionCount(
                        assignment.CompetitionTest),

                CreatedAt =
                    now
            };

        attempt.Assignment =
            assignment;

        await _unitOfWork
            .CompetitionTestAttempts
            .AddAsync(attempt);

        await _unitOfWork.SaveChangesAsync();

        return _attemptBuilder.Build(
            attempt);
    }
    public async Task<CompetitionAnswerResultDto>
    SubmitSpeakingAnswerAsync(
        int studentUserId,
        int competitionTestAttemptId,
        int speakingTaskId,
        Stream audioStream,
        string fileName,
        string contentType)
    {
        var studentRole =
            await _unitOfWork.StudentRoles
                .FirstOrDefaultAsync(x =>
                    x.UserId ==
                        studentUserId &&
                    !x.IsDeleted);

        if (studentRole == null)
        {
            throw new Exception(
                "Student not found");
        }

        var attempt =
            await _unitOfWork
                .CompetitionTestAttempts
                .GetForSubmitAsync(
                    competitionTestAttemptId);

        if (attempt == null ||
            attempt.StudentRoleId !=
                studentRole.StudentRoleId)
        {
            throw new Exception(
                "Competition attempt not found");
        }

        ValidateAttemptCanBeModified(
            attempt,
            DateTime.UtcNow);

        var speakingLink =
            attempt.Assignment
                .CompetitionTest
                .SpeakingTasks
                .FirstOrDefault(x =>
                    x.SpeakingTaskId ==
                    speakingTaskId);

        if (speakingLink == null)
        {
            throw new Exception(
                "Speaking task does not belong to this competition");
        }

        var safeFileName =
            Path.GetFileName(fileName);

        var objectName =
            $"competitions/speaking-answers/" +
            $"{studentRole.StudentRoleId}/" +
            $"{competitionTestAttemptId}/" +
            $"{Guid.NewGuid()}_{safeFileName}";

        var audioUrl =
            await _minioService.UploadFileAsync(
                audioStream,
                objectName,
                contentType);

        var answer =
            attempt.Answers
                .FirstOrDefault(x =>
                    x.SpeakingTaskId ==
                    speakingTaskId);

        if (answer == null)
        {
            answer =
                new CompetitionTestAnswer
                {
                    CompetitionTestAttemptId =
                        attempt.CompetitionTestAttemptId,

                    SpeakingTaskId =
                        speakingTaskId,

                    IsCorrect =
                        false,

                    PointEarned =
                        0
                };

            attempt.Answers.Add(
                answer);
        }

        answer.AudioAnswerUrl =
            audioUrl;

        answer.SpeakingAudioUrl =
            audioUrl;

        answer.SpeakingAudioObjectName =
            objectName;

        answer.UpdatedAt =
            DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return new CompetitionAnswerResultDto
        {
            CompetitionTestAnswerId =
                answer.CompetitionTestAnswerId,

            SpeakingTaskId =
                speakingTaskId,

            SpeakingTaskTitle =
                speakingLink.SpeakingTask.Title,

            QuestionContent =
                speakingLink.SpeakingTask.Title,

            AudioAnswerUrl =
                audioUrl,

            PointEarned =
                0,

            IsCorrect =
                false,

            TeacherFeedback =
                "Đã lưu audio. Bài nói sẽ được chấm sau khi nộp bài."
        };
    }
    public async Task<CompetitionAttemptResultDto>
    SubmitAsync(
        int studentUserId,
        SubmitCompetitionAttemptDto dto)
    {
        var studentRole =
            await _unitOfWork.StudentRoles
                .FirstOrDefaultAsync(x =>
                    x.UserId ==
                        studentUserId &&
                    !x.IsDeleted);

        if (studentRole == null)
        {
            throw new Exception(
                "Student not found");
        }

        var attempt =
            await _unitOfWork
                .CompetitionTestAttempts
                .GetForSubmitAsync(
                    dto.CompetitionTestAttemptId);

        if (attempt == null ||
            attempt.StudentRoleId !=
                studentRole.StudentRoleId)
        {
            throw new Exception(
                "Competition attempt not found");
        }

        var now =
    DateTime.UtcNow;

        /*
         * Submit idempotent:
         * nếu request trước đã nộp thành công nhưng frontend retry,
         * trả lại kết quả hiện tại thay vì báo lỗi.
         */
        if (attempt.SubmittedAt != null)
        {
            var existingResultAttempt =
                await _unitOfWork
                    .CompetitionTestAttempts
                    .GetResultAsync(
                        attempt
                            .CompetitionTestAttemptId);

            if (existingResultAttempt == null)
            {
                throw new Exception(
                    "Submitted competition result could not be loaded");
            }

            return _resultMapper.Build(
                existingResultAttempt);
        }
        
        ValidateAttemptCanBeModified(
            attempt,
            now);

        var competitionTest =
            attempt.Assignment.CompetitionTest;

        var listeningQuestions =
            competitionTest.ListeningTasks
                .OrderBy(x => x.OrderIndex)
                .SelectMany(link =>
                    link.ListeningTask.Questions
                        .OrderBy(question =>
                            question.OrderIndex))
                .ToList();

        var readingQuestions =
            competitionTest.ReadingTasks
                .OrderBy(x => x.OrderIndex)
                .SelectMany(link =>
                    link.ReadingTask.Questions
                        .OrderBy(question =>
                            question.OrderIndex))
                .ToList();

        var writingLinks =
            competitionTest.WritingTasks
                .OrderBy(x => x.OrderIndex)
                .ToList();

        var speakingLinks =
            competitionTest.SpeakingTasks
                .OrderBy(x => x.OrderIndex)
                .ToList();

        var existingSpeakingAnswers =
            attempt.Answers
                .Where(x =>
                    x.SpeakingTaskId != null)
                .ToList();

        var removableAnswers =
            attempt.Answers
                .Where(x =>
                    x.SpeakingTaskId == null)
                .ToList();

        foreach (var answer in removableAnswers)
        {
            attempt.Answers.Remove(
                answer);
        }

        var correctAnswers = 0;
        var objectiveScore = 0.0;

        GradeListeningAnswers(
            dto,
            attempt,
            competitionTest,
            listeningQuestions,
            ref correctAnswers,
            ref objectiveScore);

        GradeReadingAnswers(
            dto,
            attempt,
            competitionTest,
            readingQuestions,
            ref correctAnswers,
            ref objectiveScore);

        SaveWritingAnswers(
            dto,
            attempt,
            writingLinks);

        EnsureSpeakingAnswers(
            attempt,
            speakingLinks,
            existingSpeakingAnswers);

        attempt.TotalQuestions =
            listeningQuestions.Count +
            readingQuestions.Count +
            writingLinks.Count +
            speakingLinks.Count;

        attempt.CorrectAnswers =
            correctAnswers;

        attempt.TotalPoints =
            competitionTest.TotalPoints;

        attempt.Score =
            ToeicScoreConversionHelper
                .CalculateCompetitionScore(
                    competitionTest,
                    attempt.Answers)
                .ScoreForRanking;

        attempt.SubmittedAt =
            now;

        var needsAiGrading =
            writingLinks.Count > 0 ||
            speakingLinks.Count > 0;

        attempt.Status =
            needsAiGrading
                ? CompetitionAttemptStatus
                    .PendingAiGrading
                : CompetitionAttemptStatus
                    .Submitted;

        attempt.UpdatedAt =
            now;

        await _unitOfWork.SaveChangesAsync();

        if (needsAiGrading)
        {
            try
            {
                await _competitionAiGradingQueueService
                    .EnqueueAsync(
                        attempt
                            .CompetitionTestAttemptId);
            }
            catch
            {
                /*
                 * Bài đã được nộp thành công nhưng
                 * không tạo được Hangfire job.
                 */
                attempt.Status =
                    CompetitionAttemptStatus
                        .AiGradingFailed;

                attempt.UpdatedAt =
                    DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
            }
        }

        var resultAttempt =
            await _unitOfWork
                .CompetitionTestAttempts
                .GetResultAsync(
                    attempt.CompetitionTestAttemptId);

        if (resultAttempt == null)
        {
            throw new Exception(
                "Competition result could not be loaded");
        }

        return _resultMapper.Build(
            resultAttempt);
    }
    public async Task<StudentCompetitionLeaderboardDto>
    GetLeaderboardAsync(
        int studentUserId,
        int competitionTestAssignmentId)
    {
        var studentRole =
            await _unitOfWork.StudentRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == studentUserId &&
                    !x.IsDeleted);

        if (studentRole == null ||
            studentRole.ClassRoomId == null)
        {
            throw new Exception(
                "Student has no class");
        }

        var assignment =
            await _unitOfWork
                .CompetitionTestAssignments
                .GetDetailAsync(
                    competitionTestAssignmentId);

        if (assignment == null ||
            assignment.ClassRoomId !=
                studentRole.ClassRoomId.Value ||
            assignment.IsDeleted ||
            !assignment.IsActive)
        {
            throw new Exception(
                "Competition not found");
        }

        var results =
            await _unitOfWork.CompetitionResults
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.CompetitionTestAssignmentId ==
                    competitionTestAssignmentId)
                .OrderBy(x => x.Rank)
                .ToListAsync();

        var isAvailable =
            assignment.Status ==
                CompetitionStatus.Ranked;

        var myResult =
            results.FirstOrDefault(x =>
                x.StudentRoleId ==
                studentRole.StudentRoleId);

        return new StudentCompetitionLeaderboardDto
        {
            CompetitionTestAssignmentId =
                assignment
                    .CompetitionTestAssignmentId,

            CompetitionTitle =
                assignment.Title,

            CompetitionTestTitle =
                assignment.CompetitionTest.Title,

            TestType =
                assignment.CompetitionTest.TestType,

            CourseType =
                assignment.CompetitionTest.CourseType,

            ClassRoomName =
                assignment.ClassRoom.ClassName,

            Status =
                assignment.Status,

            DeadlineAt =
                assignment.DeadlineAt,

            RankedAt =
                assignment.RankedAt,

            IsLeaderboardAvailable =
                isAvailable,

            MyRank =
                isAvailable
                    ? myResult?.Rank
                    : null,

            MyScore =
                isAvailable
                    ? myResult?.FinalScore
                    : null,

            MyInternalScore =
                isAvailable && myResult?.Attempt != null
                    ? Math.Round(
                        myResult.Attempt.Answers.Sum(x =>
                            x.PointEarned),
                        2)
                    : null,

            Rankings =
                isAvailable
                    ? results.Select(result =>
                        new StudentCompetitionLeaderboardItemDto
                        {
                            StudentRoleId =
                                result.StudentRoleId,

                            StudentName =
                                result.StudentRole.User
                                    .FullName,

                            AvatarUrl =
                                result.StudentRole.User
                                    .AvatarUrl,

                            Rank =
                                result.Rank,

                            FinalScore =
                                result.FinalScore,

                            InternalScore =
                                Math.Round(
                                    result.Attempt.Answers.Sum(x =>
                                        x.PointEarned),
                                    2),

                            CorrectAnswers =
                                CountCorrectAnswers(
                                    result.Attempt.Answers),

                            TotalQuestions =
                                result.TotalQuestions,

                            CompletionSeconds =
                                result.CompletionSeconds,

                            SubmittedAt =
                                result.SubmittedAt,

                            IsCurrentStudent =
                                result.StudentRoleId ==
                                studentRole.StudentRoleId
                        })
                        .ToList()
                    : new()
        };
    }
    public async Task<CompetitionAttemptResultDto>
    GetMyResultAsync(
        int studentUserId,
        int competitionTestAttemptId)
    {
        var studentRole =
            await _unitOfWork.StudentRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId ==
                        studentUserId &&
                    !x.IsDeleted);

        if (studentRole == null)
        {
            throw new Exception(
                "Student not found");
        }

        var attempt =
            await _unitOfWork
                .CompetitionTestAttempts
                .GetResultAsync(
                    competitionTestAttemptId);

        if (attempt == null ||
            attempt.StudentRoleId !=
                studentRole.StudentRoleId)
        {
            throw new Exception(
                "Competition result not found");
        }

        if (attempt.SubmittedAt == null)
        {
            throw new Exception(
                "Competition attempt has not been submitted");
        }

        var now =
            DateTime.UtcNow;

        var canViewResult =
            now >= attempt.Assignment.DeadlineAt ||
            attempt.Assignment.Status ==
                CompetitionStatus.Ranked;

        if (!canViewResult)
        {
            throw new Exception(
                "Competition result will be available after the deadline");
        }

        return _resultMapper.Build(
            attempt);
    }
    private static void GradeListeningAnswers(
    SubmitCompetitionAttemptDto dto,
    CompetitionTestAttempt attempt,
    CompetitionTest competitionTest,
    List<Domain.Entities.Listening.ListeningQuestion>
        questions,
    ref int correctAnswers,
    ref double score)
    {
        foreach (var question in questions)
        {
            var submitted =
                dto.Answers.FirstOrDefault(x =>
                    x.ListeningQuestionId ==
                    question.ListeningQuestionId);

            var selectedOption =
                submitted?.SelectedListeningOptionId ==
                    null
                    ? null
                    : question.Options.FirstOrDefault(x =>
                        x.ListeningOptionId ==
                        submitted
                            .SelectedListeningOptionId);

            var isCorrect =
                selectedOption?.IsCorrect == true;

            var point =
                GetListeningQuestionPoint(
                    competitionTest,
                    question.ListeningQuestionId);

            var earnedPoint =
                isCorrect
                    ? point
                    : 0;

            if (isCorrect)
            {
                correctAnswers++;
                score += earnedPoint;
            }

            attempt.Answers.Add(
                new CompetitionTestAnswer
                {
                    CompetitionTestAttemptId =
                        attempt.CompetitionTestAttemptId,

                    ListeningQuestionId =
                        question.ListeningQuestionId,

                    SelectedListeningOptionId =
                        selectedOption?
                            .ListeningOptionId,

                    IsCorrect =
                        isCorrect,

                    PointEarned =
                        Math.Round(
                            earnedPoint,
                            4)
                });
        }
    }
    private static void GradeReadingAnswers(
    SubmitCompetitionAttemptDto dto,
    CompetitionTestAttempt attempt,
    CompetitionTest competitionTest,
    List<Domain.Entities.Reading.ReadingQuestion>
        questions,
    ref int correctAnswers,
    ref double score)
    {
        foreach (var question in questions)
        {
            var submitted =
                dto.Answers.FirstOrDefault(x =>
                    x.ReadingQuestionId ==
                    question.ReadingQuestionId);

            var selectedOption =
                submitted?.SelectedReadingOptionId ==
                    null
                    ? null
                    : question.Options.FirstOrDefault(x =>
                        x.ReadingOptionId ==
                        submitted
                            .SelectedReadingOptionId);

            var isCorrect =
                selectedOption?.IsCorrect == true;

            var point =
                GetReadingQuestionPoint(
                    competitionTest,
                    question.ReadingQuestionId);

            var earnedPoint =
                isCorrect
                    ? point
                    : 0;

            if (isCorrect)
            {
                correctAnswers++;
                score += earnedPoint;
            }

            attempt.Answers.Add(
                new CompetitionTestAnswer
                {
                    CompetitionTestAttemptId =
                        attempt.CompetitionTestAttemptId,

                    ReadingQuestionId =
                        question.ReadingQuestionId,

                    SelectedReadingOptionId =
                        selectedOption?
                            .ReadingOptionId,

                    IsCorrect =
                        isCorrect,

                    PointEarned =
                        Math.Round(
                            earnedPoint,
                            4)
                });
        }
    }
    private static void SaveWritingAnswers(
    SubmitCompetitionAttemptDto dto,
    CompetitionTestAttempt attempt,
    List<CompetitionTestWritingTask> writingLinks)
    {
        foreach (var link in writingLinks)
        {
            var submitted =
                dto.Answers.FirstOrDefault(x =>
                    x.WritingTaskId ==
                    link.WritingTaskId);

            attempt.Answers.Add(
                new CompetitionTestAnswer
                {
                    CompetitionTestAttemptId =
                        attempt.CompetitionTestAttemptId,

                    WritingTaskId =
                        link.WritingTaskId,

                    TextAnswer =
                        submitted?.TextAnswer?.Trim(),

                    IsCorrect =
                        false,

                    PointEarned =
                        0
                });
        }
    }
    private static void EnsureSpeakingAnswers(
    CompetitionTestAttempt attempt,
    List<CompetitionTestSpeakingTask> speakingLinks,
    List<CompetitionTestAnswer>
        existingSpeakingAnswers)
    {
        foreach (var link in speakingLinks)
        {
            var existing =
                existingSpeakingAnswers
                    .FirstOrDefault(x =>
                        x.SpeakingTaskId ==
                        link.SpeakingTaskId);

            if (existing != null)
            {
                continue;
            }

            attempt.Answers.Add(
                new CompetitionTestAnswer
                {
                    CompetitionTestAttemptId =
                        attempt.CompetitionTestAttemptId,

                    SpeakingTaskId =
                        link.SpeakingTaskId,

                    IsCorrect =
                        false,

                    PointEarned =
                        0,

                    TeacherFeedback =
                        "Student did not submit speaking audio"
                });
        }
    }
    private static double GetListeningQuestionPoint(
    CompetitionTest competitionTest,
    int listeningQuestionId)
    {
        foreach (var link in competitionTest.ListeningTasks)
        {
            var questions =
                link.ListeningTask.Questions;

            if (!questions.Any(x =>
                x.ListeningQuestionId ==
                listeningQuestionId))
            {
                continue;
            }

            return questions.Count == 0
                ? 0
                : Math.Round(
                    link.Point / questions.Count,
                    4);
        }

        return 0;
    }
    private static double GetReadingQuestionPoint(
    CompetitionTest competitionTest,
    int readingQuestionId)
    {
        foreach (var link in competitionTest.ReadingTasks)
        {
            var questions =
                link.ReadingTask.Questions;

            if (!questions.Any(x =>
                x.ReadingQuestionId ==
                readingQuestionId))
            {
                continue;
            }

            return questions.Count == 0
                ? 0
                : Math.Round(
                    link.Point / questions.Count,
                    4);
        }

        return 0;
    }
    private static void ValidateAttemptCanBeModified(
    CompetitionTestAttempt attempt,
    DateTime now)
    {
        if (attempt.Assignment.Status ==
            CompetitionStatus.Cancelled)
        {
            throw new Exception(
                "This competition has been cancelled");
        }

        if (attempt.Status !=
            CompetitionAttemptStatus.InProgress)
        {
            throw new Exception(
                "Only an in-progress competition attempt can be modified");
        }

        if (attempt.SubmittedAt != null)
        {
            throw new Exception(
                "This competition attempt has already been submitted");
        }

        if (now >= attempt.Assignment.DeadlineAt)
        {
            throw new Exception(
                "The competition deadline has expired");
        }

        if (now >= attempt.ExpiresAt)
        {
            throw new Exception(
                "Your competition attempt time has expired");
        }
    }
    private static CompetitionStatus
    ResolveCompetitionStatus(
        CompetitionStatus storedStatus,
        DateTime startsAt,
        DateTime deadlineAt,
        DateTime now)
    {
        if (storedStatus is
            CompetitionStatus.Cancelled or
            CompetitionStatus.WaitingForGrading or
            CompetitionStatus.Ranked or
            CompetitionStatus.Failed)
        {
            return storedStatus;
        }

        if (now >= deadlineAt)
        {
            return CompetitionStatus.Closed;
        }

        if (now >= startsAt)
        {
            return CompetitionStatus.Open;
        }

        return CompetitionStatus.Scheduled;
    }
    private static CompetitionAttemptStatus?
    ResolveAttemptStatus(
        CompetitionTestAttempt? attempt,
        CompetitionTestAssignment assignment,
        DateTime now)
    {
        if (attempt == null)
        {
            return null;
        }

        if (attempt.Status ==
                CompetitionAttemptStatus.InProgress &&
            (now >= attempt.ExpiresAt ||
             now >= assignment.DeadlineAt))
        {
            return CompetitionAttemptStatus.Expired;
        }

        return attempt.Status;
    }
    private static int GetTotalQuestionCount(
    CompetitionTest competitionTest)
    {
        return competitionTest.ListeningTasks
                   .Sum(x =>
                       x.ListeningTask.Questions.Count)
               +
               competitionTest.ReadingTasks
                   .Sum(x =>
                       x.ReadingTask.Questions.Count)
               +
               competitionTest.WritingTasks.Count
               +
               competitionTest.SpeakingTasks.Count;
    }

    private static int CountCorrectAnswers(
        IEnumerable<CompetitionTestAnswer> answers)
    {
        return answers.Count(answer =>
            answer.IsCorrect ||
            ((answer.WritingTaskId != null ||
              answer.SpeakingTaskId != null) &&
             answer.PointEarned > 0));
    }
}
