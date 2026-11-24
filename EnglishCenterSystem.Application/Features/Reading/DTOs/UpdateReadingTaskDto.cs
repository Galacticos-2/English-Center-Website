using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Reading.DTOs;

public class UpdateReadingTaskDto
{
    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string PassageText { get; set; } = null!;

    public CourseType CourseType { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateReadingQuestionDto> Questions { get; set; }
        = new();
}