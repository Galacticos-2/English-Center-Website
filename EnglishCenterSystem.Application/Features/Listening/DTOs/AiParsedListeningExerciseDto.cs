using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Listening.DTOs;

public class AiParsedListeningExerciseDto
{
    public List<AiParsedListeningQuestionDto> Questions { get; set; }
        = new();
}

public class AiParsedListeningQuestionDto
{
    public int QuestionNumber { get; set; }

    public string Content { get; set; } = null!;

    public string? Explanation { get; set; }

    public List<AiParsedListeningOptionDto> Options { get; set; }
        = new();
}

public class AiParsedListeningOptionDto
{
    public string Label { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }
}
