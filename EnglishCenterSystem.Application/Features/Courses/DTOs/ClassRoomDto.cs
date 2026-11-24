using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application
.Features.Courses.DTOs;

public class ClassRoomDto
{
    public int ClassRoomId { get; set; }

    public string ClassName { get; set; }
        = null!;

    public int Capacity { get; set; }

    public string Schedule { get; set; }
        = null!;

    public string Room { get; set; }
        = null!;
}