using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Domain.Entities.Speaking;
namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class Exercise : BaseEntity
{
    public int ExerciseId { get; set; }

    public int TeacherUserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? SourceFileUrl { get; set; }

    public string? SourceFileObjectName { get; set; }

    public CourseType CourseType { get; set; }

    public SkillType Skill { get; set; }
    public ICollection<ExerciseListeningTask> ListeningTasks { get; set; }
    = new List<ExerciseListeningTask>();
    public ICollection<ExerciseSpeakingTask> SpeakingTasks { get; set; }
    = new List<ExerciseSpeakingTask>();
    public ICollection<ExerciseWritingTask> WritingTasks { get; set; }
    = new List<ExerciseWritingTask>();

    public int? ExerciseSectionId { get; set; }

    public ExerciseSection? ExerciseSection { get; set; }
    
    public ICollection<ExerciseAssignment> Assignments { get; set; }
    = new List<ExerciseAssignment>();
    public bool IsActive { get; set; } = true;
    public ICollection<ExerciseQuestionGroup> QuestionGroups { get; set; }
    = new List<ExerciseQuestionGroup>();
    public User TeacherUser { get; set; } = null!;
    public ICollection<ExerciseReadingTask> ReadingTasks { get; set; }
    = new List<ExerciseReadingTask>();


    public ICollection<ExerciseQuestion> ExerciseQuestions { get; set; }
        = new List<ExerciseQuestion>();
}
