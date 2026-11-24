using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Listening;

namespace EnglishCenterSystem.Domain.Entities.Tests;

public class TestListeningTask : BaseEntity
{
    public int TestListeningTaskId { get; set; }

    public int TestId { get; set; }

    public int ListeningTaskId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }

    public Test Test { get; set; } = null!;

    public ListeningTask ListeningTask { get; set; } = null!;
}