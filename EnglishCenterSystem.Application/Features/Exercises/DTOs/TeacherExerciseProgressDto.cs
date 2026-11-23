using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class TeacherExerciseProgressDto
{
    public int StudentRoleId { get; set; }

    public string StudentName { get; set; } = null!;

    public string ClassRoomName { get; set; } = null!;

    public int TotalAssignedExercises { get; set; }

    public int CompletedExercises { get; set; }

    public double CompletionPercent { get; set; }

    public double AverageScore { get; set; }
}