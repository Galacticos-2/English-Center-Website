using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Writing;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseWritingTask : BaseEntity
{
    public int ExerciseWritingTaskId { get; set; }

    public int ExerciseId { get; set; }

    public int WritingTaskId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; } = 10;

    public Exercise Exercise { get; set; } = null!;

    public WritingTask WritingTask { get; set; } = null!;
}
