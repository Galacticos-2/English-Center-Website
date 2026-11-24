using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class CreateQuestionCategoryDto
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}