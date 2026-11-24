using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class StudentAttemptQuestionDto
{
    public int QuestionId { get; set; }

    public string Content { get; set; } = null!;

    public QuestionType QuestionType { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }
    public bool IsListeningQuestion { get; set; }
    public bool IsReadingQuestion { get; set; }

    public int? ReadingQuestionId { get; set; }

    public int? ReadingTaskId { get; set; }

    public int? ReadingQuestionOrderIndex { get; set; }

    public string? ReadingPassageTitle { get; set; }

    public string? ReadingPassageText { get; set; }

    public string? ReadingInstruction { get; set; }

    public int? ListeningQuestionId { get; set; }

    public int? ListeningTaskId { get; set; }

    public CourseType? ListeningCourseType { get; set; }

    public string? ListeningPartTitle { get; set; }

    public string? ListeningDirection { get; set; }

    public string? ListeningLittlePart { get; set; }

    public string? AudioUrl { get; set; }

    public string? ListeningPromptImageUrl { get; set; }

    public string? Transcript { get; set; }
    public bool IsSpeakingQuestion { get; set; }

    public int? SpeakingTaskId { get; set; }

    public string? SpeakingPdfUrl { get; set; }

    public string? SpeakingInstruction { get; set; }

    public string? SpeakingPromptText { get; set; }

    public string? SpeakingPromptImageUrl { get; set; }

    public string? SpeakingReferenceText { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public int? SpeakingTimeSeconds { get; set; }
    public string? WritingSourceText { get; set; }

    public string? WritingRequirements { get; set; }

    public string? WritingScoringCriteria { get; set; }

    public int? WritingMinimumWords { get; set; }

    public string? WritingTaskType { get; set; }
    public bool IsWritingQuestion { get; set; }

    public int? WritingTaskId { get; set; }

    public string? WritingInstruction { get; set; }

    public string? WritingPromptText { get; set; }

    public string? WritingPromptImageUrl { get; set; }

    public string? WritingReferenceText { get; set; }
    public List<StudentQuestionOptionDto> Options { get; set; }
        = new();
}
