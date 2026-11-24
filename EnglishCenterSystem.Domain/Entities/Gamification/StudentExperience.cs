using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Gamification;

public class StudentExperience : BaseEntity
{
    public int StudentExperienceId { get; set; }
    public int StudentRoleId { get; set; }
    public int TotalExp { get; set; }
    public int CurrentStreakDays { get; set; }
    public DateOnly? LastActivityDate { get; set; }
    public StudentRole StudentRole { get; set; } = null!;
}
