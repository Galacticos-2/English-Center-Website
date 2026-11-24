using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class QuestionDto
{
    public int GroupNumber { get; set; } = 1;
    public int QuestionId { get; set; }

    public int QuestionCategoryId { get; set; }

    public string QuestionCategoryName { get; set; } = null!;

    public int TeacherUserId { get; set; }
    public string? CorrectTextAnswer { get; set; }
    public string TeacherName { get; set; } = null!;

    public string Content { get; set; } = null!;

    public QuestionType QuestionType { get; set; }

    public string? Explanation { get; set; }

    public string? Passage { get; set; }

    public string? AudioUrl { get; set; }

    public List<string> AcceptedAnswers { get; set; } = new();
  

    public CourseType CourseType { get; set; }

    public SkillType Skill { get; set; }

   
    public bool IsActive { get; set; }

    public List<QuestionOptionDto> Options { get; set; } = new();
}
