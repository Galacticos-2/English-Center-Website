using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseQuestionGroup : BaseEntity
{
    public int ExerciseQuestionGroupId { get; set; }

    public int ExerciseId { get; set; }

    public string Title { get; set; } = null!;

    public ExerciseQuestionGroupType GroupType { get; set; }

    public string? PassageText { get; set; }

    public string? AudioUrl { get; set; }

    public string? ImageUrl { get; set; }

    public int OrderIndex { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public ICollection<ExerciseQuestion> ExerciseQuestions { get; set; }
        = new List<ExerciseQuestion>();
}
