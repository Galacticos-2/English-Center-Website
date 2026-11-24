using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class StudentExerciseSectionDto
{
    public int ExerciseSectionId { get; set; }

    public string Title { get; set; } = null!;

    public List<StudentExerciseDto> Exercises { get; set; }
        = new();
}