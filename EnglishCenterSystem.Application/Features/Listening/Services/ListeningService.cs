using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Listening.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Listening;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application.Features.Listening.Services;

public class ListeningService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinioService _minioService;

    public ListeningService(
        IUnitOfWork unitOfWork,
        IMinioService minioService)
    {
        _unitOfWork = unitOfWork;
        _minioService = minioService;
    }

    public async Task<List<ListeningTaskDto>> GetListeningTasksAsync(
        int teacherUserId,
        CourseType? courseType)
    {
        var query = _unitOfWork.ListeningTasks
            .Query()
            .Where(x => x.TeacherUserId == teacherUserId);

        if (courseType.HasValue)
        {
            query = query.Where(x =>
                x.CourseType == courseType.Value);
        }

        

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ListeningTaskDto
            {
                ListeningTaskId = x.ListeningTaskId,
                TeacherUserId = x.TeacherUserId,
                TeacherName = x.TeacherUser.FullName,
                Title = x.Title,
                Instruction = x.Instruction,
                AudioUrl = x.AudioUrl,
                Transcript = x.Transcript,
                CourseType = x.CourseType,
              
                IsActive = x.IsActive,
                TotalQuestions = x.Questions.Count
            })
            .ToListAsync();
    }

    public async Task<ListeningTaskDto> GetListeningTaskDetailAsync(
        int teacherUserId,
        int listeningTaskId)
    {
        var listeningTask = await _unitOfWork.ListeningTasks
            .Query()
            .FirstOrDefaultAsync(x =>
                x.ListeningTaskId == listeningTaskId &&
                x.TeacherUserId == teacherUserId);

        if (listeningTask == null)
        {
            throw new Exception("Listening task not found");
        }

        return MapToDto(listeningTask);
    }

    public async Task<int> CreateListeningTaskAsync(
        int teacherUserId,
        CreateListeningTaskDto dto)
    {
        ValidateListeningTask(dto.Title, dto.Questions);

        var listeningTask = new ListeningTask
        {
            TeacherUserId = teacherUserId,
            Title = dto.Title,
            Instruction = dto.Instruction,
            Transcript = dto.Transcript,
            AudioUrl = "",
            CourseType = dto.CourseType,
          
            IsActive = true,
            Questions = dto.Questions
                .OrderBy(x => x.OrderIndex)
                .Select(question => new ListeningQuestion
                {
                    Content = question.Content,
                    Explanation =
                        string.IsNullOrWhiteSpace(question.Explanation)
                            ? null
                            : question.Explanation.Trim(),
                    OrderIndex = question.OrderIndex,
                    Point = question.Point,
                    Options = question.Options
                        .OrderBy(option => option.OrderIndex)
                        .Select(option => new ListeningOption
                        {
                            Content = option.Content,
                            IsCorrect = option.IsCorrect,
                            OrderIndex = option.OrderIndex
                        })
                        .ToList()
                })
                .ToList()
        };

        await _unitOfWork.ListeningTasks.AddAsync(listeningTask);
        await _unitOfWork.SaveChangesAsync();

        return listeningTask.ListeningTaskId;
    }

    public async Task UpdateListeningTaskAsync(
        int teacherUserId,
        int listeningTaskId,
        UpdateListeningTaskDto dto)
    {
        ValidateListeningTask(dto.Title, dto.Questions);

        var listeningTask = await _unitOfWork.ListeningTasks
            .GetByIdAsync(listeningTaskId);

        if (listeningTask == null ||
            listeningTask.TeacherUserId != teacherUserId)
        {
            throw new Exception("Listening task not found");
        }

        listeningTask.Title = dto.Title;
        listeningTask.Instruction = dto.Instruction;
        listeningTask.Transcript = dto.Transcript;
        listeningTask.CourseType = dto.CourseType;
       
        listeningTask.IsActive = dto.IsActive;
        listeningTask.UpdatedAt = DateTime.UtcNow;

        listeningTask.Questions.Clear();

        foreach (var questionDto in dto.Questions.OrderBy(x => x.OrderIndex))
        {
            listeningTask.Questions.Add(new ListeningQuestion
            {
                Content = questionDto.Content,
                Explanation =
                    string.IsNullOrWhiteSpace(questionDto.Explanation)
                        ? null
                        : questionDto.Explanation.Trim(),

                OrderIndex = questionDto.OrderIndex,
                Point = questionDto.Point,
                Options = questionDto.Options
                    .OrderBy(option => option.OrderIndex)
                    .Select(option => new ListeningOption
                    {
                        Content = option.Content,
                        IsCorrect = option.IsCorrect,
                        OrderIndex = option.OrderIndex
                    })
                    .ToList()
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteListeningTaskAsync(
        int teacherUserId,
        int listeningTaskId)
    {
        var listeningTask = await _unitOfWork.ListeningTasks
            .GetByIdAsync(listeningTaskId);

        if (listeningTask == null ||
            listeningTask.TeacherUserId != teacherUserId)
        {
            throw new Exception("Listening task not found");
        }

        _unitOfWork.ListeningTasks.Remove(listeningTask);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UploadAudioAsync(
        int teacherUserId,
        int listeningTaskId,
        Stream fileStream,
        string fileName,
        string contentType)
    {
        var listeningTask = await _unitOfWork.ListeningTasks
            .GetByIdAsync(listeningTaskId);

        if (listeningTask == null ||
            listeningTask.TeacherUserId != teacherUserId)
        {
            throw new Exception("Listening task not found");
        }

        var extension = Path.GetExtension(fileName).ToLower();

        if (extension != ".mp3" &&
            extension != ".wav" &&
            extension != ".m4a")
        {
            throw new Exception("Only MP3, WAV or M4A audio files are allowed");
        }

        var objectName =
            $"listening/audio/{teacherUserId}/{Guid.NewGuid()}_{fileName}";

        var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);

        memoryStream.Position = 0;

        var audioUrl = await _minioService.UploadFileAsync(
            memoryStream,
            objectName,
            contentType);

        listeningTask.AudioUrl = audioUrl;
        listeningTask.AudioObjectName = objectName;
        listeningTask.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
    }

    private void ValidateListeningTask(
        string title,
        List<CreateListeningQuestionDto> questions)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new Exception("Listening task title is required");
        }

        if (!questions.Any())
        {
            throw new Exception("Listening task must have at least one question");
        }

        foreach (var question in questions)
        {
            if (string.IsNullOrWhiteSpace(question.Content))
            {
                throw new Exception("Listening question content is required");
            }

            if (question.Point <= 0)
            {
                throw new Exception("Question point must be greater than 0");
            }

            if (question.Options.Count < 2)
            {
                throw new Exception("Listening question must have at least 2 options");
            }

            if (question.Options.Count(x => x.IsCorrect) != 1)
            {
                throw new Exception("Listening question must have exactly 1 correct option");
            }
        }
    }

    private ListeningTaskDto MapToDto(ListeningTask listeningTask)
    {
        return new ListeningTaskDto
        {
            ListeningTaskId = listeningTask.ListeningTaskId,
            TeacherUserId = listeningTask.TeacherUserId,
            TeacherName = listeningTask.TeacherUser.FullName,
            Title = listeningTask.Title,
            Instruction = listeningTask.Instruction,
            AudioUrl = listeningTask.AudioUrl,
            Transcript = listeningTask.Transcript,
            CourseType = listeningTask.CourseType,
          
            IsActive = listeningTask.IsActive,
            TotalQuestions = listeningTask.Questions.Count,
            Questions = listeningTask.Questions
                .OrderBy(x => x.OrderIndex)
                .Select(x => new ListeningQuestionDto
                {
                    ListeningQuestionId = x.ListeningQuestionId,
                    Content = x.Content,
                    PromptImageUrl = x.PromptImageUrl,
                    Explanation = x.Explanation,
                    OrderIndex = x.OrderIndex,
                    Point = x.Point,
                    Options = x.Options
                        .OrderBy(o => o.OrderIndex)
                        .Select(o => new ListeningOptionDto
                        {
                            ListeningOptionId = o.ListeningOptionId,
                            Content = o.Content,
                            IsCorrect = o.IsCorrect,
                            OrderIndex = o.OrderIndex
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
