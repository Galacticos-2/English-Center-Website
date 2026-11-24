using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class ExerciseDraftDto
{
    public int ExerciseDraftId { get; set; }

    public int ExerciseSourceDocumentId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    public string? ParsedJson { get; set; }

    public string? AiModelUsed { get; set; }

    public string? ParseError { get; set; }

    public string RawText { get; set; } = null!;

    public CourseType CourseType { get; set; }

    public SkillType Skill { get; set; }

    

    public int? ExerciseSectionId { get; set; }

    public string? ExerciseSectionTitle { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}