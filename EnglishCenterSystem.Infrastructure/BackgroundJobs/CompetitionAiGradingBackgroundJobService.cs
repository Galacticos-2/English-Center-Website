using System.Text.Json;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Application
    .Features.Competitions.Services;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Domain.Enums;
using Hangfire;

namespace EnglishCenterSystem.Infrastructure.BackgroundJobs;

public class CompetitionAiGradingBackgroundJobService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IMinioService
        _minioService;

    private readonly IWritingAiEvaluationService
        _writingAiEvaluationService;

    private readonly ISpeechAssessmentService
        _speechAssessmentService;

    private readonly ISpeakingAiEvaluationService
        _speakingAiEvaluationService;

    private readonly CompetitionRankingService
        _competitionRankingService;

    public CompetitionAiGradingBackgroundJobService(
        IUnitOfWork unitOfWork,
        IMinioService minioService,
        IWritingAiEvaluationService
            writingAiEvaluationService,
        ISpeechAssessmentService
            speechAssessmentService,
        ISpeakingAiEvaluationService
            speakingAiEvaluationService,
        CompetitionRankingService
            competitionRankingService)
    {
        _unitOfWork =
            unitOfWork;

        _minioService =
            minioService;

        _writingAiEvaluationService =
            writingAiEvaluationService;

        _speechAssessmentService =
            speechAssessmentService;

        _speakingAiEvaluationService =
            speakingAiEvaluationService;

        _competitionRankingService =
            competitionRankingService;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ProcessAsync(
        int competitionTestAttemptId)
    {
        var attempt =
            await _unitOfWork
                .CompetitionTestAttempts
                .GetForAiGradingAsync(
                    competitionTestAttemptId);

        if (attempt == null)
        {
            throw new Exception(
                "Competition attempt not found");
        }

        if (attempt.Status is
            CompetitionAttemptStatus.AiGraded or
            CompetitionAttemptStatus.Reviewed)
        {
            return;
        }

        if (attempt.SubmittedAt == null)
        {
            throw new Exception(
                "Competition attempt has not been submitted");
        }

        try
        {
            attempt.Status =
                CompetitionAttemptStatus.AiGrading;

            attempt.UpdatedAt =
                DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            await GradeWritingAnswersAsync(
                attempt);

            await GradeSpeakingAnswersAsync(
                attempt);

            attempt.Score =
                ToeicScoreConversionHelper
                    .CalculateCompetitionScore(
                        attempt.Assignment.CompetitionTest,
                        attempt.Answers)
                    .ScoreForRanking;

            attempt.CorrectAnswers =
                attempt.Answers.Count(x =>
                    x.IsCorrect ||
                    ((x.WritingTaskId != null ||
                      x.SpeakingTaskId != null) &&
                     x.PointEarned > 0));

            attempt.Status =
                CompetitionAttemptStatus.AiGraded;

            attempt.UpdatedAt =
                DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            /*
             * Giữ lại điểm objective hoặc phần AI
             * nào đã chấm được.
             */
            attempt.Score =
                ToeicScoreConversionHelper
                    .CalculateCompetitionScore(
                        attempt.Assignment.CompetitionTest,
                        attempt.Answers)
                    .ScoreForRanking;

            attempt.Status =
                CompetitionAttemptStatus
                    .AiGradingFailed;

            attempt.UpdatedAt =
                DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            await TryFinalizeCompetitionAsync(
                attempt.Assignment);

            throw;
        }

        await TryFinalizeCompetitionAsync(
            attempt.Assignment);
    }

    private async Task GradeWritingAnswersAsync(
        CompetitionTestAttempt attempt)
    {
        var competitionTest =
            attempt.Assignment.CompetitionTest;

        var writingAnswers =
            attempt.Answers
                .Where(x =>
                    x.WritingTaskId != null)
                .ToList();

        foreach (var answer in writingAnswers)
        {
            var writingLink =
                competitionTest.WritingTasks
                    .FirstOrDefault(x =>
                        x.WritingTaskId ==
                        answer.WritingTaskId);

            if (writingLink == null)
            {
                continue;
            }

            var essayText =
                answer.TextAnswer ?? "";

            if (string.IsNullOrWhiteSpace(
                essayText))
            {
                answer.PointEarned = 0;
                answer.IsCorrect = false;

                answer.TeacherFeedback =
                    "Student did not submit a writing answer";

                answer.WritingFeedback =
                    answer.TeacherFeedback;

                answer.GradedAt =
                    DateTime.UtcNow;

                answer.UpdatedAt =
                    DateTime.UtcNow;

                continue;
            }

            var aiResult =
                await _writingAiEvaluationService
                    .EvaluateAsync(
                        writingLink.WritingTask,
                        essayText);

            var earnedPoint =
                Math.Round(
                    aiResult.OverallBand /
                    10 *
                    writingLink.Point,
                    2);

            answer.PointEarned =
                earnedPoint;

            answer.ManualScore =
                aiResult.OverallBand;

            answer.WritingOverallBand =
                aiResult.OverallBand;

            answer.WritingTaskAchievement =
                aiResult.TaskAchievement;

            answer.WritingCoherenceAndCohesion =
                aiResult.CoherenceAndCohesion;

            answer.WritingLexicalResource =
                aiResult.LexicalResource;

            answer
                .WritingGrammaticalRangeAndAccuracy =
                aiResult
                    .GrammaticalRangeAndAccuracy;

            answer.WritingFeedback =
                aiResult.Feedback;

            answer
                .WritingImprovementSuggestionsJson =
                JsonSerializer.Serialize(
                    aiResult
                        .ImprovementSuggestions);

            answer.TeacherFeedback =
                aiResult.Feedback;

            answer.IsCorrect =
                earnedPoint > 0;

            answer.GradedAt =
                DateTime.UtcNow;

            answer.UpdatedAt =
                DateTime.UtcNow;
        }
    }

    private async Task GradeSpeakingAnswersAsync(
        CompetitionTestAttempt attempt)
    {
        var competitionTest =
            attempt.Assignment.CompetitionTest;

        var speakingAnswers =
            attempt.Answers
                .Where(x =>
                    x.SpeakingTaskId != null)
                .ToList();

        foreach (var answer in speakingAnswers)
        {
            var speakingLink =
                competitionTest.SpeakingTasks
                    .FirstOrDefault(x =>
                        x.SpeakingTaskId ==
                        answer.SpeakingTaskId);

            if (speakingLink == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                answer.SpeakingAudioObjectName))
            {
                answer.PointEarned = 0;
                answer.IsCorrect = false;

                answer.TeacherFeedback =
                    "Student did not submit speaking audio";

                answer.AiSpeakingFeedback =
                    answer.TeacherFeedback;

                answer.GradedAt =
                    DateTime.UtcNow;

                answer.UpdatedAt =
                    DateTime.UtcNow;

                continue;
            }

            await using var audioStream =
                await _minioService
                    .DownloadFileAsync(
                        answer
                            .SpeakingAudioObjectName);

            var speechResult =
                await _speechAssessmentService
                    .AssessAsync(
                        audioStream,
                        "competition-speaking-answer.webm",
                        speakingLink
                            .SpeakingTask
                            .ReferenceText);

            var aiResult =
                await _speakingAiEvaluationService
                    .EvaluateAsync(
                        speakingLink
                            .SpeakingTask
                            .PromptText,

                        speakingLink
                            .SpeakingTask
                            .ReferenceText,

                        speechResult.Transcript);

            var earnedPoint =
                Math.Round(
                    aiResult.OverallScore /
                    10 *
                    speakingLink.Point,
                    2);

            answer.SpeakingTranscript =
                speechResult.Transcript;

            answer.PronunciationScore =
                speechResult.PronunciationScore;

            answer.AccuracyScore =
                speechResult.AccuracyScore;

            answer.FluencyScore =
                speechResult.FluencyScore;

            answer.CompletenessScore =
                speechResult.CompletenessScore;

            answer.ProsodyScore =
                speechResult.ProsodyScore;

            answer.WordsPerMinute =
                speechResult.WordsPerMinute;

            answer.SpeakingWordCount =
                speechResult.WordCount;

            answer.FillerWordCount =
                speechResult.FillerWordCount;

            answer.RepetitionCount =
                speechResult.RepetitionCount;

            answer.LongPauseCount =
                speechResult.LongPauseCount;

            answer.AveragePauseSeconds =
                speechResult.AveragePauseSeconds;

            answer.SpeakingAnalysisJson =
                speechResult.AnalysisJson;

            answer.GrammarScore =
                aiResult.GrammarScore;

            answer.VocabularyScore =
                aiResult.VocabularyScore;

            answer.ContentRelevanceScore =
                aiResult.ContentRelevanceScore;

            answer.CoherenceScore =
                aiResult.CoherenceScore;

            answer.OverallSpeakingScore =
                aiResult.OverallScore;

            answer.AiSpeakingFeedback =
                aiResult.Feedback;

            answer.PointEarned =
                earnedPoint;

            answer.ManualScore =
                Math.Round(
                    aiResult.OverallScore,
                    2);

            answer.TeacherFeedback =
                aiResult.Feedback;

            answer.IsCorrect =
                earnedPoint > 0;

            answer.GradedAt =
                DateTime.UtcNow;

            answer.UpdatedAt =
                DateTime.UtcNow;
        }
    }

    private async Task TryFinalizeCompetitionAsync(
        CompetitionTestAssignment assignment)
    {
        if (DateTime.UtcNow <
            assignment.DeadlineAt)
        {
            return;
        }

        await _competitionRankingService
            .FinalizeAsync(
                assignment
                    .CompetitionTestAssignmentId);
    }
}
