using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application
.Features.Users.DTOs;

public class TeacherClassDto
{
    public int ClassRoomId { get; set; }

    public string ClassName { get; set; }
        = null!;

    public string CourseName { get; set; }
        = null!;
}
