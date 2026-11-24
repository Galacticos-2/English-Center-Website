using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class ExerciseQuestionDto
{
    public int ExerciseQuestionId { get; set; }

    public int QuestionId { get; set; }

    public string Content { get; set; } = null!;

    public QuestionType QuestionType { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }
    public int? ExerciseQuestionGroupId { get; set; }
    public List<QuestionOptionDto> Options { get; set; }
        = new();
}
