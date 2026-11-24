using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class StudentQuestionOptionDto
{
    public int QuestionOptionId { get; set; }

    public string Content { get; set; } = null!;
    public int? ListeningOptionId { get; set; }
    public int? ReadingOptionId { get; set; }

    public int OrderIndex { get; set; }
}