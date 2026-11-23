using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class UpdateQuestionDto
{
    public int QuestionCategoryId { get; set; }
    public string? CorrectTextAnswer { get; set; }
    public string Content { get; set; } = null!;

    public QuestionType QuestionType { get; set; }

    public string? Explanation { get; set; }

    public string? Passage { get; set; }

    public string? AudioUrl { get; set; }

    public List<string> AcceptedAnswers { get; set; } = new();

    public bool IsActive { get; set; } = true;
   

    public CourseType CourseType { get; set; }

    public SkillType Skill { get; set; }

 

    public List<CreateQuestionOptionDto> Options { get; set; } = new();
}
