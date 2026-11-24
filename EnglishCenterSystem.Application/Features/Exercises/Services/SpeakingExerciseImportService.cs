using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Speaking.Services;
using EnglishCenterSystem.Application.Features.Notifications.DTOs;
using EnglishCenterSystem.Application.Features.Notifications.Services;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using EnglishCenterSystem.Application.Common.Enums;
using Microsoft.Extensions.Logging;
namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class SpeakingExerciseImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SpeakingCoreImportService _speakingCoreImportService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<SpeakingExerciseImportService> _logger;

    public SpeakingExerciseImportService(
        IUnitOfWork unitOfWork,
        SpeakingCoreImportService speakingCoreImportService,
        NotificationService notificationService,
        ILogger<SpeakingExerciseImportService> logger)
    {
        _unitOfWork = unitOfWork;
        _speakingCoreImportService = speakingCoreImportService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<int>
    ImportSpeakingExerciseAsync(
        int teacherUserId,
        Stream questionStream,
        string questionFileName,
        string contentType,
        ImportSpeakingExerciseDto dto)
    {
        if (string.IsNullOrWhiteSpace(
            dto.Title))
        {
            throw new Exception(
                "Speaking exercise title is required");
        }

        if (dto.MaxAttempts <= 0)
        {
            throw new Exception(
                "Max attempts must be greater than 0");
        }

        var section =
            await _unitOfWork.ExerciseSections
                .FirstOrDefaultAsync(x =>
                    x.ExerciseSectionId ==
                        dto.ExerciseSectionId &&
                    x.TeacherUserId ==
                        teacherUserId &&
                    x.ClassRoomId ==
                        dto.ClassRoomId &&
                    x.IsActive);

        if (section == null)
        {
            throw new Exception(
                "Exercise section not found");
        }

        var importCode =
            Guid.NewGuid()
                .ToString("N")[..8];

        var storageFolderName =
            $"{dto.Title}-{importCode}";

        var speakingTasks =
            await _speakingCoreImportService
                .CreateSpeakingTasksAsync(
                    teacherUserId,
                    ImportStorageModule.Exercise,
                    storageFolderName,
                    questionStream,
                    questionFileName,
                    contentType,
                    dto.Description,
                    dto.CourseType,
                    dto.PreparationTimeSeconds,
                    dto.SpeakingTimeSeconds);

        if (!speakingTasks.Any())
        {
            throw new Exception(
                "Speaking file has no valid speaking tasks");
        }

        var pointPerTask =
            Math.Round(
                10.0 / speakingTasks.Count,
                2);

        var speakingTaskLinks =
            speakingTasks
                .Select((task, index) =>
                    new ExerciseSpeakingTask
                    {
                        SpeakingTask = task,
                        OrderIndex = index + 1,
                        Point = pointPerTask
                    })
                .ToList();

        var exercise = new Exercise
        {
            TeacherUserId =
                teacherUserId,

            ExerciseSectionId =
                dto.ExerciseSectionId,

            Title =
                dto.Title.Trim(),

            Description =
                string.IsNullOrWhiteSpace(
                    dto.Description)
                    ? null
                    : dto.Description.Trim(),

            CourseType =
                dto.CourseType,

            Skill =
                SkillType.Speaking,

            SourceFileUrl = speakingTasks.First().PdfUrl,

            SourceFileObjectName = speakingTasks.First().PdfObjectName,

            IsActive = true,

            SpeakingTasks =
                speakingTaskLinks
        };

        await _unitOfWork.Exercises
            .AddAsync(exercise);

        await _unitOfWork
            .SaveChangesAsync();

        var assignment =
            new ExerciseAssignment
            {
                ExerciseId =
                    exercise.ExerciseId,

                ClassRoomId =
                    dto.ClassRoomId,

                MaxAttempts =
                    dto.MaxAttempts,

                DeadlineAt = dto.DeadlineAt,

                IsActive = true
            };

        await _unitOfWork
            .ExerciseAssignments
            .AddAsync(assignment);

        await _unitOfWork
            .SaveChangesAsync();

        await CreateExerciseNotificationSafelyAsync(
            teacherUserId,
            exercise.Title,
            dto.ClassRoomId,
            assignment.ExerciseAssignmentId);

        return exercise.ExerciseId;
    }

    private async Task CreateExerciseNotificationSafelyAsync(
        int teacherUserId,
        string exerciseTitle,
        int classRoomId,
        int exerciseAssignmentId)
    {
        try
        {
            await _notificationService.CreateForClassAsync(
                new CreateClassNotificationDto
                {
                    CreatedByUserId = teacherUserId,
                    ClassRoomId = classRoomId,
                    Type = NotificationType.ExerciseAssigned,
                    RelatedEntityId = exerciseAssignmentId,
                    Title = "New exercise assigned",
                    Message = $"Exercise \"{exerciseTitle}\" has been assigned to the class.",
                    TeacherActionUrl = "/teacher/exercises",
                    StudentActionUrl = "/student/exercises"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not create exercise notification for ExerciseAssignmentId {ExerciseAssignmentId}",
                exerciseAssignmentId);
        }
    }
}
