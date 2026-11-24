using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Reading;

public class ReadingQuestion : BaseEntity
{
    public int ReadingQuestionId { get; set; }

    public int ReadingTaskId { get; set; }

    public string Content { get; set; } = null!;

    public int OrderIndex { get; set; }

    public double Point { get; set; } = 1;

    public string? Explanation { get; set; }

    public ReadingTask ReadingTask { get; set; } = null!;

    public ICollection<ReadingOption> Options { get; set; }
        = new List<ReadingOption>();
}
