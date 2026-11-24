using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Writing;

namespace EnglishCenterSystem.Domain.Entities.Tests;

public class TestWritingTask : BaseEntity
{
    public int TestWritingTaskId { get; set; }

    public int TestId { get; set; }

    public int WritingTaskId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }

    public Test Test { get; set; } = null!;

    public WritingTask WritingTask { get; set; } = null!;
}