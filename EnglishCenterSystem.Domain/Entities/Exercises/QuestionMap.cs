using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class QuestionMap : BaseEntity
{
    public int QuestionMapId { get; set; }
    public int TeacherUserId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public CourseType CourseType { get; set; }
    public ICollection<QuestionGroup> Groups { get; set; } = new List<QuestionGroup>();
}

public class QuestionGroup : BaseEntity
{
    public int QuestionGroupId { get; set; }
    public int QuestionMapId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Icon { get; set; } = "book";
    public string Color { get; set; } = "#2563eb";
    public int OrderIndex { get; set; }
    public int QuestionCount { get; set; }
    public QuestionMap QuestionMap { get; set; } = null!;
    public ICollection<QuestionGroupQuestion> Questions { get; set; } = new List<QuestionGroupQuestion>();
    public ICollection<QuestionGroupAssignment> Assignments { get; set; } = new List<QuestionGroupAssignment>();
}

public class QuestionGroupQuestion : BaseEntity
{
    public int QuestionGroupQuestionId { get; set; }
    public int QuestionGroupId { get; set; }
    public int QuestionId { get; set; }
    public int OrderIndex { get; set; }
    public QuestionGroup QuestionGroup { get; set; } = null!;
    public Question Question { get; set; } = null!;
}

public class QuestionGroupAssignment : BaseEntity
{
    public int QuestionGroupAssignmentId { get; set; }
    public int QuestionGroupId { get; set; }
    public int ClassRoomId { get; set; }
    public QuestionGroup QuestionGroup { get; set; } = null!;
    public ClassRoom ClassRoom { get; set; } = null!;
}

public class StudentQuestionGroupProgress : BaseEntity
{
    public int StudentQuestionGroupProgressId { get; set; }
    public int StudentRoleId { get; set; }
    public int QuestionGroupId { get; set; }
    public DateTime CompletedAt { get; set; }
    public StudentRole StudentRole { get; set; } = null!;
    public QuestionGroup QuestionGroup { get; set; } = null!;
}
