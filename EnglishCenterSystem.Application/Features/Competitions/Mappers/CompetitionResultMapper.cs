using EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Domain.Entities.Competitions;

namespace EnglishCenterSystem.Application
    .Features.Competitions.Mappers;

public class CompetitionResultMapper
{
    public CompetitionAttemptResultDto Build(
        CompetitionTestAttempt attempt)
    {
        var competitionTest =
            attempt.Assignment.CompetitionTest;

        var scoreBreakdown =
            ToeicScoreConversionHelper
                .CalculateCompetitionScore(
                    competitionTest,
                    attempt.Answers);

        return new CompetitionAttemptResultDto
        {
            CompetitionTestAssignmentId =
                attempt.CompetitionTestAssignmentId,

            CompetitionTestAttemptId =
                attempt.CompetitionTestAttemptId,

            CompetitionTitle =
                attempt.Assignment.Title,

            CompetitionTestTitle =
                competitionTest.Title,

            TestType =
                competitionTest.TestType,

            CourseType =
                competitionTest.CourseType,

            TotalQuestions =
                attempt.TotalQuestions,

            CorrectAnswers =
                attempt.CorrectAnswers,

            TotalPoints =
                attempt.TotalPoints,

            Score =
                attempt.Score,

            InternalScore =
                scoreBreakdown.InternalScore,

            ToeicListeningScore =
                scoreBreakdown.ToeicListeningScore,

            ToeicReadingScore =
                scoreBreakdown.ToeicReadingScore,

            ToeicSpeakingScore =
                scoreBreakdown.ToeicSpeakingScore,

            ToeicWritingScore =
                scoreBreakdown.ToeicWritingScore,

            ToeicTotalScore =
                scoreBreakdown.ToeicTotalScore,

            Status =
                attempt.Status,

            StartedAt =
                attempt.StartedAt,

            ExpiresAt =
                attempt.ExpiresAt,

            SubmittedAt =
                attempt.SubmittedAt,

            DeadlineAt =
                attempt.Assignment.DeadlineAt,

            IsLeaderboardAvailable =
                attempt.Assignment.Status ==
                Domain.Enums.CompetitionStatus.Ranked,

            Rank =
                attempt.Result?.Rank,

            Answers =
                attempt.Answers
                    .OrderBy(answer =>
                        GetAnswerOrder(
                            attempt,
                            answer))
                    .Select(answer =>
                        BuildAnswer(
                            attempt,
                            answer))
                    .ToList()
        };
    }

