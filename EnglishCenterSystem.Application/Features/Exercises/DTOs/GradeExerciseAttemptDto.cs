using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class GradeExerciseAttemptDto
{
    public int ExerciseAttemptId { get; set; }

    public List<GradeExerciseAnswerDto> Answers { get; set; }
        = new();
}
