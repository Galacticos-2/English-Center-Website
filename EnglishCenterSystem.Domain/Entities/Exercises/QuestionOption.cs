using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class QuestionOption : BaseEntity
{
    public int QuestionOptionId { get; set; }

    public int QuestionId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }

    public Question Question { get; set; } = null!;
}