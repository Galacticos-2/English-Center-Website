using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class SubmitExerciseAnswerDto
{
    public int? QuestionId { get; set; }

    public int? SelectedQuestionOptionId { get; set; }
    public string? TextAnswer { get; set; }
    public int? ListeningQuestionId { get; set; }
    public int? ReadingQuestionId { get; set; }

    public int? SelectedReadingOptionId { get; set; }

    public int? SelectedListeningOptionId { get; set; }
    public int? WritingTaskId { get; set; }
    public string? AudioAnswerUrl { get; set; }
}