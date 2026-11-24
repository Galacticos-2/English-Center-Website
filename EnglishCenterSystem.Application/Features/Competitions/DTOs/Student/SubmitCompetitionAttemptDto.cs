namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;

public class SubmitCompetitionAttemptDto
{
    public int CompetitionTestAttemptId { get; set; }

    public List<SubmitCompetitionAnswerDto> Answers { get; set; }
        = new();
}

public class SubmitCompetitionAnswerDto
{
    public int? ListeningQuestionId { get; set; }

    public int? ReadingQuestionId { get; set; }

    public int? WritingTaskId { get; set; }

    public int? SpeakingTaskId { get; set; }

    public int? SelectedListeningOptionId { get; set; }

    public int? SelectedReadingOptionId { get; set; }

    public string? TextAnswer { get; set; }
}