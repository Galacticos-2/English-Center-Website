using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class Course : BaseEntity
{
    public int CourseId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string CourseType { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public ICollection<ClassRoom> Classes
        = new List<ClassRoom>();

    public ICollection<StudentRole> Students
        = new List<StudentRole>();
}
