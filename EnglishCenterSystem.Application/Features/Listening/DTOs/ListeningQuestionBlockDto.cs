using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Listening.DTOs;

public class ListeningQuestionBlockDto
{
    public int QuestionNumber { get; set; }

    public string RawText { get; set; } = "";

    public string? CorrectAnswer { get; set; }
}
