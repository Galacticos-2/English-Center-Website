namespace EnglishCenterSystem.API.Requests;

public class SubmitCompetitionSpeakingAnswerRequest
{
    public int CompetitionTestAttemptId { get; set; }

    public int SpeakingTaskId { get; set; }

    public IFormFile AudioFile { get; set; } = null!;
}