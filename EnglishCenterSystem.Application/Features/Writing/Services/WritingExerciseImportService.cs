using EnglishCenterSystem.Application.Features.Writing.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Features.Notifications.DTOs;
using EnglishCenterSystem.Application.Features.Notifications.Services;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishCenterSystem.Application.Features.Writing.Services;

public class WritingExerciseImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly WritingCoreImportService _writingCoreImportService;
    private readonly IMinioService _minioService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<WritingExerciseImportService> _logger;

    public WritingExerciseImportService(
        IUnitOfWork unitOfWork,
        WritingCoreImportService writingCoreImportService,
        IMinioService minioService,
        NotificationService notificationService,
        ILogger<WritingExerciseImportService> logger)
    {
        _unitOfWork = unitOfWork;
        _writingCoreImportService = writingCoreImportService;
        _minioService = minioService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<int> ImportWritingExerciseAsync(
        int teacherUserId,
        Stream fileStream,
        string fileName,
        ImportWritingExerciseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new Exception("Writing exercise title is required");
        }

        if (dto.MaxAttempts <= 0)
        {
            throw new Exception("Max attempts must be greater than 0");
        }

        var section = await _unitOfWork.ExerciseSections
            .FirstOrDefaultAsync(x =>
                x.ExerciseSectionId == dto.ExerciseSectionId &&
                x.TeacherUserId == teacherUserId &&
                x.ClassRoomId == dto.ClassRoomId &&
                x.IsActive);

        if (section == null)
        {
            throw new Exception("Exercise section not found");
        }

        var writingTasks =
            await _writingCoreImportService.CreateWritingTasksAsync(
                teacherUserId,
                dto.CourseType,
                fileStream,
                fileName);

        var pointPerTask =
            Math.Round(10.0 / writingTasks.Count, 2);

        var exercise = new Exercise
        {
            TeacherUserId = teacherUserId,
            ExerciseSectionId = dto.ExerciseSectionId,
            Title = dto.Title,
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim(),
            CourseType = dto.CourseType,
            Skill = SkillType.Writing,
            IsActive = true,
            WritingTasks = writingTasks
                .Select((task, index) => new ExerciseWritingTask
                {
                    WritingTask = task,
                    OrderIndex = index + 1,
                    Point = pointPerTask
                })
                .ToList()
        };

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        var sourceObjectName = $"exercises/source/{Guid.NewGuid():N}_" +
            StoragePathHelper.SanitizeFileName(fileName);
        exercise.SourceFileUrl = await _minioService.UploadFileAsync(
            fileStream,
            sourceObjectName,
            StoragePathHelper.GetDocumentContentType(fileName));
        exercise.SourceFileObjectName = sourceObjectName;

        await _unitOfWork.Exercises.AddAsync(exercise);
        await _unitOfWork.SaveChangesAsync();

        var assignment = new ExerciseAssignment
        {
            ExerciseId = exercise.ExerciseId,
            ClassRoomId = dto.ClassRoomId,
            MaxAttempts = dto.MaxAttempts,
            DeadlineAt = dto.DeadlineAt,
            IsActive = true
        };

        await _unitOfWork.ExerciseAssignments.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

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
