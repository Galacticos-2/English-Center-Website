using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class ExerciseQuestionGroupDto
{
    public int ExerciseQuestionGroupId { get; set; }

    public string Title { get; set; } = null!;

    public ExerciseQuestionGroupType GroupType { get; set; }

    public string? PassageText { get; set; }

    public string? AudioUrl { get; set; }

    public string? ImageUrl { get; set; }

    public int OrderIndex { get; set; }

    public List<ExerciseQuestionDto> Questions { get; set; }
        = new();
}
