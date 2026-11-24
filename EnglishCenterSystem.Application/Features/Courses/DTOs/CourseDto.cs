using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application
.Features.Courses.DTOs;

public class CourseDto
{
    public int CourseId { get; set; }

    public string Name { get; set; }
        = null!;

    public string Description { get; set; }
        = null!;

    public string CourseType { get; set; }
        = null!;
    public List<ClassSummaryDto>
    Classes
    { get; set; }
        = new();
    public bool IsActive { get; set; }
}