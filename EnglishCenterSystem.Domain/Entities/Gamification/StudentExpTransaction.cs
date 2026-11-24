using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Gamification;

public class StudentExpTransaction : BaseEntity
{
    public int StudentExpTransactionId { get; set; }
    public int StudentRoleId { get; set; }
    public StudentExpTransactionType Type { get; set; }
    public int ExpAmount { get; set; }
    public string ReferenceKey { get; set; } = null!;
    public DateTime AwardedAt { get; set; }
    public StudentRole StudentRole { get; set; } = null!;
}
