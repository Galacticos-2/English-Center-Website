using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Speaking;

namespace EnglishCenterSystem.Domain.Entities.Competitions;

public class CompetitionTestSpeakingTask : BaseEntity
{
    public int CompetitionTestSpeakingTaskId { get; set; }

    public int CompetitionTestId { get; set; }

    public int SpeakingTaskId { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }

    public CompetitionTest CompetitionTest { get; set; } = null!;

    public SpeakingTask SpeakingTask { get; set; } = null!;
}