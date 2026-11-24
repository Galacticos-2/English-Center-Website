using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class CreateExerciseAssignmentDto
{
    public int ExerciseId { get; set; }

    public int ClassRoomId { get; set; }

   

    public int MaxAttempts { get; set; } = 1;

    public DateTime? DeadlineAt { get; set; }
}