    private static CompetitionAnswerResultDto BuildAnswer(
        CompetitionTestAttempt attempt,
        CompetitionTestAnswer answer)
    {
        var competitionTest =
            attempt.Assignment.CompetitionTest;

        if (answer.ListeningQuestionId != null)
        {
            var question =
                answer.ListeningQuestion;

            var listeningLink =
                competitionTest.ListeningTasks
                    .FirstOrDefault(link =>
                        link.ListeningTask.Questions.Any(q =>
                            q.ListeningQuestionId ==
                            answer.ListeningQuestionId));

            var listeningTask =
                listeningLink?.ListeningTask;

            var orderedOptions =
                question?.Options
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

            var selected =
                orderedOptions?.FirstOrDefault(x =>
                    x.ListeningOptionId ==
                    answer.SelectedListeningOptionId);

            var correct =
                orderedOptions?.FirstOrDefault(x =>
                    x.IsCorrect);

            var resultOptions =
                orderedOptions?
                    .Select((option, index) =>
                        new CompetitionAnswerOptionResultDto
                        {
                            OptionId =
                                option.ListeningOptionId,

                            Label =
                                ((char)('A' + index))
                                    .ToString(),

                            Content =
                                option.Content,

                            IsSelected =
                                option.ListeningOptionId ==
                                answer.SelectedListeningOptionId,

                            IsCorrect =
                                option.IsCorrect
                        })
                    .ToList()
                ?? new List<CompetitionAnswerOptionResultDto>();

            return new CompetitionAnswerResultDto
            {
                CompetitionTestAnswerId =
                    answer.CompetitionTestAnswerId,

                ListeningQuestionId =
                    answer.ListeningQuestionId,

                ListeningTaskId =
                    question?.ListeningTaskId ??
                    listeningTask?.ListeningTaskId,

                ListeningTaskTitle =
                    listeningTask?.Title,

                ListeningInstruction =
                    listeningTask?.Instruction,

                ListeningPartTitle =
                    listeningTask?.PartTitle,

                ListeningDirection =
                    listeningTask?.Direction,

                ListeningLittlePart =
                    listeningTask?.LittlePart,

                ListeningAudioUrl =
                    listeningTask?.AudioUrl,

                ListeningPromptImageUrl =
                    question?.PromptImageUrl,

                ListeningTranscript =
                    listeningTask?.Transcript,

                QuestionContent =
                    question?.Content ?? "",

                Explanation =
                    question?.Explanation,

                Options =
                    resultOptions,
                
                SelectedListeningOptionId =
                    answer.SelectedListeningOptionId,

                SelectedAnswerLabel =
                    GetOptionLabel(
                        orderedOptions,
                        selected),

                CorrectAnswerLabel =
                    GetOptionLabel(
                        orderedOptions,
                        correct),

                IsCorrect =
                    answer.IsCorrect,

                PointEarned =
                    answer.PointEarned,

                TeacherFeedback =
                    answer.TeacherFeedback
            };
        }

        if (answer.ReadingQuestionId != null)
        {
            var question =
                answer.ReadingQuestion;

            var readingLink =
                competitionTest.ReadingTasks
                    .FirstOrDefault(link =>
                        link.ReadingTaskId ==
                        question?.ReadingTaskId);

            var readingTask =
                question?.ReadingTask ??
                readingLink?.ReadingTask;

            var orderedOptions =
                question?.Options
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

            var selected =
                orderedOptions?.FirstOrDefault(x =>
                    x.ReadingOptionId ==
                    answer.SelectedReadingOptionId);

            var correct =
                orderedOptions?.FirstOrDefault(x =>
                    x.IsCorrect);

            var resultOptions =
                orderedOptions?
                    .Select((option, index) =>
                        new CompetitionAnswerOptionResultDto
                        {
                            OptionId =
                                option.ReadingOptionId,

                            Label =
                                ((char)('A' + index))
                                    .ToString(),

                            Content =
                                option.Content,

                            IsSelected =
                                option.ReadingOptionId ==
                                answer.SelectedReadingOptionId,

                            IsCorrect =
                                option.IsCorrect
                        })
                    .ToList()
                ?? new List<CompetitionAnswerOptionResultDto>();

            return new CompetitionAnswerResultDto
            {
                CompetitionTestAnswerId =
                    answer.CompetitionTestAnswerId,

                ReadingQuestionId =
                    answer.ReadingQuestionId,

                ReadingQuestionOrderIndex =
                    question?.OrderIndex,

                ReadingTaskId =
                    question?.ReadingTaskId ??
                    readingTask?.ReadingTaskId,

                ReadingTaskTitle =
                    readingTask?.Title,

                ReadingInstruction =
                    readingTask?.Instruction,

                ReadingPassageTitle =
                    readingTask?.Title,

                ReadingPassageText =
                    readingTask?.PassageText,

                QuestionContent =
                    question?.Content ?? "",

                Explanation =
                    question?.Explanation,

                Options =
                    resultOptions,
            
                SelectedReadingOptionId =
                    answer.SelectedReadingOptionId,

                SelectedAnswerLabel =
                    GetOptionLabel(
                        orderedOptions,
                        selected),

                CorrectAnswerLabel =
                    GetOptionLabel(
                        orderedOptions,
                        correct),

                IsCorrect =
                    answer.IsCorrect,

                PointEarned =
                    answer.PointEarned,

                TeacherFeedback =
                    answer.TeacherFeedback
            };
        }

        if (answer.WritingTaskId != null)
        {
            var link =
                competitionTest.WritingTasks
                    .FirstOrDefault(x =>
                        x.WritingTaskId ==
                        answer.WritingTaskId);

            return new CompetitionAnswerResultDto
            {
                CompetitionTestAnswerId =
                    answer.CompetitionTestAnswerId,

                WritingTaskId =
                    answer.WritingTaskId,

                WritingTaskTitle =
                    link?.WritingTask.Title,

                WritingInstruction =
                    link?.WritingTask.Instruction,

                WritingPromptText =
                    link?.WritingTask.PromptText,

                WritingPromptImageUrl =
                    link?.WritingTask.PromptImageUrl,

                WritingReferenceText =
                    link?.WritingTask.ReferenceText,

                WritingSourceText =
                    link?.WritingTask.SourceText,

                WritingRequirements =
                    link?.WritingTask.Requirements,

                WritingMinimumWords =
                    link?.WritingTask.MinimumWords,

                QuestionContent =
                    link?.WritingTask.Title ??
                    "Writing Task",

                TextAnswer =
                    answer.TextAnswer,

                IsCorrect =
                    answer.IsCorrect,

                PointEarned =
                    answer.PointEarned,

                ManualScore =
                    answer.ManualScore,

                TeacherFeedback =
                    answer.TeacherFeedback,

                WritingOverallBand =
                    answer.WritingOverallBand,

                WritingTaskAchievement =
                    answer.WritingTaskAchievement,

                WritingCoherenceAndCohesion =
                    answer
                        .WritingCoherenceAndCohesion,

                WritingLexicalResource =
                    answer.WritingLexicalResource,

                WritingGrammaticalRangeAndAccuracy =
                    answer
                        .WritingGrammaticalRangeAndAccuracy,

                WritingFeedback =
                    answer.WritingFeedback
            };
        }

        if (answer.SpeakingTaskId != null)
        {
            var link =
                competitionTest.SpeakingTasks
                    .FirstOrDefault(x =>
                        x.SpeakingTaskId ==
                        answer.SpeakingTaskId);

            return new CompetitionAnswerResultDto
            {
                CompetitionTestAnswerId =
                    answer.CompetitionTestAnswerId,

                SpeakingTaskId =
                    answer.SpeakingTaskId,

                SpeakingTaskTitle =
                    link?.SpeakingTask.Title,

                SpeakingInstruction =
                    link?.SpeakingTask.Instruction,

                SpeakingPromptText =
                    link?.SpeakingTask.PromptText,

                SpeakingPromptImageUrl =
                    link?.SpeakingTask.PromptImageUrl,

                SpeakingReferenceText =
                    link?.SpeakingTask.ReferenceText,

                PreparationTimeSeconds =
                    link?.SpeakingTask
                        .PreparationTimeSeconds,

                SpeakingTimeSeconds =
                    link?.SpeakingTask
                        .SpeakingTimeSeconds,

                QuestionContent =
                    link?.SpeakingTask.Title ??
                    "Speaking Task",

                AudioAnswerUrl =
                    answer.AudioAnswerUrl ??
                    answer.SpeakingAudioUrl,

                SpeakingTranscript =
                    answer.SpeakingTranscript,

                IsCorrect =
                    answer.IsCorrect,

                PointEarned =
                    answer.PointEarned,

                ManualScore =
                    answer.ManualScore,

                TeacherFeedback =
                    answer.TeacherFeedback,

                PronunciationScore =
                    answer.PronunciationScore,

                AccuracyScore =
                    answer.AccuracyScore,

                FluencyScore =
                    answer.FluencyScore,

                CompletenessScore =
                    answer.CompletenessScore,

                GrammarScore =
                    answer.GrammarScore,

                VocabularyScore =
                    answer.VocabularyScore,

                ContentRelevanceScore =
                    answer.ContentRelevanceScore,

                CoherenceScore =
                    answer.CoherenceScore,

                OverallSpeakingScore =
                    answer.OverallSpeakingScore,

                AiSpeakingFeedback =
                    answer.AiSpeakingFeedback
            };
        }

        return new CompetitionAnswerResultDto
        {
            CompetitionTestAnswerId =
                answer.CompetitionTestAnswerId
        };
    }

