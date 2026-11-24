namespace EnglishCenterSystem.Application.Features.Competitions.DTOs;

public class CompetitionTestImportJobPayloadDto
{
    public int TeacherUserId { get; set; }

    public string TestFilePath { get; set; } = null!;

    public string TestFileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public string QuestionCountsJson { get; set; } = "[]";
    public int? ReplacingCompetitionTestId
    {
        get;
        set;
    }
    public string? Transcript { get; set; }

    public List<CompetitionTestImportAudioFileDto>
        AudioFiles
    { get; set; } = new();

    public ImportCompetitionTestDto ImportDto { get; set; }
        = null!;
}