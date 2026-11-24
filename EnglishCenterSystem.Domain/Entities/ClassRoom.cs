using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class ClassRoom : BaseEntity
{
    public int ClassRoomId { get; set; }

    public string ClassName { get; set; } = null!;

    public int CourseId { get; set; }

    public int Capacity { get; set; }

    public string Schedule { get; set; } = null!;

    public string Room { get; set; } = null!;

    public bool IsOpen { get; set; } = true;

    public Course Course { get; set; } = null!;
    public ICollection<TeacherClass>
TeacherClasses
    { get; set; }
    = new List<TeacherClass>();

    public ICollection<StudentRole> Students { get; set; }
    = new List<StudentRole>();

}
