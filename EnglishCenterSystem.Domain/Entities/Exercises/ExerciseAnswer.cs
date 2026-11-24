using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseAnswer : BaseEntity
{
    public int ExerciseAnswerId { get; set; }

    public int ExerciseAttemptId { get; set; }

    public int? QuestionId { get; set; }
    public int? SpeakingTaskId { get; set; }

    public string? SpeakingAudioUrl { get; set; }

    public string? SpeakingTranscript { get; set; }
    public int? WritingTaskId { get; set; }

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
    public int? SelectedQuestionOptionId { get; set; }
    public double? ManualScore { get; set; }
    public int? ListeningQuestionId { get; set; }

    public int? SelectedListeningOptionId { get; set; }
    public int? ReadingQuestionId { get; set; }

    public int? SelectedReadingOptionId { get; set; }

    public string? TeacherFeedback { get; set; }

    public int? GradedByTeacherUserId { get; set; }

    public DateTime? GradedAt { get; set; }
    public bool IsCorrect { get; set; }

    public double PointEarned { get; set; }

    public ExerciseAttempt ExerciseAttempt { get; set; } = null!;

    public Question? Question { get; set; }


    public QuestionOption? SelectedQuestionOption { get; set; }
    public string? TextAnswer { get; set; }

    public string? AudioAnswerUrl { get; set; }
    public double? ProsodyScore { get; set; }

    public double? WordsPerMinute { get; set; }

    public int? SpeakingWordCount { get; set; }

    public int? FillerWordCount { get; set; }

    public int? RepetitionCount { get; set; }

    public int? LongPauseCount { get; set; }

    public double? AveragePauseSeconds { get; set; }

    public string? SpeakingAnalysisJson { get; set; }
}