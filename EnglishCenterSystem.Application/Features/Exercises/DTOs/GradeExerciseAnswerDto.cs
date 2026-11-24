using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class GradeExerciseAnswerDto
{
    public int ExerciseAnswerId { get; set; }

    public double ManualScore { get; set; }

    public string? TeacherFeedback { get; set; }
}