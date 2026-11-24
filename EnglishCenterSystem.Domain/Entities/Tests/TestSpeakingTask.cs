using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Speaking;

namespace EnglishCenterSystem.Domain.Entities.Tests;

public class TestSpeakingTask : BaseEntity
{
    public int TestSpeakingTaskId { get; set; }

    public int TestId { get; set; }

    public int SpeakingTaskId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }

    public Test Test { get; set; } = null!;

    public SpeakingTask SpeakingTask { get; set; } = null!;
}