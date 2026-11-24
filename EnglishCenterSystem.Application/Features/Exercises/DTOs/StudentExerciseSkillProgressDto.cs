using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class StudentExerciseSkillProgressDto
{
    public SkillType Skill { get; set; }

    public string SkillName { get; set; } = null!;

    public int TotalAssigned { get; set; }

    public int Completed { get; set; }

    public double CompletionPercent { get; set; }

    public double AverageScore { get; set; }

    public double HighestScore { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public int CurrentStreakDays { get; set; }
}