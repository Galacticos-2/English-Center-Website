namespace EnglishCenterSystem.API.Requests;

public class SubmitSpeakingAnswerRequest
{
    public int ExerciseAttemptId { get; set; }

    public int SpeakingTaskId { get; set; }

    public IFormFile AudioFile { get; set; } = null!;
}