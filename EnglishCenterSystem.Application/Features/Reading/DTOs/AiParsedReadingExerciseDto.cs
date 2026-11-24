using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Reading.DTOs;

public class AiParsedReadingExerciseDto
{
    public List<AiParsedReadingTaskDto> Passages { get; set; }
        = new();
}

public class AiParsedReadingTaskDto
{
    public int PassageNumber { get; set; }

    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string PassageText { get; set; } = null!;

    public List<AiParsedReadingQuestionDto> Questions { get; set; }
        = new();
}

public class AiParsedReadingQuestionDto
{
    public int QuestionNumber { get; set; }

    public string Content { get; set; } = null!;

    public string? Explanation { get; set; }


    public List<AiParsedReadingOptionDto> Options { get; set; }
        = new();
}

public class AiParsedReadingOptionDto
{
    public string Label { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }
}
