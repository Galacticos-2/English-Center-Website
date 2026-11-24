using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Domain.Entities
{
    public class TeacherClass
    {
        public int TeacherClassId { get; set; }

        public int TeacherRoleId { get; set; }

        public TeacherRole TeacherRole { get; set; }
            = null!;

        public int ClassRoomId { get; set; }

        public ClassRoom ClassRoom { get; set; }
            = null!;

        public DateTime AssignedAt { get; set; }
            = DateTime.UtcNow;
    }
}
