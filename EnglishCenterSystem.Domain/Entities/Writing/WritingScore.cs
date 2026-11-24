using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Writing;

public class WritingScore : BaseEntity
{
    public int WritingScoreId { get; set; }

    public int WritingSubmissionId { get; set; }

    public double OverallBand { get; set; }

    public double TaskAchievement { get; set; }

    public double CoherenceAndCohesion { get; set; }

    public double LexicalResource { get; set; }
    public string? ImprovementSuggestionsJson { get; set; }

    public double GrammaticalRangeAndAccuracy { get; set; }

    public string Feedback { get; set; } = null!;
}
