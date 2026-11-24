using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class StudentRole : BaseEntity
{
    public int StudentRoleId { get; set; }

    public int UserId { get; set; }

    public int? ClassRoomId { get; set; }

    public int? CourseId { get; set; }

    public float? TargetScore { get; set; }

    public string? Level { get; set; }

    public User User { get; set; } = null!;

    public ClassRoom? ClassRoom { get; set; }

    public Course? Course { get; set; }
    public List<StudentLessonProgress> LessonProgresses { get; set; } = new();
}
