using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application
.Features.Courses.DTOs;

public class AssignTeacherDto
{
    public int UserId { get; set; }

    public List<int> ClassRoomIds
    { get; set; }
        = new();
}