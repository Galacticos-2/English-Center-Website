using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Reading.DTOs;

public class CreateReadingTaskDto
{
    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }
    
    public string PassageText { get; set; } = null!;

    public CourseType CourseType { get; set; }

    public List<CreateReadingQuestionDto> Questions { get; set; }
        = new();
}

public class CreateReadingQuestionDto
{
    public string Content { get; set; } = null!;

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; } = 1;

    public List<CreateReadingOptionDto> Options { get; set; }
        = new();
}

public class CreateReadingOptionDto
{
    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }
}
