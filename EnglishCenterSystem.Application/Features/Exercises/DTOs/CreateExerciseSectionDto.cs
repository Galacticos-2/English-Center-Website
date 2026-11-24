using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class CreateExerciseSectionDto
{
    public string Title { get; set; } = null!;

    public int CourseId { get; set; }

    public int ClassRoomId { get; set; }

    public int ExerciseCategoryId { get; set; }
}
