using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class StudentExerciseDto
{
    public int ExerciseAssignmentId { get; set; }

    public int ExerciseId { get; set; }
    public int? ExerciseSectionId { get; set; }

    public string? ExerciseSectionTitle { get; set; }

    public SkillType Skill { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

   

    public int MaxAttempts { get; set; }

    public int AttemptCount { get; set; }

    public bool CanStart { get; set; }

    public int TotalQuestions { get; set; }

    public double TotalPoints { get; set; }
    public List<StudentExerciseAttemptHistoryItemDto> AttemptHistory { get; set; }
    = new();
}
public class StudentExerciseAttemptHistoryItemDto
{
    public int ExerciseAttemptId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public double Score { get; set; }

    public string Status { get; set; } = null!;
}