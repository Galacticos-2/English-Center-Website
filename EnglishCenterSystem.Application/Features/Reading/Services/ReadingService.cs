using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Reading.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Reading;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application.Features.Reading.Services;

public class ReadingService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReadingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ReadingTaskDto>> GetReadingTasksAsync(
        int teacherUserId,
        CourseType? courseType)
    {
        var query = _unitOfWork.ReadingTasks
            .Query()
            .Where(x => x.TeacherUserId == teacherUserId);

        if (courseType.HasValue)
        {
            query = query.Where(x => x.CourseType == courseType.Value);
        }

        return await query
     .OrderByDescending(x => x.CreatedAt)
     .Select(x => new ReadingTaskDto
     {
         ReadingTaskId = x.ReadingTaskId,
         TeacherUserId = x.TeacherUserId,
         TeacherName = x.TeacherUser.FullName,
         Title = x.Title,
         Instruction = x.Instruction,
         PassageText = x.PassageText,
         CourseType = x.CourseType,
         IsActive = x.IsActive,
         TotalQuestions = x.Questions.Count,

         PassageImages = x.PassageImages
             .OrderBy(image => image.OrderIndex)
             .Select(image => new ReadingPassageImageDto
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

         Questions = x.Questions
             .OrderBy(q => q.OrderIndex)
             .Select(q => new ReadingQuestionDto
             {
                 ReadingQuestionId = q.ReadingQuestionId,
                 Content = q.Content,
                 Explanation = q.Explanation,
                 OrderIndex = q.OrderIndex,
                 Point = q.Point,
                 Options = q.Options
                     .OrderBy(o => o.OrderIndex)
                     .Select(o => new ReadingOptionDto
                     {
                         ReadingOptionId = o.ReadingOptionId,
                         Content = o.Content,
                         IsCorrect = o.IsCorrect,
                         OrderIndex = o.OrderIndex
                     })
                     .ToList()
             })
             .ToList()
     })
     .ToListAsync();
    }

    public async Task<ReadingTaskDto> GetReadingTaskDetailAsync(
        int teacherUserId,
        int readingTaskId)
    {
        var task = await _unitOfWork.ReadingTasks
            .GetByIdAsync(readingTaskId);

        if (task == null ||
            task.TeacherUserId != teacherUserId)
        {
            throw new Exception("Reading task not found");
        }

        return MapToDto(task);
    }

    public async Task<int> CreateReadingTaskAsync(
        int teacherUserId,
        CreateReadingTaskDto dto)
    {
        ValidateReadingTask(dto.Title, dto.PassageText, dto.Questions);

        var task = new ReadingTask
        {
            TeacherUserId = teacherUserId,
            Title = dto.Title,
            Instruction = dto.Instruction,
            PassageText = dto.PassageText,
            CourseType = dto.CourseType,
            IsActive = true,
            Questions = dto.Questions
                .OrderBy(x => x.OrderIndex)
                .Select(q => new ReadingQuestion
                {
                    Content = q.Content,
                    Explanation =
                        string.IsNullOrWhiteSpace(q.Explanation)
                            ? null
                            : q.Explanation.Trim(),
                    OrderIndex = q.OrderIndex,
                    Point = q.Point,
                    Options = q.Options
                        .OrderBy(o => o.OrderIndex)
                        .Select(o => new ReadingOption
                        {
                            Content = o.Content,
                            IsCorrect = o.IsCorrect,
                            OrderIndex = o.OrderIndex
                        })
                        .ToList()
                })
                .ToList()
        };

        await _unitOfWork.ReadingTasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return task.ReadingTaskId;
    }

    public async Task UpdateReadingTaskAsync(
        int teacherUserId,
        int readingTaskId,
        UpdateReadingTaskDto dto)
    {
        ValidateReadingTask(dto.Title, dto.PassageText, dto.Questions);

        var task = await _unitOfWork.ReadingTasks
            .GetByIdAsync(readingTaskId);

        if (task == null ||
            task.TeacherUserId != teacherUserId)
        {
            throw new Exception("Reading task not found");
        }

        task.Title = dto.Title;
        task.Instruction = dto.Instruction;
        task.PassageText = dto.PassageText;
        task.CourseType = dto.CourseType;
        task.IsActive = dto.IsActive;
        task.UpdatedAt = DateTime.UtcNow;

        task.Questions.Clear();

        foreach (var questionDto in dto.Questions.OrderBy(x => x.OrderIndex))
        {
            task.Questions.Add(new ReadingQuestion
            {
                Content = questionDto.Content,
                Explanation =
                    string.IsNullOrWhiteSpace(questionDto.Explanation)
                        ? null
                        : questionDto.Explanation.Trim(),
                OrderIndex = questionDto.OrderIndex,
                Point = questionDto.Point,
                Options = questionDto.Options
                    .OrderBy(o => o.OrderIndex)
                    .Select(o => new ReadingOption
                    {
                        Content = o.Content,
                        IsCorrect = o.IsCorrect,
                        OrderIndex = o.OrderIndex
                    })
                    .ToList()
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteReadingTaskAsync(
        int teacherUserId,
        int readingTaskId)
    {
        var task = await _unitOfWork.ReadingTasks
            .GetByIdAsync(readingTaskId);

        if (task == null ||
            task.TeacherUserId != teacherUserId)
        {
            throw new Exception("Reading task not found");
        }

        _unitOfWork.ReadingTasks.Remove(task);
        await _unitOfWork.SaveChangesAsync();
    }

    private void ValidateReadingTask(
        string title,
        string passageText,
        List<CreateReadingQuestionDto> questions)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new Exception("Reading title is required");
        }

        if (string.IsNullOrWhiteSpace(passageText))
        {
            throw new Exception("Reading passage text is required");
        }

        if (!questions.Any())
        {
            throw new Exception("Reading task must have at least one question");
        }

        foreach (var question in questions)
        {
            if (string.IsNullOrWhiteSpace(question.Content))
            {
                throw new Exception("Reading question content is required");
            }

            if (question.Point <= 0)
            {
                throw new Exception("Reading question point must be greater than 0");
            }

            if (question.Options.Count < 2)
            {
                throw new Exception("Reading question must have at least 2 options");
            }

            if (question.Options.Count(x => x.IsCorrect) != 1)
            {
                throw new Exception("Reading question must have exactly 1 correct answer");
            }
        }
    }

    private ReadingTaskDto MapToDto(ReadingTask task)
    {
        return new ReadingTaskDto
        {
            ReadingTaskId = task.ReadingTaskId,
            TeacherUserId = task.TeacherUserId,
            TeacherName = task.TeacherUser.FullName,
            Title = task.Title,
            Instruction = task.Instruction,
            PassageText = task.PassageText,
            CourseType = task.CourseType,
            IsActive = task.IsActive,
            TotalQuestions = task.Questions.Count,

            PassageImages = task.PassageImages
                .OrderBy(image => image.OrderIndex)
                .Select(image => new ReadingPassageImageDto
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

            Questions = task.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new ReadingQuestionDto
                {
                    ReadingQuestionId = q.ReadingQuestionId,
                    Content = q.Content,
                    Explanation = q.Explanation,
                    OrderIndex = q.OrderIndex,
                    Point = q.Point,
                    Options = q.Options
                        .OrderBy(o => o.OrderIndex)
                        .Select(o => new ReadingOptionDto
                        {
                            ReadingOptionId =
                                o.ReadingOptionId,

                            Content =
                                o.Content,

                            IsCorrect =
                                o.IsCorrect,

                            OrderIndex =
                                o.OrderIndex
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
