using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Writing;

public class WritingTask : BaseEntity
{
    public int WritingTaskId { get; set; }

    public int TeacherUserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string? PromptText { get; set; }

    public string? PromptImageUrl { get; set; }

    public WritingTaskType TaskType { get; set; }

    public int OrderIndex { get; set; }

    public int MinimumWords { get; set; }

    public string? SourceText { get; set; }

    public string? Requirements { get; set; }

    public string? ScoringCriteria { get; set; }

    public string? ReferenceText { get; set; }

    public CourseType CourseType { get; set; }

    public bool IsActive { get; set; } = true;
}
