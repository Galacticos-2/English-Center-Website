using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.API.Requests;

public class ImportCompetitionTestRequest
{
    public IFormFile TestFile { get; set; } = null!;

    public List<IFormFile> AudioFiles { get; set; }
        = new();

    public string QuestionCountsJson { get; set; } = "[]";

    public string? Transcript { get; set; }
    public int? ReplacingCompetitionTestId
    {
        get;
        set;
    }
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public bool GenerateExplanations { get; set; }
}
