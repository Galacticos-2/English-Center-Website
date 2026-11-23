using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class TeacherExerciseAnalyticsDto
{
    public int TotalAttempts { get; set; }

    public int SubmittedAttempts { get; set; }

    public double AverageScore { get; set; }

    public double HighestScore { get; set; }

    public double LowestScore { get; set; }

    public List<TeacherExerciseResultDto> Results { get; set; }
        = new();
}