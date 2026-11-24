namespace EnglishCenterSystem.Application.Features.Tests.DTOs;

public class SubmitTestAnswerDto
{
    public int? QuestionId { get; set; }

    public int? ListeningQuestionId { get; set; }

    public int? ReadingQuestionId { get; set; }

    public int? WritingTaskId { get; set; }

    public int? SpeakingTaskId { get; set; }

    public int? SelectedQuestionOptionId { get; set; }

    public int? SelectedListeningOptionId { get; set; }

    public int? SelectedReadingOptionId { get; set; }

    public string? TextAnswer { get; set; }

    public string? AudioAnswerUrl { get; set; }
}