using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Reading;

namespace EnglishCenterSystem.Domain.Entities.Tests;

public class TestReadingTask : BaseEntity
{
    public int TestReadingTaskId { get; set; }

    public int TestId { get; set; }

    public int ReadingTaskId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }

    public Test Test { get; set; } = null!;

    public ReadingTask ReadingTask { get; set; } = null!;
}