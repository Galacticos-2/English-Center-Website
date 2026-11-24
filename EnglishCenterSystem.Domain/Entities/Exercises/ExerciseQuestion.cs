using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseQuestion : BaseEntity
{
    public int ExerciseQuestionId { get; set; }

    public int ExerciseId { get; set; }

    public int QuestionId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; } = 1;
    public int? ExerciseQuestionGroupId { get; set; }

    public ExerciseQuestionGroup? ExerciseQuestionGroup { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public Question Question { get; set; } = null!;
}