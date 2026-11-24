using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class TeacherRole : BaseEntity
{
    public int TeacherRoleId { get; set; }

    public int UserId { get; set; }
    public ICollection<TeacherClass>
TeacherClasses
    { get; set; }
    = new List<TeacherClass>();
    public User User { get; set; } = null!;
}