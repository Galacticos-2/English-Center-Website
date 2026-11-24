namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class QuestionAnswerResultDto
{
    public bool IsCorrect { get; set; }

    public string? Explanation { get; set; }

    public List<int> CorrectOptionIds { get; set; } = new();

    public string? CorrectTextAnswer { get; set; }
}
