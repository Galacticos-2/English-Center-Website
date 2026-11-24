using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class UploadPublishAssignExerciseDraftDto
{
    public CourseType CourseType { get; set; }

    public SkillType Skill { get; set; }

    

    public int ExerciseSectionId { get; set; }

    public int ClassRoomId { get; set; }
    public string Title { get; set; } = null!;

  

    public int MaxAttempts { get; set; } = 1;
}