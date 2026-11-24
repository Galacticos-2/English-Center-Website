using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Entities.Listening;
using EnglishCenterSystem.Domain.Entities.Reading;
using EnglishCenterSystem.Domain.Entities.Speaking;
using EnglishCenterSystem.Domain.Entities.Writing;

namespace EnglishCenterSystem.Domain.Entities.Competitions;

public class CompetitionTestAnswer : BaseEntity
{
    public int CompetitionTestAnswerId { get; set; }

    public int CompetitionTestAttemptId { get; set; }

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

    public string? SpeakingAudioUrl { get; set; }

    public string? SpeakingAudioObjectName { get; set; }

    public string? SpeakingTranscript { get; set; }

    public bool IsCorrect { get; set; }

    public double PointEarned { get; set; }

    public double? ManualScore { get; set; }

    public string? TeacherFeedback { get; set; }

    public int? GradedByTeacherUserId { get; set; }

    public DateTime? GradedAt { get; set; }

    // Writing AI grading

    public double? WritingOverallBand { get; set; }

    public double? WritingTaskAchievement { get; set; }

    public double? WritingCoherenceAndCohesion { get; set; }

    public double? WritingLexicalResource { get; set; }

    public double? WritingGrammaticalRangeAndAccuracy { get; set; }

    public string? WritingFeedback { get; set; }

    public string? WritingImprovementSuggestionsJson { get; set; }

    // Speaking assessment

    public double? PronunciationScore { get; set; }

    public double? AccuracyScore { get; set; }

    public double? FluencyScore { get; set; }

    public double? CompletenessScore { get; set; }

    public double? GrammarScore { get; set; }

    public double? VocabularyScore { get; set; }

    public double? ContentRelevanceScore { get; set; }

    public double? CoherenceScore { get; set; }

    public double? OverallSpeakingScore { get; set; }

    public string? AiSpeakingFeedback { get; set; }

    public double? ProsodyScore { get; set; }

    public double? WordsPerMinute { get; set; }

    public int? SpeakingWordCount { get; set; }

    public int? FillerWordCount { get; set; }

    public int? RepetitionCount { get; set; }

    public int? LongPauseCount { get; set; }

    public double? AveragePauseSeconds { get; set; }

    public string? SpeakingAnalysisJson { get; set; }

    // Navigation

    public CompetitionTestAttempt Attempt { get; set; } = null!;

    public Question? Question { get; set; }

    public QuestionOption? SelectedQuestionOption { get; set; }

    public ListeningQuestion? ListeningQuestion { get; set; }

    public ListeningOption? SelectedListeningOption { get; set; }

    public ReadingQuestion? ReadingQuestion { get; set; }

    public ReadingOption? SelectedReadingOption { get; set; }

    public WritingTask? WritingTask { get; set; }

    public SpeakingTask? SpeakingTask { get; set; }
}