using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Lessons.DTOs;

public class CreateLessonSectionDto
{
    public string Title { get; set; } = null!;

    public int CourseId { get; set; }
    public int ClassRoomId { get; set; }
    public int LessonCategoryId { get; set; }
}