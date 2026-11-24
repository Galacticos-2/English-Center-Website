namespace EnglishCenterSystem.Application.Features.Tests.DTOs;

public class TestAnswerOptionResultDto
{
    public int OptionId { get; set; }

    public string Label { get; set; } = "";

    public string Content { get; set; } = "";

    public bool IsSelected { get; set; }

    public bool IsCorrect { get; set; }
}