using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class StudentExerciseHistoryDto
{
    public int ExerciseAttemptId { get; set; }

    public int ExerciseAssignmentId { get; set; }

    public string ExerciseTitle { get; set; } = null!;

    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public double TotalPoints { get; set; }

    public double Score { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string Status { get; set; } = null!;
}