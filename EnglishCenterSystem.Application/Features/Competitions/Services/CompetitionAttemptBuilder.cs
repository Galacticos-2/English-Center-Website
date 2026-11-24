using EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application
    .Features.Competitions.Services;

public class CompetitionAttemptBuilder
{
    public CompetitionAttemptDto Build(
        CompetitionTestAttempt attempt)
    {
        var assignment = attempt.Assignment;
        var competitionTest = assignment.CompetitionTest;

        return new CompetitionAttemptDto
        {
            CompetitionTestAttemptId =
                attempt.CompetitionTestAttemptId,

            CompetitionTestAssignmentId =
                assignment.CompetitionTestAssignmentId,

            CompetitionTestId =
                competitionTest.CompetitionTestId,

            CompetitionTitle =
                assignment.Title,

            CompetitionTestTitle =
                competitionTest.Title,

            StartedAt =
    EnsureUtc(
        attempt.StartedAt
    ),

            ExpiresAt =
    EnsureUtc(
        attempt.ExpiresAt
    ),

            DeadlineAt =
    EnsureUtc(
        assignment.DeadlineAt
    ),

            ServerTime =
    DateTime.UtcNow,

            DurationMinutes =
                assignment.DurationMinutes,

            Questions =
                BuildQuestions(competitionTest)
        };
    }

    private static List<CompetitionQuestionDto>
        BuildQuestions(
            CompetitionTest competitionTest)
    {
        var result =
            new List<CompetitionQuestionDto>();

        var globalOrderIndex = 0;

        BuildListeningQuestions(
            competitionTest,
            result,
            ref globalOrderIndex);

        BuildReadingQuestions(
            competitionTest,
            result,
            ref globalOrderIndex);

        BuildWritingQuestions(
            competitionTest,
            result,
            ref globalOrderIndex);

        BuildSpeakingQuestions(
            competitionTest,
            result,
            ref globalOrderIndex);

        return result;
    }

