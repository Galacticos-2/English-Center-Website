using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Writing.DTOs;

public class WritingTaskDto
{
    public int WritingTaskId { get; set; }

    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string? PromptText { get; set; }

    public string? PromptImageUrl { get; set; }

    public string? ReferenceText { get; set; }

    public CourseType CourseType { get; set; }

    public bool IsActive { get; set; }
}
