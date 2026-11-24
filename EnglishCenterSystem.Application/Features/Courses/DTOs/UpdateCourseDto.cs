using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application
.Features.Courses.DTOs;

public class UpdateCourseDto
{
    public string Name { get; set; }
        = null!;

    public string Description { get; set; }
        = null!;

    public string CourseType { get; set; }
        = null!;
    public List<UpdateClassRoomDto> Classes { get; set; }
       = new();
}

public class UpdateClassRoomDto
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