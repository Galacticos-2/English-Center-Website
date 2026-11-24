using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;


public class ExerciseAnswerOptionResultDto
{
    public int OptionId { get; set; }

    public string Label { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool IsSelected { get; set; }

    public bool IsCorrect { get; set; }
}


public class ExerciseAnswerResultDto
{
    public int ExerciseAnswerId { get; set; }

    /*
     * Grammar / question bank
     */
    public int? QuestionId { get; set; }

    /*
     * Listening
     */
    public int? ListeningQuestionId { get; set; }

    public int? ListeningTaskId { get; set; }

    public string? ListeningPromptImageUrl { get; set; }

    /*
     * Reading
     */
    public int? ReadingQuestionId { get; set; }

    public int? ReadingTaskId { get; set; }

    /*
     * Writing
     */
    public int? WritingTaskId { get; set; }

    public string? WritingTaskTitle { get; set; }

    public string? WritingInstruction { get; set; }

    public string? WritingPromptText { get; set; }

    public string? WritingPromptImageUrl { get; set; }

    public string? WritingReferenceText { get; set; }

    public string? WritingSourceText { get; set; }

    public string? WritingRequirements { get; set; }

    public int? WritingMinimumWords { get; set; }

    /*
     * Speaking
     */
    public int? SpeakingTaskId { get; set; }

    public string QuestionContent { get; set; } = null!;

    public string? Explanation { get; set; }

    /*
     * Có thể giữ hai field cũ để không phá code hiện tại.
     */
    public int? SelectedQuestionOptionId { get; set; }

    public int? CorrectQuestionOptionId { get; set; }

    /*
     * Field mới dùng trực tiếp cho frontend.
     */
    public string? SelectedAnswerLabel { get; set; }
    public string? ReadingTaskTitle { get; set; }

    public string? ReadingPassageTitle { get; set; }

    public string? ReadingInstruction { get; set; }

    public string? ReadingPassageText { get; set; }
    public string? CorrectAnswerLabel { get; set; }

    public List<ExerciseAnswerOptionResultDto> Options
    {
        get;
        set;
    } = new();

    public string? TextAnswer { get; set; }

    public string? AudioAnswerUrl { get; set; }

    public bool IsCorrect { get; set; }

    public double PointEarned { get; set; }

    public double? ManualScore { get; set; }

    public string? TeacherFeedback { get; set; }

    public int? GradedByTeacherUserId { get; set; }

    public DateTime? GradedAt { get; set; }

    public double? WritingOverallBand { get; set; }

    public double? WritingTaskAchievement { get; set; }

    public double? WritingCoherenceAndCohesion { get; set; }

    public double? WritingLexicalResource { get; set; }

    public double?
        WritingGrammaticalRangeAndAccuracy
    {
        get;
        set;
    }

    public string? WritingFeedback { get; set; }

    public string?
        WritingImprovementSuggestionsJson
    {
        get;
        set;
    }

    public string? WritingStatus { get; set; }

    /*
     * Speaking result
     */
    public string? SpeakingTaskTitle { get; set; }

    public string? SpeakingInstruction { get; set; }

    public string? SpeakingPromptText { get; set; }

    public string? SpeakingPromptImageUrl { get; set; }

    public string? SpeakingReferenceText { get; set; }

    public string? SpeakingTranscript { get; set; }

    public double? OverallSpeakingScore { get; set; }

    public double? PronunciationScore { get; set; }

    public double? FluencyScore { get; set; }

    public double? GrammarScore { get; set; }

    public double? VocabularyScore { get; set; }

    public string? AiSpeakingFeedback { get; set; }
}
