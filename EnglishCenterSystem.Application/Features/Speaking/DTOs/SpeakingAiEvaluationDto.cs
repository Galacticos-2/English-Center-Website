using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Speaking.DTOs;

public class SpeakingAiEvaluationDto
{
    public double GrammarScore { get; set; }

    public double VocabularyScore { get; set; }

    public double ContentRelevanceScore { get; set; }

    public double CoherenceScore { get; set; }

    public double OverallScore { get; set; }

    public string Feedback { get; set; } = "";
}