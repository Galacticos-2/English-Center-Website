using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Writing.DTOs;

public class WritingAiScoreDto
{
    public double OverallBand { get; set; }

    public double TaskAchievement { get; set; }

    public double CoherenceAndCohesion { get; set; }

    public double LexicalResource { get; set; }

    public double GrammaticalRangeAndAccuracy { get; set; }

    public string Feedback { get; set; } = null!;

    public List<string> ImprovementSuggestions { get; set; }
        = new();
}
