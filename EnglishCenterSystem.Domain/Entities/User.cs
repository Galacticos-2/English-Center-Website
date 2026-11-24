using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities;

public class User : BaseEntity
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public DateTime Birthday { get; set; }

    public UserRole Role { get; set; }
    public string FullName
    {
        get; set;
    }
= null!;
    public string? AvatarUrl { get; set; }
    public string? OriginalAvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public StudentRole? StudentRole { get; set; }

    public TeacherRole? TeacherRole { get; set; }
}
