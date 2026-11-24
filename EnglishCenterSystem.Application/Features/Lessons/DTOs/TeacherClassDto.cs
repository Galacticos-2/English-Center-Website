using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace EnglishCenterSystem.Application.Features.Lessons.DTOs;

public class TeacherClassDto
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = null!;

    public int ClassRoomId { get; set; }

    public string ClassName { get; set; } = null!;
}