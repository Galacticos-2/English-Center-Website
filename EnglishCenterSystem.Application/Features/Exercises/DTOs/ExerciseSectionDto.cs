using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class ExerciseSectionDto
{
    public int ExerciseSectionId { get; set; }

    public string Title { get; set; } = null!;

    public int CourseId { get; set; }

    public string CourseName { get; set; } = null!;

    public int ClassRoomId { get; set; }

    public string ClassRoomName { get; set; } = null!;

    public int ExerciseCategoryId { get; set; }

    public string ExerciseCategoryName { get; set; } = null!;
}