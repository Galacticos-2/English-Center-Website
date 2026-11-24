using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Lessons.DTOs;

public class LessonSectionDto
{
    public int LessonSectionId { get; set; }

    public string Title { get; set; } = null!;

    public int CourseId { get; set; }

    public int LessonCategoryId { get; set; }

    public string CategoryName { get; set; } = null!;
    public int? ClassRoomId { get; set; }
    public string? ClassName { get; set; }

    public List<LessonDto> Lessons { get; set; } = new();
}
