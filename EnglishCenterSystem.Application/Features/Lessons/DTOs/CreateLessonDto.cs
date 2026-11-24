using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application
.Features.Lessons.DTOs;

public class CreateLessonDto
{
    public string Title { get; set; }
        = null!;

    public int LessonSectionId
    {
        get; set;
    }

    public int OrderIndex
    {
        get; set;
    }
}