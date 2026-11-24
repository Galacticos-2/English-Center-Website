using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseAttempt : BaseEntity
{
    public int ExerciseAttemptId { get; set; }

    public int ExerciseAssignmentId { get; set; }

    public int StudentRoleId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public ExerciseAttemptStatus Status { get; set; }
    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public double TotalPoints { get; set; }

    public double Score { get; set; }

    public ICollection<ExerciseAnswer> Answers { get; set; }
        = new List<ExerciseAnswer>();

    public ExerciseAssignment ExerciseAssignment { get; set; } = null!;

    public StudentRole StudentRole { get; set; } = null!;
}