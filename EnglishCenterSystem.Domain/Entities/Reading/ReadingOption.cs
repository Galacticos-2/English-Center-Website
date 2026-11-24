using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Reading;

public class ReadingOption : BaseEntity
{
    public int ReadingOptionId { get; set; }

    public int ReadingQuestionId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }

    public ReadingQuestion ReadingQuestion { get; set; } = null!;
}