using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application
.Features.Users.DTOs;

public class StudentProfileDto
{
    public int UserId { get; set; }

    public string UserName { get; set; }
        = null!;

    public string Email { get; set; }
        = null!;

    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? CourseName { get; set; }

    public string? CourseType { get; set; }

    public string? ClassName { get; set; }

    public bool HasClass { get; set; }

    public int TotalExp { get; set; }

    public int CurrentStreakDays { get; set; }

    public int? ClassRank { get; set; }

    public int ClassStudentCount { get; set; }

    public List<int> UnlockedAchievementTypes { get; set; } = new();
}
