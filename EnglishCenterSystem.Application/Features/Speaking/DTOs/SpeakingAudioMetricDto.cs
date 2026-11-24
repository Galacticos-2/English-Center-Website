namespace EnglishCenterSystem.Application.Features.Speaking.DTOs;

public class SpeakingAudioMetricDto
{
    public string Word { get; set; } = "";

    public double Start { get; set; }

    public double End { get; set; }

    public string? MatchedReferenceWord { get; set; }

    public double AccuracyEstimate { get; set; }

    public string? Issue { get; set; }

    public List<string> Syllables { get; set; } = new();

    public List<string> EstimatedPhonemes { get; set; } = new();
}