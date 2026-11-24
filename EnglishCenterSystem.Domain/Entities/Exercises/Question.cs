using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class Question : BaseEntity
{
    public int QuestionId { get; set; }
    public string? CorrectTextAnswer { get; set; }
    public int QuestionCategoryId { get; set; }

   

    public int TeacherUserId { get; set; }

    public string Content { get; set; } = null!;

    public QuestionType QuestionType { get; set; }

    public string? Explanation { get; set; }

    public string? Passage { get; set; }

    public string? AudioUrl { get; set; }

    public string? AcceptedAnswersJson { get; set; }

    public CourseType CourseType { get; set; }

    public SkillType Skill { get; set; }



 

    public bool IsActive { get; set; } = true;

    public QuestionCategory QuestionCategory { get; set; } = null!;

   

    public User TeacherUser { get; set; } = null!;
    public ICollection<ExerciseQuestion> ExerciseQuestions { get; set; }
    = new List<ExerciseQuestion>();
    public ICollection<QuestionOption> Options { get; set; }
        = new List<QuestionOption>();
    public ICollection<QuestionAssignment> Assignments { get; set; }
        = new List<QuestionAssignment>();
}
