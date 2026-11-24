using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseAssignment : BaseEntity
{
    public int ExerciseAssignmentId { get; set; }

    public int ExerciseId { get; set; }

    public int ClassRoomId { get; set; }

    

    public int MaxAttempts { get; set; } = 1;

    public DateTime? DeadlineAt { get; set; }

    public bool IsActive { get; set; } = true;

    public Exercise Exercise { get; set; } = null!;

    public ClassRoom ClassRoom { get; set; } = null!;
}
