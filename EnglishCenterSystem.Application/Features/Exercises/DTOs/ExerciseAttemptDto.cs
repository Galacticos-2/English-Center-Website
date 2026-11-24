using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class ExerciseAttemptDto
{
    public int ExerciseAttemptId { get; set; }

    public int ExerciseAssignmentId { get; set; }

    public string ExerciseTitle { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public List<StudentAttemptQuestionDto> Questions { get; set; }
        = new();
}
