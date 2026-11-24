using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class ExerciseCategoryDto
{
    public int ExerciseCategoryId { get; set; }

    public string Name { get; set; } = null!;

    public int OrderIndex { get; set; }
}