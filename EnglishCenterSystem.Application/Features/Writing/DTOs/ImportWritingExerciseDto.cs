using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Writing.DTOs;

public class ImportWritingExerciseDto
{
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public CourseType CourseType { get; set; }

    public int ExerciseSectionId { get; set; }

    public int ClassRoomId { get; set; }

    
    public int MaxAttempts { get; set; } = 1;

    public DateTime? DeadlineAt { get; set; }
}
