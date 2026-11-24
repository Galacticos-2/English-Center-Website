using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Competitions.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using EnglishCenterSystem.Application.Interfaces.Services;
namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class ExerciseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinioService _minioService;
    private readonly IQuestionExplanationService _questionExplanationService;

    public async Task<CompetitionSourceFileDto> GetSourceFileAsync(
        int teacherUserId,
        int exerciseId)
    {
        var objectName = await _unitOfWork.Exercises
            .QueryBasic()
            .AsNoTracking()
            .Where(x => x.ExerciseId == exerciseId &&
                x.TeacherUserId == teacherUserId && x.IsActive)
            .Select(x => x.SourceFileObjectName)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new Exception("Exercise source file not found");
        }

        return new CompetitionSourceFileDto
        {
            FileUrl = await _minioService.GetPresignedUrlAsync(objectName, 600),
            FileName = Path.GetFileName(objectName)
        };
    }
    private async Task ValidateQuestionGroupsAsync(
    int teacherUserId,
    List<CreateExerciseQuestionGroupDto> groups)
    {
        foreach (var group in groups)
        {
            if (string.IsNullOrWhiteSpace(group.Title))
            {
                throw new Exception("Question group title is required");
            }

            if (group.GroupType ==
                ExerciseQuestionGroupType.ReadingPassage &&
                string.IsNullOrWhiteSpace(group.PassageText))
            {
                throw new Exception(
                    "Reading question group must have passage text");
            }

            if (group.GroupType ==
                ExerciseQuestionGroupType.ListeningAudio &&
                string.IsNullOrWhiteSpace(group.AudioUrl))
            {
                throw new Exception(
                    "Listening question group must have audio url");
            }

            await ValidateExerciseAsync(
                teacherUserId,
                group.Questions);
        }
    }

    public ExerciseService(
    IUnitOfWork unitOfWork,
    IMinioService minioService,
    IQuestionExplanationService questionExplanationService)
    {
        _unitOfWork = unitOfWork;
        _minioService = minioService;
        _questionExplanationService = questionExplanationService;
    }

    public async Task<List<ExerciseDto>> GetExercisesAsync(
        int teacherUserId,
        CourseType? courseType,
        SkillType? skill
        
        )
    {
        var query = _unitOfWork.Exercises
            .Query()
            .Where(x => x.TeacherUserId == teacherUserId);

        if (courseType.HasValue)
        {
            query = query.Where(x => x.CourseType == courseType.Value);
        }

        if (skill.HasValue)
        {
            query = query.Where(x => x.Skill == skill.Value);
        }
        var result = await query
    .OrderByDescending(x => x.CreatedAt)
    .Select(x => new ExerciseDto
    {
        ExerciseId = x.ExerciseId,

        TeacherUserId =
            x.TeacherUserId,

        TeacherName =
            x.TeacherUser.FullName,

        Title =
            x.Title,

        Description =
            x.Description,

        HasSourceFile =
            !string.IsNullOrWhiteSpace(
                x.SourceFileObjectName),

        CourseType =
            x.CourseType,

        Skill =
            x.Skill,

        ExerciseSectionId =
            x.ExerciseSectionId,

        ExerciseSectionTitle =
            x.ExerciseSection != null
                ? x.ExerciseSection.Title
                : null,

        ExerciseCategoryId =
            x.ExerciseSection != null
                ? x.ExerciseSection
                    .ExerciseCategoryId
                : null,

        ExerciseCategoryName =
            x.ExerciseSection != null
                ? x.ExerciseSection
                    .Category.Name
                : null,

        IsActive =
            x.IsActive,

        TotalQuestions =
            x.ExerciseQuestions.Count +

            x.QuestionGroups
                .SelectMany(g =>
                    g.ExerciseQuestions)
                .Count() +

            x.ReadingTasks
                .SelectMany(t =>
                    t.ReadingTask.Questions)
                .Count() +

            x.ListeningTasks
                .SelectMany(t =>
                    t.ListeningTask.Questions)
                .Count() +

            x.SpeakingTasks.Count +

            x.WritingTasks.Count,

        TotalPoints =
            x.ExerciseQuestions
                .Sum(q => q.Point) +

            x.QuestionGroups
                .SelectMany(g =>
                    g.ExerciseQuestions)
                .Sum(q => q.Point) +

            x.ReadingTasks
                .SelectMany(t =>
                    t.ReadingTask.Questions)
                .Sum(q => q.Point) +

            x.ListeningTasks
                .SelectMany(t =>
                    t.ListeningTask.Questions)
                .Sum(q => q.Point) +

            x.SpeakingTasks
                .Sum(t => t.Point) +

            x.WritingTasks
                .Sum(t => t.Point)
    })
    .ToListAsync();

        var exerciseIds =
            result
                .Select(x =>
                    x.ExerciseId)
                .ToList();

        var assignmentData =
            await _unitOfWork
                .ExerciseAssignments
                .Query()
                .Where(x =>
                    exerciseIds.Contains(
                        x.ExerciseId))
                .GroupBy(x =>
                    x.ExerciseId)
                .Select(group => new
                {
                    ExerciseId =
                        group.Key,

                    MaxAttempts =
                        group
                            .OrderByDescending(x =>
                                x.CreatedAt)
                            .Select(x =>
                                x.MaxAttempts)
                            .FirstOrDefault()
                })
                .ToListAsync();

        var maxAttemptsDictionary =
            assignmentData
                .ToDictionary(
                    x => x.ExerciseId,
                    x => x.MaxAttempts
                );

        foreach (var exerciseDto in result)
        {
            exerciseDto.MaxAttempts =
                maxAttemptsDictionary
                    .TryGetValue(
                        exerciseDto.ExerciseId,
                        out var attempts)
                    ? attempts
                    : 1;
        }

        return result;
    }

    public async Task<ExerciseDto> GetExerciseDetailAsync(
        int teacherUserId,
        int exerciseId)
    {
        var exercise = await _unitOfWork.Exercises
            .Query()
            .Where(x =>
                x.ExerciseId == exerciseId &&
                x.TeacherUserId == teacherUserId)
            .FirstOrDefaultAsync();

        if (exercise == null)
        {
            throw new Exception("Exercise not found");
        }

        return MapToDto(exercise);
    }

    public async Task<int> CreateExerciseAsync(
    int teacherUserId,
    CreateExerciseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new Exception("Exercise set title is required");
        }

        if (dto.Questions.Any())
        {
            await ValidateExerciseAsync(
                teacherUserId,
                dto.Questions);
        }
        if (dto.QuestionGroups.Any())
        {
            await ValidateQuestionGroupsAsync(
                teacherUserId,
                dto.QuestionGroups);
        }
        var exercise = new Exercise
        {
            TeacherUserId = teacherUserId,
            ExerciseSectionId = dto.ExerciseSectionId,
            Title = dto.Title,
            Description = dto.Description,
            CourseType = dto.CourseType,
            Skill = dto.Skill,
            
            
            IsActive = true,
            ExerciseQuestions = dto.Questions
                .OrderBy(x => x.OrderIndex)
                .Select(x => new ExerciseQuestion
                {
                    QuestionId = x.QuestionId,
                    OrderIndex = x.OrderIndex,
                    Point = x.Point
                })
                .ToList(),
            QuestionGroups = dto.QuestionGroups
    .OrderBy(x => x.OrderIndex)
    .Select(group => new ExerciseQuestionGroup
    {
        Title = group.Title,
        GroupType = group.GroupType,
        PassageText = group.PassageText,
        AudioUrl = group.AudioUrl,
        ImageUrl = group.ImageUrl,
        OrderIndex = group.OrderIndex,
        ExerciseQuestions = group.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(q => new ExerciseQuestion
            {
                QuestionId = q.QuestionId,
                OrderIndex = q.OrderIndex,
                Point = q.Point
            })
            .ToList()
    })
    .ToList()
        };

        await _unitOfWork.Exercises.AddAsync(exercise);
        await _unitOfWork.SaveChangesAsync();

        return exercise.ExerciseId;
    }
    public async Task UpdateExerciseMetadataAsync(
    int teacherUserId,
    int exerciseId,
    UpdateExerciseMetadataDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new Exception(
                "Exercise set title is required");
        }

        if (dto.MaxAttempts <= 0)
        {
            throw new Exception(
                "Max attempts must be greater than 0");
        }

        var exercise =
            await _unitOfWork.Exercises
                .Query()
                .FirstOrDefaultAsync(x =>
                    x.ExerciseId == exerciseId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (exercise == null)
        {
            throw new Exception(
                "Exercise not found");
        }

        exercise.Title =
            dto.Title.Trim();

        exercise.Description =
            string.IsNullOrWhiteSpace(
                dto.Description)
                ? null
                : dto.Description.Trim();

        exercise.UpdatedAt =
            DateTime.UtcNow;

        var assignments =
            await _unitOfWork
                .ExerciseAssignments
                .Query()
                .Where(x =>
                    x.ExerciseId ==
                        exerciseId)
                .ToListAsync();

        foreach (var assignment in assignments)
        {
            assignment.MaxAttempts =
                dto.MaxAttempts;
        }

        if (dto.GenerateExplanations)
        {
            var questions = exercise.ExerciseQuestions
                .Select(x => x.Question)
                .Concat(exercise.QuestionGroups
                    .SelectMany(x => x.ExerciseQuestions)
                    .Select(x => x.Question))
                .GroupBy(x => x.QuestionId)
                .Select(x => x.First())
                .ToList();

            await _questionExplanationService
                .AddExerciseExplanationsAsync(questions);

            foreach (var listeningTask in exercise.ListeningTasks
                .Select(x => x.ListeningTask))
            {
                await _questionExplanationService
                    .AddListeningExplanationsAsync(listeningTask);
            }

            foreach (var readingTask in exercise.ReadingTasks
                .Select(x => x.ReadingTask))
            {
                await _questionExplanationService
                    .AddReadingExplanationsAsync(readingTask);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task UpdateExerciseAsync(
        int teacherUserId,
        int exerciseId,
        UpdateExerciseDto dto)
    {
        var exercise = await _unitOfWork.Exercises
            .GetByIdAsync(exerciseId);

        if (exercise == null ||
            exercise.TeacherUserId != teacherUserId)
        {
            throw new Exception("Exercise not found");
        }

        if (dto.Questions.Any())
        {
            await ValidateExerciseAsync(
                teacherUserId,
                dto.Questions);
        }
        if (dto.QuestionGroups.Any())
        {
            await ValidateQuestionGroupsAsync(
                teacherUserId,
                dto.QuestionGroups);
        }
        exercise.Title = dto.Title;
        exercise.ExerciseSectionId = dto.ExerciseSectionId;
        exercise.Description = dto.Description;
        exercise.CourseType = dto.CourseType;
        exercise.Skill = dto.Skill;
        
        
        exercise.IsActive = dto.IsActive;
        exercise.UpdatedAt = DateTime.UtcNow;

        exercise.ExerciseQuestions.Clear();
        exercise.QuestionGroups.Clear();
        foreach (var questionDto in dto.Questions.OrderBy(x => x.OrderIndex))
        {
            exercise.ExerciseQuestions.Add(new ExerciseQuestion
            {
                QuestionId = questionDto.QuestionId,
                OrderIndex = questionDto.OrderIndex,
                Point = questionDto.Point
            });
        }
        foreach (var groupDto in dto.QuestionGroups.OrderBy(x => x.OrderIndex))
        {
            exercise.QuestionGroups.Add(new ExerciseQuestionGroup
            {
                Title = groupDto.Title,
                GroupType = groupDto.GroupType,
                PassageText = groupDto.PassageText,
                AudioUrl = groupDto.AudioUrl,
                ImageUrl = groupDto.ImageUrl,
                OrderIndex = groupDto.OrderIndex,
                ExerciseQuestions = groupDto.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new ExerciseQuestion
                    {
                        QuestionId = q.QuestionId,
                        OrderIndex = q.OrderIndex,
                        Point = q.Point
                    })
                    .ToList()
            });
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteExerciseAsync(
    int teacherUserId,
    int exerciseId)
    {
        /*
         * Bắt buộc AsNoTracking vì Exercise sẽ được
         * HardDeleteAsync xóa trực tiếp khỏi database.
         *
         * Nếu để tracking, EF sẽ tiếp tục giữ Exercise
         * trong ChangeTracker và cố update nó khi
         * SaveChanges xóa Section.
         */
        var exercise =
            await _unitOfWork.Exercises
                .QueryBasic()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ExerciseId == exerciseId &&
                    x.TeacherUserId == teacherUserId);

        if (exercise == null)
        {
            throw new Exception(
                "Exercise not found");
        }

        var objectNames =
            await _unitOfWork.Exercises
                .GetListeningObjectNamesAsync(
                    exerciseId,
                    teacherUserId);

        /*
         * Hard delete toàn bộ dữ liệu liên quan
         * trong database.
         */
        await _unitOfWork.Exercises
            .HardDeleteAsync(
                exerciseId,
                teacherUserId);

        /*
         * Xóa file MinIO sau khi database thành công.
         */
        foreach (var objectName in objectNames)
        {
            try
            {
                await _minioService
                    .DeleteFileAsync(objectName);
            }
            catch
            {
                /*
                 * Nên ghi log tại đây.
                 * Không làm thất bại việc xóa database.
                 */
            }
        }
    }
    private async Task ValidateExerciseAsync(
        int teacherUserId,
        List<CreateExerciseQuestionDto> questions)
    {
        

        if (questions.Any(x => x.Point <= 0))
        {
            throw new Exception("Question point must be greater than 0");
        }

        var questionIds = questions
            .Select(x => x.QuestionId)
            .ToList();

        if (questionIds.Count != questionIds.Distinct().Count())
        {
            throw new Exception("Exercise cannot contain duplicate questions");
        }

        var validQuestionCount = await _unitOfWork.Questions
            .Query()
            .CountAsync(x =>
                questionIds.Contains(x.QuestionId) &&
                x.TeacherUserId == teacherUserId &&
                x.IsActive);

        if (validQuestionCount != questionIds.Count)
        {
            throw new Exception("Some questions are invalid");
        }
    }

    private ExerciseDto MapToDto(Exercise exercise)
    {
        return new ExerciseDto
        {
            ExerciseId = exercise.ExerciseId,
            TeacherUserId = exercise.TeacherUserId,
            TeacherName = exercise.TeacherUser.FullName,
            Title = exercise.Title,
            Description = exercise.Description,
            HasSourceFile = !string.IsNullOrWhiteSpace(exercise.SourceFileObjectName),
            CourseType = exercise.CourseType,
            Skill = exercise.Skill,
           
            
            ExerciseSectionId = exercise.ExerciseSectionId,
            ExerciseSectionTitle = exercise.ExerciseSection != null
                ? exercise.ExerciseSection.Title
                : null,
            ExerciseCategoryId = exercise.ExerciseSection != null
                ? exercise.ExerciseSection.ExerciseCategoryId
                : null,
            ExerciseCategoryName = exercise.ExerciseSection != null
                ? exercise.ExerciseSection.Category.Name
                : null,
            
            IsActive = exercise.IsActive,
            TotalQuestions =
    exercise.ExerciseQuestions.Count +
    exercise.ReadingTasks
        .SelectMany(t => t.ReadingTask.Questions)
        .Count() +
    exercise.ListeningTasks
        .SelectMany(t => t.ListeningTask.Questions)
        .Count() +
    exercise.QuestionGroups
        .SelectMany(g => g.ExerciseQuestions)
        .Count() +
    exercise.SpeakingTasks.Count +
    exercise.WritingTasks.Count,

            TotalPoints =
    exercise.ExerciseQuestions.Sum(x => x.Point) +
    exercise.ReadingTasks
        .SelectMany(t => t.ReadingTask.Questions)
        .Sum(x => x.Point) +
    exercise.ListeningTasks
        .SelectMany(t => t.ListeningTask.Questions)
        .Sum(x => x.Point) +
    exercise.QuestionGroups
        .SelectMany(g => g.ExerciseQuestions)
        .Sum(x => x.Point) +
    exercise.SpeakingTasks
        .Sum(x => x.Point) +
    exercise.WritingTasks
        .Sum(x => x.Point),

            Questions = exercise.ExerciseQuestions
                .OrderBy(x => x.OrderIndex)
                .Select(x => new ExerciseQuestionDto
                {
                    ExerciseQuestionId = x.ExerciseQuestionId,
                    ExerciseQuestionGroupId = x.ExerciseQuestionGroupId,
                    QuestionId = x.QuestionId,
                    Content = x.Question.Content,
                    QuestionType = x.Question.QuestionType,
                    OrderIndex = x.OrderIndex,
                    Point = x.Point,
                    Options = x.Question.Options
                        .OrderBy(o => o.OrderIndex)
                        .Select(o => new QuestionOptionDto
                        {
                            QuestionOptionId = o.QuestionOptionId,
                            Content = o.Content,
                            IsCorrect = o.IsCorrect,
                            OrderIndex = o.OrderIndex
                        })
                        .ToList()
                })
                .ToList(),

            QuestionGroups = exercise.QuestionGroups
                .OrderBy(group => group.OrderIndex)
                .Select(group => new ExerciseQuestionGroupDto
                {
                    ExerciseQuestionGroupId = group.ExerciseQuestionGroupId,
                    Title = group.Title,
                    GroupType = group.GroupType,
                    PassageText = group.PassageText,
                    AudioUrl = group.AudioUrl,
                    ImageUrl = group.ImageUrl,
                    OrderIndex = group.OrderIndex,
                    Questions = group.ExerciseQuestions
                        .OrderBy(x => x.OrderIndex)
                        .Select(x => new ExerciseQuestionDto
                        {
                            ExerciseQuestionId = x.ExerciseQuestionId,
                            ExerciseQuestionGroupId = x.ExerciseQuestionGroupId,
                            QuestionId = x.QuestionId,
                            Content = x.Question.Content,
                            QuestionType = x.Question.QuestionType,
                            OrderIndex = x.OrderIndex,
                            Point = x.Point,
                            Options = x.Question.Options
                                .OrderBy(o => o.OrderIndex)
                                .Select(o => new QuestionOptionDto
                                {
                                    QuestionOptionId = o.QuestionOptionId,
                                    Content = o.Content,
                                    IsCorrect = o.IsCorrect,
                                    OrderIndex = o.OrderIndex
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
