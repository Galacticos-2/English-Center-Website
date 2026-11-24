using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Gamification;

public class StudentAchievement : BaseEntity
{
    public int StudentAchievementId { get; set; }
    public int StudentRoleId { get; set; }
    public StudentAchievementType Type { get; set; }
    public DateTime UnlockedAt { get; set; }
    public StudentRole StudentRole { get; set; } = null!;
}
