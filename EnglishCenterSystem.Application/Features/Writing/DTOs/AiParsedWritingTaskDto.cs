using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Writing.DTOs;

public class AiParsedWritingTaskDto
{
    public List<AiParsedWritingTaskItemDto> Tasks { get; set; }
        = new();
}

public class AiParsedWritingTaskItemDto
{
    public string Title { get; set; } = null!;

    public string TaskType { get; set; } = null!;

    public int OrderIndex { get; set; }

    public string? Instruction { get; set; }

    public string? TopicText { get; set; }

    public string? SourceText { get; set; }

    public string? PromptText { get; set; }

    public string? Requirements { get; set; }

    public int? MinimumWords { get; set; }

    public string? ScoringCriteria { get; set; }

    public string? ReferenceText { get; set; }
}
