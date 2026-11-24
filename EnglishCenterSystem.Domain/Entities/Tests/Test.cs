using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Tests;

public class Test : BaseEntity
{
    public int TestId { get; set; }

    public int TeacherUserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public TestType TestType { get; set; }

    public CourseType CourseType { get; set; }

    public string? SourceFileUrl { get; set; }

    public string? SourceFileObjectName { get; set; }

    public double TotalPoints { get; set; } = 10;

    public bool IsActive { get; set; } = true;

    public User TeacherUser { get; set; } = null!;

    public ICollection<TestAssignment> Assignments { get; set; } =
        new List<TestAssignment>();

    public ICollection<TestListeningTask> ListeningTasks { get; set; } =
        new List<TestListeningTask>();

    public ICollection<TestReadingTask> ReadingTasks { get; set; } =
        new List<TestReadingTask>();

    public ICollection<TestWritingTask> WritingTasks { get; set; } =
        new List<TestWritingTask>();

    public ICollection<TestSpeakingTask> SpeakingTasks { get; set; } =
        new List<TestSpeakingTask>();
}