using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class UpdateExerciseDraftDto
{
    public string Title { get; set; } = null!;
    public int? ExerciseSectionId { get; set; }

    public string? Description { get; set; }
    public string? ParsedJson { get; set; }

    public string RawText { get; set; } = null!;
}