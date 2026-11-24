namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Teacher;

public class CreateCompetitionAssignmentDto
{
    public int CompetitionTestId { get; set; }

    public int ClassRoomId { get; set; }

    public string Title { get; set; } = "";

    public string? Description { get; set; }

    /// <summary>
    /// Frontend nên gửi ISO-8601 có offset.
    ///
    /// Ví dụ giờ Việt Nam:
    /// 2026-07-20T08:00:00+07:00
    /// </summary>
    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset DeadlineAt { get; set; }

    public int DurationMinutes { get; set; }
}