namespace EnglishCenterSystem.Application.Features.Tests.DTOs;

public class TestImportJobPayloadDto
{
    public int TeacherUserId { get; set; }

    public string TestFilePath { get; set; } = null!;

    public string TestFileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public string QuestionCountsJson { get; set; } = "[]";

    public string? Transcript { get; set; }

    public List<TestImportAudioFileDto> AudioFiles { get; set; } =
        new();

    public ImportTestDto ImportDto { get; set; } = null!;
}