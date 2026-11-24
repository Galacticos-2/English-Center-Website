namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Teacher;

public class UpdateCompetitionAssignmentDto
{
    public string Title { get; set; } =
        string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset DeadlineAt { get; set; }

    public int DurationMinutes { get; set; }

    public bool GenerateExplanations { get; set; }
}
