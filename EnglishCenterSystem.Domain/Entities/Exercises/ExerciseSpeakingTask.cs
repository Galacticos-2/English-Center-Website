using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Speaking;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseSpeakingTask : BaseEntity
{
    public int ExerciseSpeakingTaskId { get; set; }

    public int ExerciseId { get; set; }

    public int SpeakingTaskId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; } = 20;

    public Exercise Exercise { get; set; } = null!;

    public SpeakingTask SpeakingTask { get; set; } = null!;
}