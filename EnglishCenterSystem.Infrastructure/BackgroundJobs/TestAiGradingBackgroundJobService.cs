using System.Text.Json;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.BackgroundJobs;

public class TestAiGradingBackgroundJobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinioService _minioService;
    private readonly IWritingAiEvaluationService _writingAiEvaluationService;
    private readonly ISpeechAssessmentService _speechAssessmentService;
    private readonly ISpeakingAiEvaluationService _speakingAiEvaluationService;
    private readonly IStudentGamificationService _studentGamificationService;

    public TestAiGradingBackgroundJobService(
        IUnitOfWork unitOfWork,
        IMinioService minioService,
        IWritingAiEvaluationService writingAiEvaluationService,
        ISpeechAssessmentService speechAssessmentService,
        ISpeakingAiEvaluationService speakingAiEvaluationService,
        IStudentGamificationService studentGamificationService)
    {
        _unitOfWork = unitOfWork;
        _minioService = minioService;
        _writingAiEvaluationService = writingAiEvaluationService;
        _speechAssessmentService = speechAssessmentService;
        _speakingAiEvaluationService = speakingAiEvaluationService;
        _studentGamificationService = studentGamificationService;
    }

    public async Task ProcessAsync(int testAttemptId)
    {
        var attempt = await _unitOfWork.TestAttempts
    .GetForAiGradingAsync(testAttemptId);

        if (attempt == null)
        {
            throw new Exception("Test attempt not found");
        }

        if (attempt.Status == TestAttemptStatus.AiGraded ||
            attempt.Status == TestAttemptStatus.Reviewed)
        {
            return;
        }

        try
        {
            attempt.Status = TestAttemptStatus.AiGrading;
            attempt.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            await GradeWritingAnswersAsync(attempt);

            await GradeSpeakingAnswersAsync(attempt);

            attempt.Score = ToeicScoreConversionHelper
                .CalculateTestScore(
                    attempt.TestAssignment.Test,
                    attempt.Answers)
                .ScoreForRanking;
            attempt.Status = TestAttemptStatus.AiGraded;
            attempt.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await _studentGamificationService.AwardTestSubmissionAsync(testAttemptId);
        }
        catch
        {
            attempt.Status = TestAttemptStatus.AiGradingFailed;
            attempt.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            throw;
        }
    }

    private async Task GradeWritingAnswersAsync(
        Domain.Entities.Tests.TestAttempt attempt)
    {
        var test = attempt.TestAssignment.Test;

        var writingAnswers = attempt.Answers
            .Where(x => x.WritingTaskId != null)
            .ToList();

        foreach (var answer in writingAnswers)
        {
            var writingLink = test.WritingTasks
                .FirstOrDefault(x =>
                    x.WritingTaskId == answer.WritingTaskId);

            if (writingLink == null)
            {
                continue;
            }

            var essayText = answer.TextAnswer ?? "";

            if (string.IsNullOrWhiteSpace(essayText))
            {
                answer.PointEarned = 0;
                answer.ManualScore = 0;
                answer.WritingOverallBand = 0;
                answer.WritingTaskAchievement = 0;
                answer.WritingCoherenceAndCohesion = 0;
                answer.WritingLexicalResource = 0;
                answer.WritingGrammaticalRangeAndAccuracy = 0;
                answer.WritingFeedback =
                    "Student did not submit a writing answer";
                answer.TeacherFeedback = answer.WritingFeedback;
                answer.IsCorrect = false;
                answer.GradedAt = DateTime.UtcNow;
                answer.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            var aiResult =
                await _writingAiEvaluationService.EvaluateAsync(
                    writingLink.WritingTask,
                    essayText);

            var earnedPoint = Math.Round(
                aiResult.OverallBand / 10 * writingLink.Point,
                2);

            answer.PointEarned = earnedPoint;
            answer.ManualScore = aiResult.OverallBand;
            answer.WritingOverallBand = aiResult.OverallBand;
            answer.WritingTaskAchievement = aiResult.TaskAchievement;
            answer.WritingCoherenceAndCohesion =
                aiResult.CoherenceAndCohesion;
            answer.WritingLexicalResource = aiResult.LexicalResource;
            answer.WritingGrammaticalRangeAndAccuracy =
                aiResult.GrammaticalRangeAndAccuracy;
            answer.WritingFeedback = aiResult.Feedback;
            answer.WritingImprovementSuggestionsJson =
                JsonSerializer.Serialize(aiResult.ImprovementSuggestions);
            answer.TeacherFeedback = aiResult.Feedback;
            answer.IsCorrect = earnedPoint > 0;
            answer.GradedAt = DateTime.UtcNow;
            answer.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task GradeSpeakingAnswersAsync(
        Domain.Entities.Tests.TestAttempt attempt)
    {
        var test = attempt.TestAssignment.Test;

        var speakingAnswers = attempt.Answers
            .Where(x => x.SpeakingTaskId != null)
            .ToList();

        foreach (var answer in speakingAnswers)
        {
            var speakingLink = test.SpeakingTasks
                .FirstOrDefault(x =>
                    x.SpeakingTaskId == answer.SpeakingTaskId);

            if (speakingLink == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(answer.SpeakingAudioObjectName))
            {
                answer.PointEarned = 0;
                answer.IsCorrect = false;
                answer.TeacherFeedback =
                    "Student did not submit speaking audio";
                answer.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            await using var audioStream =
                await _minioService.DownloadFileAsync(
                    answer.SpeakingAudioObjectName);

            var speechResult =
                await _speechAssessmentService.AssessAsync(
                    audioStream,
                    "speaking-answer.webm",
                    speakingLink.SpeakingTask.ReferenceText);

            var aiResult =
                await _speakingAiEvaluationService.EvaluateAsync(
                    speakingLink.SpeakingTask.PromptText,
                    speakingLink.SpeakingTask.ReferenceText,
                    speechResult.Transcript);

            var earnedPoint = Math.Round(
                aiResult.OverallScore / 10 * speakingLink.Point,
                2);

            answer.SpeakingTranscript = speechResult.Transcript;

            answer.PronunciationScore =
                speechResult.PronunciationScore;
            answer.AccuracyScore = speechResult.AccuracyScore;
            answer.FluencyScore = speechResult.FluencyScore;
            answer.CompletenessScore =
                speechResult.CompletenessScore;
            answer.ProsodyScore = speechResult.ProsodyScore;
            answer.WordsPerMinute = speechResult.WordsPerMinute;
            answer.SpeakingWordCount = speechResult.WordCount;
            answer.FillerWordCount = speechResult.FillerWordCount;
            answer.RepetitionCount = speechResult.RepetitionCount;
            answer.LongPauseCount = speechResult.LongPauseCount;
            answer.AveragePauseSeconds =
                speechResult.AveragePauseSeconds;
            answer.SpeakingAnalysisJson = speechResult.AnalysisJson;

            answer.GrammarScore = aiResult.GrammarScore;
            answer.VocabularyScore = aiResult.VocabularyScore;
            answer.ContentRelevanceScore =
                aiResult.ContentRelevanceScore;
            answer.CoherenceScore = aiResult.CoherenceScore;
            answer.OverallSpeakingScore = aiResult.OverallScore;
            answer.AiSpeakingFeedback = aiResult.Feedback;

            answer.PointEarned = earnedPoint;
            answer.ManualScore = Math.Round(
                aiResult.OverallScore,
                2);
            answer.TeacherFeedback = aiResult.Feedback;
            answer.IsCorrect = earnedPoint > 0;
            answer.GradedAt = DateTime.UtcNow;
            answer.UpdatedAt = DateTime.UtcNow;
        }
    }
}
