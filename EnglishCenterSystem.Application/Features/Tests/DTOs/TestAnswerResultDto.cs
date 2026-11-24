using EnglishCenterSystem.Application.Features.Reading.DTOs;

namespace EnglishCenterSystem.Application.Features.Tests.DTOs;

public class TestAnswerResultDto
{
    public int TestAnswerId { get; set; }

    public int QuestionId { get; set; }
    public List<ReadingPassageImageDto>
    ReadingPassageImages
    { get; set; }
        = new();
    public string QuestionContent { get; set; } = "";

    public string? Explanation { get; set; }

    public int? ListeningQuestionId { get; set; }

    public int? ReadingQuestionId { get; set; }

    public int? WritingTaskId { get; set; }

    public int? SpeakingTaskId { get; set; }

    public string? WritingTaskTitle { get; set; }

    public string? SpeakingTaskTitle { get; set; }

    public int? SelectedQuestionOptionId { get; set; }
    public string? SelectedAnswerLabel { get; set; }
    public List<TestAnswerOptionResultDto> Options { get; set; }
    = new();
    public string? CorrectAnswerLabel { get; set; }

    public int? SelectedListeningOptionId { get; set; }

    public int? SelectedReadingOptionId { get; set; }

    public bool IsCorrect { get; set; }

    public double PointEarned { get; set; }

    public double? ManualScore { get; set; }

    public string? TeacherFeedback { get; set; }

    public string? TextAnswer { get; set; }

    public string? AudioAnswerUrl { get; set; }

    public string? SpeakingTranscript { get; set; }

    public double? PronunciationScore { get; set; }

    public double? AccuracyScore { get; set; }
    public double? WritingOverallBand { get; set; }
    // Listening đề bài
    public int? ListeningTaskId { get; set; }

    public string? ListeningTaskTitle { get; set; }

    public string? ListeningInstruction { get; set; }

    public string? ListeningPartTitle { get; set; }

    public string? ListeningDirection { get; set; }

    public string? ListeningLittlePart { get; set; }

    public string? ListeningAudioUrl { get; set; }

    public string? ListeningPromptImageUrl { get; set; }

    public string? ListeningTranscript { get; set; }


    // Reading đề bài
    public int? ReadingTaskId { get; set; }

    public string? ReadingTaskTitle { get; set; }

    public string? ReadingInstruction { get; set; }

    public string? ReadingPassageText { get; set; }
    public string? ReadingPassageTitle { get; set; }
    public int? ReadingQuestionOrderIndex { get; set; }

    // Writing đề bài
    public string? WritingInstruction { get; set; }

    public string? WritingPromptText { get; set; }

    public string? WritingPromptImageUrl { get; set; }

    public string? WritingReferenceText { get; set; }

    public string? WritingSourceText { get; set; }

    public string? WritingRequirements { get; set; }

    public int? WritingMinimumWords { get; set; }


    // Speaking đề bài
    public string? SpeakingInstruction { get; set; }

    public string? SpeakingPromptText { get; set; }

    public string? SpeakingPromptImageUrl { get; set; }

    public string? SpeakingReferenceText { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public int? SpeakingTimeSeconds { get; set; }

    public double? WritingTaskAchievement { get; set; }

    public double? WritingCoherenceAndCohesion { get; set; }

    public double? WritingLexicalResource { get; set; }

    public double? WritingGrammaticalRangeAndAccuracy { get; set; }

    public string? WritingFeedback { get; set; }

    public double? FluencyScore { get; set; }

    public double? CompletenessScore { get; set; }

    public double? GrammarScore { get; set; }

    public double? VocabularyScore { get; set; }
    
    public double? ContentRelevanceScore { get; set; }

    public double? CoherenceScore { get; set; }

    public double? OverallSpeakingScore { get; set; }

    public string? AiSpeakingFeedback { get; set; }
}
