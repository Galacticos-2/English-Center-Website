using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class SubmitSpeakingAnswerDto
{
    public int ExerciseAttemptId { get; set; }

    public int SpeakingTaskId { get; set; }
}