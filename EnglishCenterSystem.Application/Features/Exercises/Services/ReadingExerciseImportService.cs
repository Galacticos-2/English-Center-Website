using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Reading.Services;
using EnglishCenterSystem.Application.Features.Notifications.DTOs;
using EnglishCenterSystem.Application.Features.Notifications.Services;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class ReadingExerciseImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ReadingCoreImportService _readingCoreImportService;
    private readonly IQuestionExplanationService _questionExplanationService;
    private readonly IMinioService _minioService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<ReadingExerciseImportService> _logger;

    public ReadingExerciseImportService(
        IUnitOfWork unitOfWork,
        ReadingCoreImportService readingCoreImportService,
        IQuestionExplanationService questionExplanationService,
        IMinioService minioService,
        NotificationService notificationService,
        ILogger<ReadingExerciseImportService> logger)
    {
        _unitOfWork = unitOfWork;
        _readingCoreImportService = readingCoreImportService;
        _questionExplanationService = questionExplanationService;
        _minioService = minioService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<int> ImportReadingExerciseAsync(
        int teacherUserId,
        Stream questionStream,
        string questionFileName,
        ImportReadingExerciseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new Exception("Reading exercise title is required");
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

        var readingTasks =
            await _readingCoreImportService.CreateReadingTasksAsync(
                teacherUserId,
                dto.Title,
                dto.Description,
                dto.CourseType,
                questionStream,
                questionFileName);

        if (dto.GenerateExplanations)
        {
            foreach (var readingTask in readingTasks)
            {
                await _questionExplanationService
                    .AddReadingExplanationsAsync(readingTask);
            }
        }

        var exercise = new Exercise
        {
            TeacherUserId = teacherUserId,
            ExerciseSectionId = dto.ExerciseSectionId,
            Title = dto.Title,
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim(),
            CourseType = dto.CourseType,
            Skill = SkillType.Reading,
            IsActive = true,
            ReadingTasks = readingTasks
                .Select((task, index) =>
                    new ExerciseReadingTask
                    {
                        ReadingTask = task,
                        OrderIndex = index + 1
                    })
                .ToList()
        };

        if (questionStream.CanSeek)
        {
            questionStream.Position = 0;
        }

        var sourceObjectName = $"exercises/source/{Guid.NewGuid():N}_" +
            StoragePathHelper.SanitizeFileName(questionFileName);
        exercise.SourceFileUrl = await _minioService.UploadFileAsync(
            questionStream,
            sourceObjectName,
            StoragePathHelper.GetDocumentContentType(questionFileName));
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
