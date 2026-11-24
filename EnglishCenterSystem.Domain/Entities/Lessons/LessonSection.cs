using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class LessonSection : BaseEntity
{
    public int LessonSectionId { get; set; }

    public string Title { get; set; }
        = null!;

    public int CourseId { get; set; }

    public int LessonCategoryId
    {
        get; set;
    }

    public Course Course { get; set; }
        = null!;

    public LessonCategory Category
    {
        get; set;
    }
        = null!;
    public int? TeacherUserId { get; set; }

    public User? TeacherUser { get; set; }
    public int? ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }

    public ICollection<Lesson> Lessons { get; set; }
    = new List<Lesson>();
}