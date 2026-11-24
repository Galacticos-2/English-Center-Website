using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application
.Features.Users.DTOs;

public class UserDto
{
    public int UserId { get; set; }

    public string UserName { get; set; }
        = null!;

    public string Email { get; set; }
        = null!;
    public DateTime CreatedAt
    {
        get;
        set;
    }
    public string Role { get; set; }
        = null!;
    public string FullName
    {
        get; set;
    }
= null!;
    public string? PhoneNumber { get; set; }

    public DateTime? Birthday { get; set; }

    public string? AvatarUrl { get; set; }
    public string? OriginalAvatarUrl { get; set; }

    public string? Level { get; set; }

    public float? TargetScore { get; set; }

    public string? CourseType { get; set; }
    public bool IsActive { get; set; }

    public int? CourseId { get; set; }

    public string? CourseName { get; set; }

    public string? ClassName { get; set; }
    public List<TeacherClassDto>
TeacherClasses
    { get; set; }
    = new();
}
