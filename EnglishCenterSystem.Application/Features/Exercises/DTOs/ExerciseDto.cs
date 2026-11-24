using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class ExerciseDto
{
    public int ExerciseId { get; set; }

    public int TeacherUserId { get; set; }

    public string TeacherName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    public bool HasSourceFile { get; set; }
    public int? ExerciseSectionId { get; set; }
    public List<ExerciseQuestionGroupDto> QuestionGroups { get; set; }
    = new();
    public string? ExerciseSectionTitle { get; set; }

    public int? ExerciseCategoryId { get; set; }

    public string? ExerciseCategoryName { get; set; }
    public CourseType CourseType { get; set; }

    public SkillType Skill { get; set; }

   

    

    public bool IsActive { get; set; }

    public int TotalQuestions { get; set; }

    public double TotalPoints { get; set; }
    public int MaxAttempts { get; set; }

    public List<ExerciseQuestionDto> Questions { get; set; }
        = new();
}
