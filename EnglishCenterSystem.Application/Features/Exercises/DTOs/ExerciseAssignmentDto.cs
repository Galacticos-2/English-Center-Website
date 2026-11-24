using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class ExerciseAssignmentDto
{
    public int ExerciseAssignmentId { get; set; }

    public int ExerciseId { get; set; }

    public string ExerciseTitle { get; set; } = null!;

    public int ClassRoomId { get; set; }

    public string ClassRoomName { get; set; } = null!;

  

    public int MaxAttempts { get; set; }

    public DateTime? DeadlineAt { get; set; }

    public bool IsActive { get; set; }
}
