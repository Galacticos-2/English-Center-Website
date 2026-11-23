using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class UploadExerciseDraftDto
{
    public string OriginalFileName { get; set; } = null!;

    public string FileUrl { get; set; } = null!;

    public string ObjectName { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public CourseType CourseType { get; set; }

    public SkillType Skill { get; set; }

  

    public int? ExerciseSectionId { get; set; }
}