using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class StudentExerciseProgressDto
{
    public int TotalAssignedExercises { get; set; }

    public int CompletedExercises { get; set; }

    public int InProgressAttempts { get; set; }

    public double CompletionPercent { get; set; }

    public double AverageScore { get; set; }

    public double TotalScore { get; set; }

    public double TotalPossibleScore { get; set; }
}