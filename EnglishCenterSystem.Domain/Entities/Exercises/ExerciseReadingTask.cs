using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Reading;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseReadingTask : BaseEntity
{
    public int ExerciseReadingTaskId { get; set; }

    public int ExerciseId { get; set; }

    public int ReadingTaskId { get; set; }

    public int OrderIndex { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public ReadingTask ReadingTask { get; set; } = null!;
}