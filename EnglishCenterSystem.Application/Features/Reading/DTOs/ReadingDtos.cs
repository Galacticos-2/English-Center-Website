using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Reading.DTOs;

public class ReadingTaskDto
{
    public int ReadingTaskId { get; set; }

    public int TeacherUserId { get; set; }
    public List<ReadingPassageImageDto>
    PassageImages
    { get; set; }
        = new();
    public string TeacherName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string PassageText { get; set; } = null!;

    public CourseType CourseType { get; set; }

    

    public bool IsActive { get; set; }

    public int TotalQuestions { get; set; }

    public List<ReadingQuestionDto> Questions { get; set; }
        = new();
}
public class ReadingPassageImageDto
{
    public int ReadingPassageImageId
    {
        get;
        set;
    }

    public string ImageUrl
    {
        get;
        set;
    } = null!;

    public int PageNumber
    {
        get;
        set;
    }

    public int OrderIndex
    {
        get;
        set;
    }
}
public class ReadingQuestionDto
{
    public int ReadingQuestionId { get; set; }

    public string Content { get; set; } = null!;

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }

    public List<ReadingOptionDto> Options { get; set; }
        = new();
}

public class ReadingOptionDto
{
    public int ReadingOptionId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }
}
