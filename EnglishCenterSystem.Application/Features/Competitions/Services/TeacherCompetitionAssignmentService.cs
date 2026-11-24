using EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Teacher;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

using EnglishCenterSystem.Application
    .Features.Notifications.Services;

using Microsoft.Extensions.Logging;
namespace EnglishCenterSystem.Application
    .Features.Competitions.Services;

public class TeacherCompetitionAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly NotificationService
    _notificationService;

    private readonly ILogger<
        TeacherCompetitionAssignmentService>
        _logger;
    private readonly ICompetitionDeadlineQueueService
        _competitionDeadlineQueueService;
    private readonly IMinioService
    _minioService;
    private readonly IQuestionExplanationService
        _questionExplanationService;
    public TeacherCompetitionAssignmentService(
    IUnitOfWork unitOfWork,
    ICompetitionDeadlineQueueService
        competitionDeadlineQueueService,
    IMinioService
        minioService,
    NotificationService
        notificationService,
    ILogger<
        TeacherCompetitionAssignmentService>
        logger,
    IQuestionExplanationService
        questionExplanationService)
    {
        _unitOfWork =
            unitOfWork;

        _competitionDeadlineQueueService =
            competitionDeadlineQueueService;

        _notificationService =
            notificationService;
        _minioService =
    minioService;
        _logger =
            logger;
        _questionExplanationService =
            questionExplanationService;
    }
    public async Task UpdateAsync(
    int teacherUserId,
    int competitionTestAssignmentId,
    UpdateCompetitionAssignmentDto dto)
    {
        if (string.IsNullOrWhiteSpace(
            dto.Title))
        {
            throw new Exception(
                "Competition title is required");
        }

        var assignment =
            await _unitOfWork
                .CompetitionTestAssignments
                .Query()
                .FirstOrDefaultAsync(x =>
                    x.CompetitionTestAssignmentId ==
                        competitionTestAssignmentId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (assignment == null)
        {
            throw new Exception(
                "Competition assignment not found");
        }

        var startsAtUtc =
            dto.StartsAt.UtcDateTime;

        var deadlineAtUtc =
            dto.DeadlineAt.UtcDateTime;

        if (deadlineAtUtc <=
            startsAtUtc)
        {
            throw new Exception(
                "Deadline must be after start time");
        }

        if (deadlineAtUtc <=
            DateTime.UtcNow)
        {
            throw new Exception(
                "Deadline must be in the future");
        }

        if (dto.DurationMinutes <= 0)
        {
            throw new Exception(
                "Duration must be greater than 0");
        }

        if (dto.DurationMinutes > 24 * 60)
        {
            throw new Exception(
                "Duration cannot exceed 24 hours");
        }

        var availableMinutes =
            (deadlineAtUtc -
             startsAtUtc).TotalMinutes;

        if (dto.DurationMinutes >
            availableMinutes)
        {
            throw new Exception(
                "Duration cannot be longer than the competition time window");
        }

        if (dto.GenerateExplanations)
        {
            foreach (var listeningTask in assignment.CompetitionTest
                .ListeningTasks.Select(x => x.ListeningTask))
            {
                await _questionExplanationService
                    .AddListeningExplanationsAsync(listeningTask);
            }

            foreach (var readingTask in assignment.CompetitionTest
                .ReadingTasks.Select(x => x.ReadingTask))
            {
                await _questionExplanationService
                    .AddReadingExplanationsAsync(readingTask);
            }
        }

        if (!string.IsNullOrWhiteSpace(
            assignment.RankingJobId))
        {
            _competitionDeadlineQueueService
                .DeleteScheduledJob(
                    assignment.RankingJobId);
        }

        assignment.Title =
            dto.Title.Trim();

        assignment.Description =
            string.IsNullOrWhiteSpace(
                dto.Description)
                ? null
                : dto.Description.Trim();

        assignment.StartsAt =
            startsAtUtc;

        assignment.DeadlineAt =
            deadlineAtUtc;

        assignment.DurationMinutes =
            dto.DurationMinutes;

        assignment.Status =
            ResolveInitialStatus(
                startsAtUtc,
                deadlineAtUtc);

        assignment.RankingJobId =
            _competitionDeadlineQueueService
                .ScheduleDeadline(
                    assignment
                        .CompetitionTestAssignmentId,
                    deadlineAtUtc);

        assignment.UpdatedAt =
            DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task<TeacherCompetitionAssignmentOptionsDto>
        GetOptionsAsync(int teacherUserId)
    {
        var competitionTests =
            await _unitOfWork.CompetitionTests
                .QueryBasic()
                .AsNoTracking()
                .Where(x =>
                    x.TeacherUserId ==
                        teacherUserId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderBy(x => x.Title)
                .Select(x =>
                    new CompetitionTestOptionDto
                    {
                        CompetitionTestId =
                            x.CompetitionTestId,

                        Title =
                            x.Title,

                        TestType =
                            x.TestType,

                        CourseType =
                            x.CourseType,

                        TotalPoints =
                            x.TotalPoints
                    })
                .ToListAsync();

        var teacherRole =
            await _unitOfWork
                .TeacherRoleRepository
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId ==
                        teacherUserId &&
                    !x.IsDeleted);

        var classes =
            teacherRole?.TeacherClasses
                .Where(x =>
                    !x.ClassRoom.IsDeleted &&
                    x.ClassRoom.IsOpen)
                .OrderBy(x =>
                    x.ClassRoom.ClassName)
                .Select(x =>
                {
                    CourseType? parsedCourseType =
                        null;

                    if (Enum.TryParse<CourseType>(
                        x.ClassRoom.Course.CourseType,
                        true,
                        out var courseType))
                    {
                        parsedCourseType =
                            courseType;
                    }

                    return new CompetitionClassOptionDto
                    {
                        ClassRoomId =
                            x.ClassRoomId,

                        ClassName =
                            x.ClassRoom.ClassName,

                        CourseName =
                            x.ClassRoom.Course.Name,

                        CourseType =
                            parsedCourseType
                    };
                })
                .ToList()
            ?? new List<CompetitionClassOptionDto>();

        return new TeacherCompetitionAssignmentOptionsDto
        {
            CompetitionTests =
                competitionTests,

            Classes =
                classes,

            ServerTime =
                DateTime.UtcNow
        };
    }

    public async Task<int> CreateAsync(
        int teacherUserId,
        CreateCompetitionAssignmentDto dto)
    {
        ValidateCreateDto(dto);

        var competitionTest =
            await _unitOfWork.CompetitionTests
                .QueryBasic()
                .FirstOrDefaultAsync(x =>
                    x.CompetitionTestId ==
                        dto.CompetitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId &&
                    x.IsActive &&
                    !x.IsDeleted);

        if (competitionTest == null)
        {
            throw new Exception(
                "Competition test not found or does not belong to this teacher");
        }

        var teacherRole =
            await _unitOfWork
                .TeacherRoleRepository
                .Query()
                .FirstOrDefaultAsync(x =>
                    x.UserId ==
                        teacherUserId &&
                    !x.IsDeleted);

        if (teacherRole == null)
        {
            throw new Exception(
                "Teacher role not found");
        }

        var teacherClass =
            teacherRole.TeacherClasses
                .FirstOrDefault(x =>
                    x.ClassRoomId ==
                    dto.ClassRoomId);

        if (teacherClass == null)
        {
            throw new Exception(
                "You are not assigned to this class");
        }

        var classRoom =
            teacherClass.ClassRoom;

        if (classRoom.IsDeleted ||
            !classRoom.IsOpen)
        {
            throw new Exception(
                "Class room is not available");
        }

        if (!Enum.TryParse<CourseType>(
            classRoom.Course.CourseType,
            true,
            out var classCourseType))
        {
            throw new Exception(
                "Class course type is invalid");
        }

        if (classCourseType !=
            competitionTest.CourseType)
        {
            throw new Exception(
                "The competition test course type does not match the class course type");
        }

        var startsAtUtc =
            dto.StartsAt.UtcDateTime;

        var deadlineAtUtc =
            dto.DeadlineAt.UtcDateTime;

        var assignment =
            new CompetitionTestAssignment
            {
                CompetitionTestId =
                    competitionTest
                        .CompetitionTestId,

                TeacherUserId =
                    teacherUserId,

                ClassRoomId =
                    classRoom.ClassRoomId,

                Title =
                    dto.Title.Trim(),

                Description =
                    string.IsNullOrWhiteSpace(
                        dto.Description)
                        ? null
                        : dto.Description.Trim(),

                StartsAt =
                    startsAtUtc,

                DeadlineAt =
                    deadlineAtUtc,

                DurationMinutes =
                    dto.DurationMinutes,

                Status =
                    ResolveInitialStatus(
                        startsAtUtc,
                        deadlineAtUtc),

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };

        await _unitOfWork
            .CompetitionTestAssignments
            .AddAsync(assignment);

        await _unitOfWork.SaveChangesAsync();

        try
        {
            var jobId =
                _competitionDeadlineQueueService
                    .ScheduleDeadline(
                        assignment
                            .CompetitionTestAssignmentId,
                        assignment.DeadlineAt);

            assignment.RankingJobId =
                jobId;

            assignment.UpdatedAt =
                DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await CreateCompetitionNotificationSafelyAsync(
    teacherUserId,
    classRoom.ClassRoomId,
    classRoom.ClassName,
    assignment.Title,
    assignment
        .CompetitionTestAssignmentId);
        }
        catch
        {
            assignment.Status =
                CompetitionStatus.Failed;

            assignment.UpdatedAt =
                DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            throw new Exception(
                "Competition was created but scheduling the deadline job failed");
        }

        return assignment
            .CompetitionTestAssignmentId;
    }
    private async Task
    CreateCompetitionNotificationSafelyAsync(
        int teacherUserId,
        int classRoomId,
        string classRoomName,
        string competitionTitle,
        int competitionTestAssignmentId)
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
                                .CompetitionAssigned,

                        RelatedEntityId =
                            competitionTestAssignmentId,

                        Title =
                            "New competition created",

                        Message =
                            $"Competition " +
                            $"\"{competitionTitle}\" " +
                            $"has been created for class " +
                            $"{classRoomName}.",

                        TeacherActionUrl =
                            "/teacher/competitions",

                        StudentActionUrl =
                            "/student/competitions"
                    });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not create competition notification for CompetitionTestAssignmentId {CompetitionTestAssignmentId}",
                competitionTestAssignmentId);
        }
    }
    public async Task<List<TeacherCompetitionAssignmentDto>>
        GetMyAssignmentsAsync(
            int teacherUserId)
    {
        var now =
            DateTime.UtcNow;

        var assignments =
            await _unitOfWork
                .CompetitionTestAssignments
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.TeacherUserId ==
                        teacherUserId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderByDescending(x =>
                    x.CreatedAt)
                .ToListAsync();

        return assignments
            .Select(x =>
            {
                var participantCount =
                    x.Attempts.Count;

                var submittedCount =
                    x.Attempts.Count(a =>
                        a.SubmittedAt != null);

                return new TeacherCompetitionAssignmentDto
                {
                    CompetitionTestAssignmentId =
                        x.CompetitionTestAssignmentId,

                    CompetitionTestId =
                        x.CompetitionTestId,

                    CompetitionTestTitle =
                        x.CompetitionTest.Title,

                    Title =
                        x.Title,

                    Description =
                        x.Description,

                    ClassRoomId =
                        x.ClassRoomId,

                    ClassRoomName =
                        x.ClassRoom.ClassName,

                    CourseName =
                        x.ClassRoom.Course.Name,

                    TestType =
                        x.CompetitionTest.TestType,

                    CourseType =
                        x.CompetitionTest.CourseType,

                    StartsAt =
                        x.StartsAt,

                    DeadlineAt =
                        x.DeadlineAt,

                    DurationMinutes =
                        x.DurationMinutes,

                    Status =
                        ResolveStatus(
                            x.Status,
                            x.StartsAt,
                            x.DeadlineAt,
                            now),

                    ParticipantCount =
                        participantCount,

                    SubmittedCount =
                        submittedCount,

                    RankedAt =
                        x.RankedAt,

                    CanCancel =
                        CanCancel(
                            x.Status,
                            x.StartsAt,
                            participantCount,
                            now),

                    CreatedAt =
                        x.CreatedAt,

                    ServerTime =
                        now
                };
            })
            .ToList();
    }

    public async Task<TeacherCompetitionAssignmentDetailDto>
        GetDetailAsync(
            int teacherUserId,
            int competitionTestAssignmentId)
    {
        var assignment =
            await _unitOfWork
                .CompetitionTestAssignments
                .GetDetailAsync(
                    competitionTestAssignmentId);

        if (assignment == null ||
            assignment.TeacherUserId !=
                teacherUserId)
        {
            throw new Exception(
                "Competition assignment not found");
        }

        var totalStudents =
            assignment.ClassRoom.Students
                .Count(x => !x.IsDeleted);

        var participantCount =
            assignment.Attempts.Count;

        var submittedCount =
            assignment.Attempts.Count(x =>
                x.SubmittedAt != null);

        var now =
            DateTime.UtcNow;

        return new TeacherCompetitionAssignmentDetailDto
        {
            CompetitionTestAssignmentId =
                assignment
                    .CompetitionTestAssignmentId,

            CompetitionTestId =
                assignment.CompetitionTestId,

            CompetitionTestTitle =
                assignment.CompetitionTest.Title,

            Title =
                assignment.Title,

            Description =
                assignment.Description,

            ClassRoomId =
                assignment.ClassRoomId,

            ClassRoomName =
                assignment.ClassRoom.ClassName,

            CourseName =
                assignment.ClassRoom.Course.Name,

            TeacherName =
                assignment.TeacherUser.FullName,

            TestType =
                assignment.CompetitionTest.TestType,

            CourseType =
                assignment.CompetitionTest.CourseType,

            TotalPoints =
                assignment.CompetitionTest.TotalPoints,

            StartsAt =
                assignment.StartsAt,

            DeadlineAt =
                assignment.DeadlineAt,

            DurationMinutes =
                assignment.DurationMinutes,

            Status =
                ResolveStatus(
                    assignment.Status,
                    assignment.StartsAt,
                    assignment.DeadlineAt,
                    now),

            TotalStudentsInClass =
                totalStudents,

            ParticipantCount =
                participantCount,

            SubmittedCount =
                submittedCount,

            NotStartedCount =
                Math.Max(
                    0,
                    totalStudents -
                    participantCount),

            RankedAt =
                assignment.RankedAt,

            RankingJobId =
                assignment.RankingJobId,

            CanCancel =
                CanCancel(
                    assignment.Status,
                    assignment.StartsAt,
                    participantCount,
                    now),

            CreatedAt =
                assignment.CreatedAt,

            ServerTime =
                now
        };
    }
    private static List<string?>
    GetCompetitionTestMinioObjectNames(
        CompetitionTest competitionTest)
    {
        var objectNames =
            new List<string?>
            {
            competitionTest
                .SourceFileObjectName
            };

        foreach (var listeningLink in
            competitionTest.ListeningTasks)
        {
            var listeningTask =
                listeningLink.ListeningTask;

            if (listeningTask == null)
            {
                continue;
            }

            objectNames.Add(
                listeningTask
                    .SourceQuestionFileObjectName);

            objectNames.Add(
                listeningTask
                    .AudioObjectName);

            objectNames.AddRange(
                listeningTask.Questions
                    .Select(question =>
                        question
                            .PromptImageObjectName));
        }

        foreach (var speakingLink in
            competitionTest.SpeakingTasks)
        {
            var speakingTask =
                speakingLink.SpeakingTask;

            if (speakingTask == null)
            {
                continue;
            }

            objectNames.Add(
                speakingTask.PdfObjectName);
        }

        return objectNames;
    }
    public async Task
    CleanupFailedReplacementImportAsync(
        int teacherUserId,
        int newCompetitionTestId)
    {
        var competitionTest =
            await _unitOfWork
                .CompetitionTests
                .QueryBasic()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.CompetitionTestId ==
                        newCompetitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (competitionTest == null)
        {
            return;
        }

        /*
         * Chỉ được cleanup nếu đề mới chưa được
         * assignment nào sử dụng.
         *
         * Tránh trường hợp replace thực tế đã thành công
         * một phần nhưng backend lại xóa nhầm đề đang dùng.
         */
        var hasAssignments =
            await _unitOfWork
                .CompetitionTestAssignments
                .QueryBasic()
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.CompetitionTestId ==
                        newCompetitionTestId);

        if (hasAssignments)
        {
            throw new Exception(
                "The imported replacement test is already used by an assignment and cannot be cleaned up automatically");
        }

        /*
         * Dùng luồng hard delete đầy đủ đã tạo:
         *
         * task links
         * notifications nếu có
         * assignments nếu có
         * CompetitionTest
         */
        await _unitOfWork
            .CompetitionTestAssignments
            .HardDeleteCompetitionTestAsync(
                newCompetitionTestId,
                teacherUserId);
    }
    public async Task HardDeleteAsync(
    int teacherUserId,
    int competitionTestAssignmentId)
    {
        var selectedAssignment =
            await _unitOfWork
                .CompetitionTestAssignments
                .QueryBasic()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.CompetitionTestAssignmentId ==
                        competitionTestAssignmentId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (selectedAssignment == null)
        {
            throw new Exception(
                "Competition assignment not found");
        }

        var competitionTestId =
            selectedAssignment
                .CompetitionTestId;
        var competitionTest =
    await _unitOfWork
        .CompetitionTests
        .Query()
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(x =>
            x.CompetitionTestId ==
                competitionTestId &&
            x.TeacherUserId ==
                teacherUserId);

        if (competitionTest == null)
        {
            throw new Exception(
                "Competition test not found");
        }
        var minioObjectNames =
    GetCompetitionTestMinioObjectNames(
        competitionTest);

        /*
         * File đề tổng:
         * competitions/source/...
         */
        minioObjectNames.Add(
            competitionTest
                .SourceFileObjectName);

        /*
         * Listening:
         * competitions/listening/.../source/...
         * competitions/listening/.../audio/...
         */
        foreach (var listeningLink in
            competitionTest.ListeningTasks)
        {
            var listeningTask =
                listeningLink.ListeningTask;

            if (listeningTask == null)
            {
                continue;
            }

            minioObjectNames.Add(
                listeningTask
                    .SourceQuestionFileObjectName);

            minioObjectNames.Add(
                listeningTask
                    .AudioObjectName);

            minioObjectNames.AddRange(
                listeningTask.Questions
                    .Select(question =>
                        question
                            .PromptImageObjectName));
        }

        /*
         * Speaking:
         * competitions/speaking/.../source/...
         */
        foreach (var speakingLink in
            competitionTest.SpeakingTasks)
        {
            var speakingTask =
                speakingLink.SpeakingTask;

            if (speakingTask == null)
            {
                continue;
            }

            minioObjectNames.Add(
                speakingTask.PdfObjectName);
        }
        /*
         * Một CompetitionTest có thể được giao cho nhiều lớp.
         * Phải lấy tất cả assignment dùng đề này để hủy
         * toàn bộ RankingJobId trước khi xóa database.
         */
        var assignments =
            await _unitOfWork
                .CompetitionTestAssignments
                .QueryBasic()
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        competitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId)
                .ToListAsync();

        foreach (var assignment in assignments)
        {
            if (string.IsNullOrWhiteSpace(
                assignment.RankingJobId))
            {
                continue;
            }

            _competitionDeadlineQueueService
                .DeleteScheduledJob(
                    assignment.RankingJobId);
        }

        await _unitOfWork
            .CompetitionTestAssignments
            .HardDeleteCompetitionTestAsync(
                competitionTestId,
                teacherUserId);
        await DeleteMinioObjectsSafelyAsync(
    minioObjectNames,
    competitionTestId);
    }
    public async Task ReplaceCompetitionTestAsync(
    int teacherUserId,
    int oldCompetitionTestId,
    int newCompetitionTestId)
    {
        /*
         * Phải lấy object name của đề cũ trước khi
         * repository xóa đề cũ khỏi database.
         */
        var oldCompetitionTest =
            await _unitOfWork
                .CompetitionTests
                .Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.CompetitionTestId ==
                        oldCompetitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (oldCompetitionTest == null)
        {
            throw new Exception(
                "Old competition test not found");
        }

        var oldMinioObjectNames =
            GetCompetitionTestMinioObjectNames(
                oldCompetitionTest);

        var assignments =
            await _unitOfWork
                .CompetitionTestAssignments
                .QueryBasic()
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        oldCompetitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId)
                .ToListAsync();

        /*
         * Hủy RankingJob cũ.
         */
        foreach (var assignment in assignments)
        {
            if (string.IsNullOrWhiteSpace(
                assignment.RankingJobId))
            {
                continue;
            }

            _competitionDeadlineQueueService
                .DeleteScheduledJob(
                    assignment.RankingJobId);
        }

        /*
         * Repository:
         * - xóa attempt/answer/result cũ;
         * - chuyển assignment sang đề mới;
         * - xóa link task đề cũ;
         * - xóa CompetitionTest cũ;
         * - commit transaction.
         */
        await _unitOfWork
            .CompetitionTestAssignments
            .ReplaceCompetitionTestAsync(
                oldCompetitionTestId,
                newCompetitionTestId,
                teacherUserId);

        /*
         * Replace database đã thành công thì
         * xóa toàn bộ file MinIO của đề cũ.
         */
        await DeleteMinioObjectsSafelyAsync(
            oldMinioObjectNames,
            oldCompetitionTestId);

        /*
         * Tạo lại RankingJob cho các assignment
         * đang sử dụng đề mới.
         */
        foreach (var assignment in assignments)
        {
            assignment.RankingJobId =
                _competitionDeadlineQueueService
                    .ScheduleDeadline(
                        assignment
                            .CompetitionTestAssignmentId,
                        assignment.DeadlineAt);

            assignment.UpdatedAt =
                DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task CancelAsync(
        int teacherUserId,
        int competitionTestAssignmentId)
    {
        var assignment =
            await _unitOfWork
                .CompetitionTestAssignments
                .Query()
                .FirstOrDefaultAsync(x =>
                    x.CompetitionTestAssignmentId ==
                        competitionTestAssignmentId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (assignment == null)
        {
            throw new Exception(
                "Competition assignment not found");
        }

        if (assignment.Status ==
            CompetitionStatus.Cancelled)
        {
            return;
        }

        if (assignment.Status is
            CompetitionStatus.Closed or
            CompetitionStatus.WaitingForGrading or
            CompetitionStatus.Ranked)
        {
            throw new Exception(
                "This competition can no longer be cancelled");
        }

        var now =
            DateTime.UtcNow;

        if (now >= assignment.StartsAt)
        {
            throw new Exception(
                "A competition that has already started cannot be cancelled");
        }

        if (assignment.Attempts.Any())
        {
            throw new Exception(
                "Cannot cancel a competition that already has participants");
        }

        if (!string.IsNullOrWhiteSpace(
            assignment.RankingJobId))
        {
            _competitionDeadlineQueueService
                .DeleteScheduledJob(
                    assignment.RankingJobId);
        }

        assignment.Status =
            CompetitionStatus.Cancelled;

        assignment.IsActive =
            false;

        assignment.UpdatedAt =
            DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
    }

    private static void ValidateCreateDto(
        CreateCompetitionAssignmentDto dto)
    {
        if (dto.CompetitionTestId <= 0)
        {
            throw new Exception(
                "Competition test is required");
        }

        if (dto.ClassRoomId <= 0)
        {
            throw new Exception(
                "Class room is required");
        }

        if (string.IsNullOrWhiteSpace(
            dto.Title))
        {
            throw new Exception(
                "Competition title is required");
        }

        if (dto.DurationMinutes <= 0)
        {
            throw new Exception(
                "Duration must be greater than 0");
        }

        if (dto.DurationMinutes >
            24 * 60)
        {
            throw new Exception(
                "Duration cannot exceed 24 hours");
        }

        var startsAtUtc =
            dto.StartsAt.UtcDateTime;

        var deadlineAtUtc =
            dto.DeadlineAt.UtcDateTime;

        if (deadlineAtUtc <=
            startsAtUtc)
        {
            throw new Exception(
                "Deadline must be after start time");
        }

        if (deadlineAtUtc <=
            DateTime.UtcNow)
        {
            throw new Exception(
                "Deadline must be in the future");
        }

        var availableMinutes =
            (deadlineAtUtc -
             startsAtUtc).TotalMinutes;

        if (dto.DurationMinutes >
            availableMinutes)
        {
            throw new Exception(
                "Duration cannot be longer than the competition time window");
        }
    }

    private static CompetitionStatus
        ResolveInitialStatus(
            DateTime startsAt,
            DateTime deadlineAt)
    {
        var now =
            DateTime.UtcNow;

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

    private static CompetitionStatus
        ResolveStatus(
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
    private async Task
    DeleteMinioObjectsSafelyAsync(
        IEnumerable<string?> objectNames,
        int competitionTestId)
    {
        var distinctObjectNames =
            objectNames
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(
                    StringComparer.Ordinal)
                .ToList();

        foreach (var objectName in
            distinctObjectNames)
        {
            try
            {
                await _minioService
                    .DeleteFileAsync(
                        objectName);
            }
            catch (Exception ex)
            {
                /*
                 * Database đã xóa thành công nên không ném lỗi
                 * làm API báo xóa thất bại.
                 *
                 * Ghi log để có thể cleanup object MinIO
                 * thủ công sau này.
                 */
                _logger.LogError(
                    ex,
                    "Could not delete MinIO object {ObjectName} for CompetitionTestId {CompetitionTestId}",
                    objectName,
                    competitionTestId);
            }
        }
    }
    private static bool CanCancel(
        CompetitionStatus storedStatus,
        DateTime startsAt,
        int participantCount,
        DateTime now)
    {
        if (storedStatus is
            CompetitionStatus.Cancelled or
            CompetitionStatus.Closed or
            CompetitionStatus.WaitingForGrading or
            CompetitionStatus.Ranked)
        {
            return false;
        }

        return now < startsAt &&
               participantCount == 0;
    }
}
