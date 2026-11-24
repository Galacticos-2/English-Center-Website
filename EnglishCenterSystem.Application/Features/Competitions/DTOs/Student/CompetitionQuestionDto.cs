using EnglishCenterSystem.Domain.Enums;

using EnglishCenterSystem.Application.Features.Reading.DTOs;

namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;

public class CompetitionQuestionDto
{
    public int? ListeningQuestionId { get; set; }

    public int? ListeningTaskId { get; set; }

    public int? ReadingQuestionId { get; set; }

    public int? ReadingTaskId { get; set; }

    public int? WritingTaskId { get; set; }

    public int? SpeakingTaskId { get; set; }

    public string Content { get; set; } = "";

    public QuestionType QuestionType { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }

    public bool IsListeningQuestion { get; set; }

    public bool IsReadingQuestion { get; set; }

    public bool IsWritingQuestion { get; set; }

    public bool IsSpeakingQuestion { get; set; }

    // Listening

    public CourseType? ListeningCourseType { get; set; }

    public string? ListeningPartTitle { get; set; }

    public string? ListeningDirection { get; set; }

    public string? ListeningLittlePart { get; set; }

    public string? AudioUrl { get; set; }

    public string? ListeningPromptImageUrl { get; set; }

    public string? Transcript { get; set; }

    // Reading

    public string? ReadingPassageTitle { get; set; }

    public string? ReadingPassageText { get; set; }

    public string? ReadingInstruction { get; set; }

    public List<ReadingPassageImageDto>
        ReadingPassageImages { get; set; } = new();

    // Writing

    public string? WritingInstruction { get; set; }

    public string? WritingPromptText { get; set; }

    public string? WritingPromptImageUrl { get; set; }

    public string? WritingReferenceText { get; set; }

    public string? WritingSourceText { get; set; }

    public string? WritingRequirements { get; set; }

    public string? WritingScoringCriteria { get; set; }

    public int? WritingMinimumWords { get; set; }

    public string? WritingTaskType { get; set; }

    // Speaking

    public string? SpeakingPdfUrl { get; set; }

    public string? SpeakingInstruction { get; set; }

    public string? SpeakingPromptText { get; set; }

    public string? SpeakingPromptImageUrl { get; set; }

    public string? SpeakingReferenceText { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public int? SpeakingTimeSeconds { get; set; }

    public List<CompetitionQuestionOptionDto> Options { get; set; }
        = new();
}

public class CompetitionQuestionOptionDto
{
    public int? ListeningOptionId { get; set; }

    public int? ReadingOptionId { get; set; }

    public string Content { get; set; } = "";

    public int OrderIndex { get; set; }
}
