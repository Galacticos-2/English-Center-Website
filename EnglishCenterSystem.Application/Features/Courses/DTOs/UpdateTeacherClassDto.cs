using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Courses.DTOs;

public class UpdateTeacherClassDto
{
    public int UserId { get; set; }

    public int OldClassRoomId { get; set; }

    public int NewClassRoomId { get; set; }
}
