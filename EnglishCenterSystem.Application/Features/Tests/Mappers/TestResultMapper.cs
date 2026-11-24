using EnglishCenterSystem.Application.Features.Reading.DTOs;
using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Domain.Entities.Tests;

namespace EnglishCenterSystem.Application.Features.Tests.Mappers;

public class TestResultMapper
{
    public TestResultDto BuildResultDto(TestAttempt attempt)
    {
        var test = GetTest(attempt);
        var scoreBreakdown =
            ToeicScoreConversionHelper
                .CalculateTestScore(
                    test,
                    attempt.Answers);
        return new TestResultDto
        {
            TestAttemptId = attempt.TestAttemptId,
            TestTitle = test.Title,
            TestType = test.TestType,
            CourseType = test.CourseType,
            TotalQuestions = attempt.TotalQuestions,
            CorrectAnswers = attempt.CorrectAnswers,
            TotalPoints = attempt.TotalPoints,
            Score = attempt.Score,
            InternalScore = scoreBreakdown.InternalScore,
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
            SourceFileUrl =
    attempt.TestAssignment.Test.SourceFileUrl,

            SourceFileType =
    GetFileExtensionFromUrl(
        attempt.TestAssignment.Test.SourceFileUrl),
            Status = attempt.Status.ToString(),
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt ?? attempt.StartedAt,
            Answers = attempt.Answers
    .OrderBy(answer => GetAnswerOrder(attempt, answer))
    .Select(answer => BuildAnswerResultDto(attempt, answer))
    .ToList()
        };
    }
    private static string? GetFileExtensionFromUrl(
    string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return null;
        }

        if (Uri.TryCreate(
                fileUrl,
                UriKind.Absolute,
                out var uri))
        {
            return Path
                .GetExtension(uri.AbsolutePath)
                .ToLowerInvariant();
        }

