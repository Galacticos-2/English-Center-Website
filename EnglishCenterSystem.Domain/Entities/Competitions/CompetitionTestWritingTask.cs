using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Writing;

namespace EnglishCenterSystem.Domain.Entities.Competitions;

public class CompetitionTestWritingTask : BaseEntity
{
    public int CompetitionTestWritingTaskId { get; set; }

    public int CompetitionTestId { get; set; }

    public int WritingTaskId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }

    public CompetitionTest CompetitionTest { get; set; } = null!;

    public WritingTask WritingTask { get; set; } = null!;
}