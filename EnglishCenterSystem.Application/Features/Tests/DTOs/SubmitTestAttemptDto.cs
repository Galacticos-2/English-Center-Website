namespace EnglishCenterSystem.Application.Features.Tests.DTOs;

public class SubmitTestAttemptDto
{
    public int TestAttemptId { get; set; }

    public List<SubmitTestAnswerDto> Answers { get; set; } = new();
}