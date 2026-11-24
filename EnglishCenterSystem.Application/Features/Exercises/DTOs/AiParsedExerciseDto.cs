using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class AiParsedExerciseDto
{
    public List<AiParsedQuestionDto> Questions { get; set; }
        = new();
}

public class AiParsedQuestionDto
{
    public int QuestionNumber { get; set; }

    public string Content { get; set; } = null!;

    public string QuestionType { get; set; } = "SingleChoice";

    public string? CorrectAnswer { get; set; }

    public string? Explanation { get; set; }

    public List<AiParsedOptionDto> Options { get; set; }
        = new();
}

public class AiParsedOptionDto
{
    public string Label { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }
}
