using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class CreateExerciseQuestionDto
{
    public int QuestionId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; } = 1;
    public int? ExerciseQuestionGroupId { get; set; }
}