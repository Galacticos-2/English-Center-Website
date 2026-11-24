using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Listening;

namespace EnglishCenterSystem.Domain.Entities.Competitions;

public class CompetitionTestListeningTask : BaseEntity
{
    public int CompetitionTestListeningTaskId { get; set; }

    public int CompetitionTestId { get; set; }

    public int ListeningTaskId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }

    public CompetitionTest CompetitionTest { get; set; } = null!;

    public ListeningTask ListeningTask { get; set; } = null!;
}