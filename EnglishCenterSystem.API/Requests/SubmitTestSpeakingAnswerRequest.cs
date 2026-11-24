namespace EnglishCenterSystem.API.Requests;

public class SubmitTestSpeakingAnswerRequest
{
    public int TestAttemptId { get; set; }

    public int SpeakingTaskId { get; set; }

    public IFormFile AudioFile { get; set; } = null!;
}