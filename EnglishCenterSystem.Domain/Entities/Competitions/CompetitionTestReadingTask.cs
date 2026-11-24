using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Reading;

namespace EnglishCenterSystem.Domain.Entities.Competitions;

public class CompetitionTestReadingTask : BaseEntity
{
    public int CompetitionTestReadingTaskId { get; set; }

    public int CompetitionTestId { get; set; }

    public int ReadingTaskId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }

    public CompetitionTest CompetitionTest { get; set; } = null!;

    public ReadingTask ReadingTask { get; set; } = null!;
}