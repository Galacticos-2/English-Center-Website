using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Tests;

public class TestAssignment : BaseEntity
{
    public int TestAssignmentId { get; set; }

    public int TestId { get; set; }

    public int ClassRoomId { get; set; }

    public int MaxAttempts { get; set; } = 1;

    public int DurationMinutes { get; set; } = 120;

    public DateTime? DeadlineAt { get; set; }

    public bool IsActive { get; set; } = true;

    public Test Test { get; set; } = null!;

    public ClassRoom ClassRoom { get; set; } = null!;
}