    private static int GetAnswerOrder(
        CompetitionTestAttempt attempt,
        CompetitionTestAnswer answer)
    {
        var test =
            attempt.Assignment.CompetitionTest;

        var order = 0;

        foreach (var link in test.ListeningTasks
            .OrderBy(x => x.OrderIndex))
        {
            foreach (var question in link.ListeningTask.Questions
                .OrderBy(x => x.OrderIndex))
            {
                order++;

                if (question.ListeningQuestionId ==
                    answer.ListeningQuestionId)
                {
                    return order;
                }
            }
        }

        foreach (var link in test.ReadingTasks
            .OrderBy(x => x.OrderIndex))
        {
            foreach (var question in link.ReadingTask.Questions
                .OrderBy(x => x.OrderIndex))
            {
                order++;

                if (question.ReadingQuestionId ==
                    answer.ReadingQuestionId)
                {
                    return order;
                }
            }
        }

        if (answer.WritingTaskId != null)
        {
            return 30000 +
                   answer.WritingTaskId.Value;
        }

        if (answer.SpeakingTaskId != null)
        {
            return 40000 +
                   answer.SpeakingTaskId.Value;
        }

        return 99999;
    }

    private static string? GetOptionLabel<TOption>(
        List<TOption>? options,
        TOption? option)
    {
        if (options == null ||
            option == null)
        {
            return null;
        }

        var index =
            options.IndexOf(option);

        return index < 0
            ? null
            : ((char)('A' + index))
                .ToString();
    }
}
