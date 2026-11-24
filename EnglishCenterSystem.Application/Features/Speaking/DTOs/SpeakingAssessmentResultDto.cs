namespace EnglishCenterSystem.Application.Features.Speaking.DTOs;

public class SpeakingAssessmentResultDto
{
    public string Transcript { get; set; } = "";

    public double PronunciationScore { get; set; }

    public double AccuracyScore { get; set; }

    public double FluencyScore { get; set; }

    public double CompletenessScore { get; set; }

    public double ProsodyScore { get; set; }

    public double WordsPerMinute { get; set; }

    public int WordCount { get; set; }

    public int FillerWordCount { get; set; }

    public int RepetitionCount { get; set; }

    public int LongPauseCount { get; set; }

    public double AveragePauseSeconds { get; set; }

    public string? AnalysisJson { get; set; }
}