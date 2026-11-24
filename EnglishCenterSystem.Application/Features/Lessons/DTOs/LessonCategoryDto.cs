using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Lessons.DTOs;

public class LessonCategoryDto
{
    public int LessonCategoryId { get; set; }

    public string Name { get; set; } = null!;
}