        return Path
            .GetExtension(fileUrl)
            .ToLowerInvariant();
    }
    private TestAnswerResultDto BuildAnswerResultDto(
        TestAttempt attempt,
        TestAnswer answer)
    {
        var test = GetTest(attempt);

        if (answer.ListeningQuestionId != null)
        {
            var question = answer.ListeningQuestion;
            var listeningLink = test.ListeningTasks
    .FirstOrDefault(link =>
        link.ListeningTask.Questions.Any(q =>
            q.ListeningQuestionId ==
            answer.ListeningQuestionId));

            var listeningTask =
                listeningLink?.ListeningTask;

            var orderedOptions = question?.Options
                .OrderBy(x => x.OrderIndex)
                .ToList();

            var selectedOption = orderedOptions?
                .FirstOrDefault(x =>
                    x.ListeningOptionId == answer.SelectedListeningOptionId);

            var correctOption = orderedOptions?
                .FirstOrDefault(x => x.IsCorrect);
            var resultOptions =
    orderedOptions?
        .Select((option, index) =>
            new TestAnswerOptionResultDto
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
    ?? new List<TestAnswerOptionResultDto>();
            return new TestAnswerResultDto
            {
                TestAnswerId =
            answer.TestAnswerId,

                QuestionId = 0,

                ListeningQuestionId =
            answer.ListeningQuestionId,
                Options =
    resultOptions,
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

                SelectedListeningOptionId =
            answer.SelectedListeningOptionId,

                SelectedAnswerLabel =
            GetOptionLabel(
                orderedOptions,
                selectedOption),

                CorrectAnswerLabel =
            GetOptionLabel(
                orderedOptions,
                correctOption),

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

            var readingLink = test.ReadingTasks
                .FirstOrDefault(link =>
                    link.ReadingTaskId ==
                    question?.ReadingTaskId);

            var readingTask =
                question?.ReadingTask ??
                readingLink?.ReadingTask;

            var orderedOptions = question?.Options
                .OrderBy(x => x.OrderIndex)
                .ToList();

            var selectedOption = orderedOptions?
                .FirstOrDefault(x =>
                    x.ReadingOptionId ==
                    answer.SelectedReadingOptionId);

            var correctOption = orderedOptions?
                .FirstOrDefault(x =>
                    x.IsCorrect);

            var resultOptions =
                orderedOptions?
                    .Select((option, index) =>
                        new TestAnswerOptionResultDto
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
                ?? new List<TestAnswerOptionResultDto>();

            return new TestAnswerResultDto
            {
                TestAnswerId =
                    answer.TestAnswerId,

                QuestionId = 0,

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
                ReadingPassageImages =
    answer.ReadingQuestion?
        .ReadingTask?
        .PassageImages
        .OrderBy(image =>
            image.OrderIndex)
        .Select(image =>
            new ReadingPassageImageDto
            {
                ReadingPassageImageId =
                    image.ReadingPassageImageId,

                ImageUrl =
                    image.ImageUrl,

                PageNumber =
                    image.PageNumber,

                OrderIndex =
                    image.OrderIndex
            })
        .ToList()
    ?? new List<ReadingPassageImageDto>(),

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
                        selectedOption),

                CorrectAnswerLabel =
                    GetOptionLabel(
                        orderedOptions,
                        correctOption),

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
            var writingLink = test.WritingTasks
                .FirstOrDefault(x =>
                    x.WritingTaskId ==
                    answer.WritingTaskId);

            return new TestAnswerResultDto
            {
                TestAnswerId =
        answer.TestAnswerId,

                QuestionId = 0,

                WritingTaskId =
        answer.WritingTaskId,

                WritingTaskTitle =
        writingLink?.WritingTask.Title,

                WritingInstruction =
        writingLink?.WritingTask.Instruction,

                WritingPromptText =
        writingLink?.WritingTask.PromptText,

                WritingPromptImageUrl =
        writingLink?.WritingTask.PromptImageUrl,

                WritingReferenceText =
        writingLink?.WritingTask.ReferenceText,

                WritingSourceText =
        writingLink?.WritingTask.SourceText,

                WritingRequirements =
        writingLink?.WritingTask.Requirements,

                WritingMinimumWords =
        writingLink?.WritingTask.MinimumWords,

                QuestionContent =
        writingLink?.WritingTask.Title ??
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
        answer.WritingCoherenceAndCohesion,

                WritingLexicalResource =
        answer.WritingLexicalResource,

                WritingGrammaticalRangeAndAccuracy =
        answer.WritingGrammaticalRangeAndAccuracy,

                WritingFeedback =
        answer.WritingFeedback ??
        answer.TeacherFeedback
            };
        }

        if (answer.SpeakingTaskId != null)
        {
            var speakingLink = test.SpeakingTasks
                .FirstOrDefault(x =>
                    x.SpeakingTaskId ==
                    answer.SpeakingTaskId);

            return new TestAnswerResultDto
            {
                TestAnswerId =
        answer.TestAnswerId,

                QuestionId = 0,

                SpeakingTaskId =
        answer.SpeakingTaskId,

                SpeakingTaskTitle =
        speakingLink?.SpeakingTask.Title,

                SpeakingInstruction =
        speakingLink?.SpeakingTask.Instruction,

                SpeakingPromptText =
        speakingLink?.SpeakingTask.PromptText,

                SpeakingPromptImageUrl =
        speakingLink?.SpeakingTask.PromptImageUrl,

                SpeakingReferenceText =
        speakingLink?.SpeakingTask.ReferenceText,

                PreparationTimeSeconds =
    speakingLink?.SpeakingTask
        .PreparationTimeSeconds,

                SpeakingTimeSeconds =
    speakingLink?.SpeakingTask
        .SpeakingTimeSeconds,
                QuestionContent =
        speakingLink?.SpeakingTask.Title ??
        "Speaking Task",

                AudioAnswerUrl =
        answer.AudioAnswerUrl,

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

        return new TestAnswerResultDto
        {
            TestAnswerId = answer.TestAnswerId,
            QuestionId = 0,
            QuestionContent = ""
        };
    }
    private int GetAnswerOrder(TestAttempt attempt, TestAnswer answer)
    {
        var test = GetTest(attempt);

        if (answer.ListeningQuestionId != null)
        {
            var order = 0;

            foreach (var listeningLink in test.ListeningTasks.OrderBy(x => x.OrderIndex))
            {
                foreach (var question in listeningLink.ListeningTask.Questions.OrderBy(x => x.OrderIndex))
                {
                    order++;

                    if (question.ListeningQuestionId == answer.ListeningQuestionId)
                    {
                        return order;
                    }
                }
            }

            return 10000;
        }

        if (answer.ReadingQuestionId != null)
        {
            var order = 10000;

            foreach (var readingLink in test.ReadingTasks.OrderBy(x => x.OrderIndex))
            {
                foreach (var question in readingLink.ReadingTask.Questions.OrderBy(x => x.OrderIndex))
                {
                    order++;

                    if (question.ReadingQuestionId == answer.ReadingQuestionId)
                    {
                        return order;
                    }
                }
            }

            return 20000;
        }

        if (answer.WritingTaskId != null)
        {
            return 30000 + (answer.WritingTaskId ?? 0);
        }

        if (answer.SpeakingTaskId != null)
        {
            return 40000 + (answer.SpeakingTaskId ?? 0);
        }

        return 99999;
    }
    private Test GetTest(TestAttempt attempt)
    {
        if (attempt.TestAssignment?.Test != null)
        {
            return attempt.TestAssignment.Test;
        }

       

        throw new Exception(
            "Test attempt is not linked to a test source");
    }
    private string? GetOptionLabel<TOption>(
    List<TOption>? options,
    TOption? option)
    {
        if (options == null || option == null)
        {
            return null;
        }

        var index = options.IndexOf(option);

        if (index < 0)
        {
            return null;
        }

        return ((char)('A' + index)).ToString();
    }
}
