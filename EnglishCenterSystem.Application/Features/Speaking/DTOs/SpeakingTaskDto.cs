using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Speaking.DTOs;

public class SpeakingTaskDto
{
    public int SpeakingTaskId { get; set; }

    public int TeacherUserId { get; set; }

    public string TeacherName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string PdfUrl { get; set; } = null!;

    public string? PromptText { get; set; }

    public string? PromptImageUrl { get; set; }

    public string? ReferenceText { get; set; }

    public CourseType CourseType { get; set; }

 

    public int? PreparationTimeSeconds { get; set; }

    public int? SpeakingTimeSeconds { get; set; }

    public bool IsActive { get; set; }
}
