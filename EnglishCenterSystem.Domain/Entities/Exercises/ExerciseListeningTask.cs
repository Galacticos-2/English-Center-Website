using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Listening;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseListeningTask : BaseEntity
{
    public int ExerciseListeningTaskId { get; set; }

    public int ExerciseId { get; set; }

    public int ListeningTaskId { get; set; }

    public int OrderIndex { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public ListeningTask ListeningTask { get; set; } = null!;
}