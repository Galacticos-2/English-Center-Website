using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class CreateExerciseDto
{
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public CourseType CourseType { get; set; }
    public List<CreateExerciseQuestionGroupDto> QuestionGroups { get; set; }
    = new();
    public SkillType Skill { get; set; }
    public int? ExerciseSectionId { get; set; }
   

  

    public List<CreateExerciseQuestionDto> Questions { get; set; }
        = new();
}