    private static void BuildListeningQuestions(
        CompetitionTest competitionTest,
        List<CompetitionQuestionDto> result,
        ref int globalOrderIndex)
    {
        foreach (var link in competitionTest.ListeningTasks
            .OrderBy(x => x.OrderIndex))
        {
            var questions = link.ListeningTask.Questions
                .OrderBy(x => x.OrderIndex)
                .ToList();

            var pointPerQuestion =
                questions.Count == 0
                    ? 0
                    : Math.Round(
                        link.Point / questions.Count,
                        4);

            foreach (var question in questions)
            {
                globalOrderIndex++;

                result.Add(new CompetitionQuestionDto
                {
                    ListeningQuestionId =
                        question.ListeningQuestionId,

                    ListeningTaskId =
                        link.ListeningTaskId,

                    Content =
                        question.Content,

                    QuestionType =
                        QuestionType.SingleChoice,

                    OrderIndex =
                        globalOrderIndex,

                    Point =
                        pointPerQuestion,

                    IsListeningQuestion =
                        true,

                    ListeningCourseType =
                        link.ListeningTask.CourseType,

                    ListeningPartTitle =
                        link.ListeningTask.PartTitle,

                    ListeningDirection =
                        link.ListeningTask.Direction,

                    ListeningLittlePart =
                        link.ListeningTask.LittlePart,

                    AudioUrl =
                        link.ListeningTask.AudioUrl,

                    ListeningPromptImageUrl =
                        question.PromptImageUrl,

                    Transcript =
                        link.ListeningTask.Transcript,

                    Options = question.Options
                        .OrderBy(x => x.OrderIndex)
                        .Select(option =>
                            new CompetitionQuestionOptionDto
                            {
                                ListeningOptionId =
                                    option.ListeningOptionId,

                                Content =
                                    option.Content,

                                OrderIndex =
                                    option.OrderIndex
                            })
                        .ToList()
                });
            }
        }
    }
    private static DateTime EnsureUtc(
    DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc =>
                value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            _ =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc
                )
        };
    }
    private static void BuildReadingQuestions(
        CompetitionTest competitionTest,
        List<CompetitionQuestionDto> result,
        ref int globalOrderIndex)
    {
        foreach (var link in competitionTest.ReadingTasks
            .OrderBy(x => x.OrderIndex))
        {
            var questions = link.ReadingTask.Questions
                .OrderBy(x => x.OrderIndex)
                .ToList();

            var pointPerQuestion =
                questions.Count == 0
                    ? 0
                    : Math.Round(
                        link.Point / questions.Count,
                        4);

            foreach (var question in questions)
            {
                globalOrderIndex++;

                result.Add(new CompetitionQuestionDto
                {
                    ReadingQuestionId =
                        question.ReadingQuestionId,

                    ReadingTaskId =
                        link.ReadingTaskId,

                    Content =
                        question.Content,

                    QuestionType =
                        QuestionType.SingleChoice,

                    OrderIndex =
                        globalOrderIndex,

                    Point =
                        pointPerQuestion,

                    IsReadingQuestion =
                        true,

                    ReadingPassageTitle =
                        link.ReadingTask.Title,

                    ReadingPassageText =
                        link.ReadingTask.PassageText,

                    ReadingInstruction =
                        link.ReadingTask.Instruction,

                    ReadingPassageImages =
                        link.ReadingTask.PassageImages
                            .OrderBy(x => x.OrderIndex)
                            .Select(image =>
                                new EnglishCenterSystem.Application
                                    .Features.Reading.DTOs
                                    .ReadingPassageImageDto
                                {
                                    ReadingPassageImageId =
                                        image.ReadingPassageImageId,
                                    ImageUrl = image.ImageUrl,
                                    PageNumber = image.PageNumber,
                                    OrderIndex = image.OrderIndex
                                })
                            .ToList(),

                    Options = question.Options
                        .OrderBy(x => x.OrderIndex)
                        .Select(option =>
                            new CompetitionQuestionOptionDto
                            {
                                ReadingOptionId =
                                    option.ReadingOptionId,

                                Content =
                                    option.Content,

                                OrderIndex =
                                    option.OrderIndex
                            })
                        .ToList()
                });
            }
        }
    }

    private static void BuildWritingQuestions(
        CompetitionTest competitionTest,
        List<CompetitionQuestionDto> result,
        ref int globalOrderIndex)
    {
        foreach (var link in competitionTest.WritingTasks
            .OrderBy(x => x.OrderIndex))
        {
            globalOrderIndex++;

            result.Add(new CompetitionQuestionDto
            {
                WritingTaskId =
                    link.WritingTaskId,

                Content =
                    link.WritingTask.Title,

                QuestionType =
                    QuestionType.TextAnswer,

                OrderIndex =
                    globalOrderIndex,

                Point =
                    link.Point,

                IsWritingQuestion =
                    true,

                WritingInstruction =
                    link.WritingTask.Instruction,

                WritingPromptText =
                    link.WritingTask.PromptText,

                WritingPromptImageUrl =
                    link.WritingTask.PromptImageUrl,

                WritingReferenceText =
                    link.WritingTask.ReferenceText,

                WritingSourceText =
                    link.WritingTask.SourceText,

                WritingRequirements =
                    link.WritingTask.Requirements,

                WritingScoringCriteria =
                    link.WritingTask.ScoringCriteria,

                WritingMinimumWords =
                    link.WritingTask.MinimumWords,

                WritingTaskType =
                    link.WritingTask.TaskType.ToString()
            });
        }
    }

    private static void BuildSpeakingQuestions(
        CompetitionTest competitionTest,
        List<CompetitionQuestionDto> result,
        ref int globalOrderIndex)
    {
        foreach (var link in competitionTest.SpeakingTasks
            .OrderBy(x => x.OrderIndex))
        {
            globalOrderIndex++;

            result.Add(new CompetitionQuestionDto
            {
                SpeakingTaskId =
                    link.SpeakingTaskId,

                Content =
                    link.SpeakingTask.Title,

                QuestionType =
                    QuestionType.AudioAnswer,

                OrderIndex =
                    globalOrderIndex,

                Point =
                    link.Point,

                IsSpeakingQuestion =
                    true,

                SpeakingPdfUrl =
                    link.SpeakingTask.PdfUrl,

                SpeakingInstruction =
                    link.SpeakingTask.Instruction,

                SpeakingPromptText =
                    link.SpeakingTask.PromptText,

                SpeakingPromptImageUrl =
                    link.SpeakingTask.PromptImageUrl,

                SpeakingReferenceText =
                    link.SpeakingTask.ReferenceText,

                PreparationTimeSeconds =
                    link.SpeakingTask
                        .PreparationTimeSeconds,

                SpeakingTimeSeconds =
                    link.SpeakingTask
                        .SpeakingTimeSeconds
            });
        }
    }
}
