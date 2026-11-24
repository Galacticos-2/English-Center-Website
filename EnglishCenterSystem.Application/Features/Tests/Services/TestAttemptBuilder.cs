using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Domain.Enums;

using EnglishCenterSystem.Application.Features.Reading.DTOs;

namespace EnglishCenterSystem.Application.Features.Tests.Services;

public class TestAttemptBuilder
{
    public TestAttemptDto Build(
    TestAttempt attempt,
    Test test,
    int testAssignmentId)
    {
        var questions = BuildQuestions(test);

        return new TestAttemptDto
        {
            TestAttemptId = attempt.TestAttemptId,
            TestAssignmentId = testAssignmentId,
       
            TestTitle = test.Title,
            StartedAt = attempt.StartedAt,
          
            Questions = questions
                .OrderBy(x => x.OrderIndex)
                .ToList()
        };
    }

    private List<StudentTestQuestionDto> BuildQuestions(
        Test test)
    {
        var questions =
            new List<StudentTestQuestionDto>();

        var globalOrderIndex = 0;

        BuildListeningQuestions(
            test,
            questions,
            ref globalOrderIndex);

        BuildReadingQuestions(
            test,
            questions,
            ref globalOrderIndex);

        BuildWritingQuestions(
            test,
            questions,
            ref globalOrderIndex);

        BuildSpeakingQuestions(
            test,
            questions,
            ref globalOrderIndex);

        return questions;
    }
    private void BuildListeningQuestions(
    Test test,
    List<StudentTestQuestionDto> questions,
    ref int globalOrderIndex)
    {
        foreach (var listeningLink in test.ListeningTasks
            .OrderBy(x => x.OrderIndex))
        {
            foreach (var question in listeningLink
                .ListeningTask
                .Questions
                .OrderBy(x => x.OrderIndex))
            {
                globalOrderIndex++;

                questions.Add(new StudentTestQuestionDto
                {
                    QuestionId = 0,

                    ListeningQuestionId =
                        question.ListeningQuestionId,

                    ListeningTaskId =
                        listeningLink.ListeningTaskId,

                    ListeningCourseType =
                        listeningLink.ListeningTask.CourseType,

                    ListeningPartTitle =
                        listeningLink.ListeningTask.PartTitle,

                    ListeningDirection =
                        listeningLink.ListeningTask.Direction,

                    ListeningLittlePart =
                        listeningLink.ListeningTask.LittlePart,

                    Content = question.Content,

                    QuestionType =
                        QuestionType.SingleChoice,

                    OrderIndex = globalOrderIndex,

                    Point = question.Point,

                    IsListeningQuestion = true,

                    AudioUrl =
                        listeningLink.ListeningTask.AudioUrl,

                    ListeningPromptImageUrl =
                        question.PromptImageUrl,

                    Transcript =
                        listeningLink.ListeningTask.Transcript,

                    Options = question.Options
                        .OrderBy(x => x.OrderIndex)
                        .Select(option =>
                            new StudentTestQuestionOptionDto
                            {
                                QuestionOptionId = 0,

                                ListeningOptionId =
                                    option.ListeningOptionId,

                                Content = option.Content,

                                OrderIndex =
                                    option.OrderIndex
                            })
                        .ToList()
                });
            }
        }
    }
    private void BuildReadingQuestions(
    Test test,
    List<StudentTestQuestionDto> questions,
    ref int globalOrderIndex)
    {
        foreach (var readingLink in test.ReadingTasks
            .OrderBy(x => x.OrderIndex))
        {
            foreach (var question in readingLink
                .ReadingTask
                .Questions
                .OrderBy(x => x.OrderIndex))
            {
                globalOrderIndex++;

                questions.Add(new StudentTestQuestionDto
                {
                    QuestionId = 0,

                    ReadingQuestionId =
                        question.ReadingQuestionId,

                    ReadingTaskId =
                        readingLink.ReadingTaskId,

                    Content = question.Content,

                    QuestionType =
                        QuestionType.SingleChoice,

                    OrderIndex = globalOrderIndex,

                    Point = question.Point,

                    IsReadingQuestion = true,

                    ReadingPassageTitle =
                        readingLink.ReadingTask.Title,

                    ReadingPassageText =
                        readingLink.ReadingTask.PassageText,

                    ReadingInstruction =
                        readingLink.ReadingTask.Instruction,

                    ReadingPassageImages =
                        readingLink.ReadingTask.PassageImages
                            .OrderBy(x => x.OrderIndex)
                            .Select(image =>
                                new ReadingPassageImageDto
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
                            new StudentTestQuestionOptionDto
                            {
                                QuestionOptionId = 0,

                                ReadingOptionId =
                                    option.ReadingOptionId,

                                Content = option.Content,

                                OrderIndex =
                                    option.OrderIndex
                            })
                        .ToList()
                });
            }
        }
    }
    private void BuildWritingQuestions(
    Test test,
    List<StudentTestQuestionDto> questions,
    ref int globalOrderIndex)
    {
        foreach (var writingLink in test.WritingTasks
            .OrderBy(x => x.OrderIndex))
        {
            globalOrderIndex++;

            questions.Add(new StudentTestQuestionDto
            {
                QuestionId = 0,

                WritingTaskId =
                    writingLink.WritingTaskId,

                Content =
                    writingLink.WritingTask.Title,

                QuestionType =
                    QuestionType.TextAnswer,

                OrderIndex = globalOrderIndex,

                Point = writingLink.Point,

                IsWritingQuestion = true,

                WritingInstruction =
                    writingLink.WritingTask.Instruction,

                WritingPromptText =
                    writingLink.WritingTask.PromptText,

                WritingPromptImageUrl =
                    writingLink.WritingTask.PromptImageUrl,

                WritingReferenceText =
                    writingLink.WritingTask.ReferenceText,

                WritingSourceText =
                    writingLink.WritingTask.SourceText,

                WritingRequirements =
                    writingLink.WritingTask.Requirements,

                WritingScoringCriteria =
                    writingLink.WritingTask.ScoringCriteria,

                WritingMinimumWords =
                    writingLink.WritingTask.MinimumWords,

                WritingTaskType =
                    writingLink.WritingTask.TaskType.ToString(),

                Options =
                    new List<StudentTestQuestionOptionDto>()
            });
        }
    }
    private void BuildSpeakingQuestions(
    Test test,
    List<StudentTestQuestionDto> questions,
    ref int globalOrderIndex)
    {
        foreach (var speakingLink in test.SpeakingTasks
            .OrderBy(x => x.OrderIndex))
        {
            globalOrderIndex++;

            questions.Add(new StudentTestQuestionDto
            {
                QuestionId = 0,

                SpeakingTaskId =
                    speakingLink.SpeakingTaskId,

                Content =
                    speakingLink.SpeakingTask.Title,

                QuestionType =
                    QuestionType.AudioAnswer,

                OrderIndex = globalOrderIndex,

                Point = speakingLink.Point,

                IsSpeakingQuestion = true,

                SpeakingPdfUrl =
                    speakingLink.SpeakingTask.PdfUrl,

                SpeakingInstruction =
                    speakingLink.SpeakingTask.Instruction,

                SpeakingPromptText =
                    speakingLink.SpeakingTask.PromptText,

                SpeakingPromptImageUrl =
                    speakingLink.SpeakingTask.PromptImageUrl,

                SpeakingReferenceText =
                    speakingLink.SpeakingTask.ReferenceText,

                PreparationTimeSeconds =
                    speakingLink.SpeakingTask
                        .PreparationTimeSeconds,

                SpeakingTimeSeconds =
                    speakingLink.SpeakingTask
                        .SpeakingTimeSeconds,

                Options =
                    new List<StudentTestQuestionOptionDto>()
            });
        }
    }

}
