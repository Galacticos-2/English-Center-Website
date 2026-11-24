using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Speaking;

public class SpeakingTask : BaseEntity
{
    public int SpeakingTaskId { get; set; }

    public int TeacherUserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string PdfUrl { get; set; } = null!;

    public string? PdfObjectName { get; set; }

    public string? PromptText { get; set; }

    public string? PromptImageUrl { get; set; }

    public string? ReferenceText { get; set; }

    public CourseType CourseType { get; set; }



    public int? PreparationTimeSeconds { get; set; }

    public int? SpeakingTimeSeconds { get; set; }

    public bool IsActive { get; set; } = true;

    public User TeacherUser { get; set; } = null!;
}
