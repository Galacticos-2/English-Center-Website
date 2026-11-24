using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class StudentLessonProgress : BaseEntity
{
    public int StudentLessonProgressId { get; set; }

    public int StudentRoleId { get; set; }

    public int LessonId { get; set; }

    public bool IsCompleted { get; set; }

    public StudentRole StudentRole { get; set; } = null!;

    public Lesson Lesson { get; set; } = null!;
}