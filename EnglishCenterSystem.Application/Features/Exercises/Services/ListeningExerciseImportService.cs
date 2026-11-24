using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Listening.Services;
using EnglishCenterSystem.Application.Features.Notifications.DTOs;
using EnglishCenterSystem.Application.Features.Notifications.Services;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using EnglishCenterSystem.Application.Common.Enums;
using EnglishCenterSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class ListeningExerciseImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ListeningCoreImportService _listeningCoreImportService;
    private readonly IQuestionExplanationService _questionExplanationService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<ListeningExerciseImportService> _logger;

    public ListeningExerciseImportService(
        IUnitOfWork unitOfWork,
        ListeningCoreImportService listeningCoreImportService,
        IQuestionExplanationService questionExplanationService,
        NotificationService notificationService,
        ILogger<ListeningExerciseImportService> logger)
    {
        _unitOfWork = unitOfWork;
        _listeningCoreImportService = listeningCoreImportService;
        _questionExplanationService = questionExplanationService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<int>
    ImportListeningExerciseAsync(
        int teacherUserId,
        List<ListeningAudioUploadItem> audioFiles,
        Stream questionStream,
        string questionFileName,
        string questionCountsJson,
        ImportListeningExerciseDto dto)
    {
        if (string.IsNullOrWhiteSpace(
            dto.Title))
        {
            throw new Exception(
                "Listening exercise title is required");
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

        var listeningTasks =
            await _listeningCoreImportService
                .CreateListeningTasksAsync(
                    teacherUserId,
                    ImportStorageModule.Exercise,
                    storageFolderName,
                    dto.Title,
                    dto.Description,
                    dto.Transcript,
                    dto.CourseType,
                    audioFiles,
                    questionStream,
                    questionFileName,
                    questionCountsJson);

        if (dto.GenerateExplanations)
        {
            foreach (var listeningTask in listeningTasks)
            {
                await _questionExplanationService
                    .AddListeningExplanationsAsync(listeningTask);
            }
        }

        if (!listeningTasks.Any())
        {
            throw new Exception(
                "Listening file has no valid listening tasks");
        }

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
                SkillType.Listening,

            SourceFileUrl = listeningTasks.First().SourceQuestionFileUrl,

            SourceFileObjectName = listeningTasks.First().SourceQuestionFileObjectName,

            IsActive = true,

            ListeningTasks =
                listeningTasks
                    .Select((task, index) =>
                        new ExerciseListeningTask
                        {
                            ListeningTask = task,
                            OrderIndex = index + 1
                        })
                    .ToList()
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
