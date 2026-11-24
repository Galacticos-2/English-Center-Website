using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Lessons.DTOs;

public class UpdateLessonDto
{
    public string Title { get; set; } = null!;

    public int OrderIndex { get; set; }
}