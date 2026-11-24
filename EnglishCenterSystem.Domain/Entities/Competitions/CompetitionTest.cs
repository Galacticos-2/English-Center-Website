using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Competitions;

public class CompetitionTest : BaseEntity
{
    public int CompetitionTestId { get; set; }

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

    public ICollection<CompetitionTestListeningTask>
        ListeningTasks
    { get; set; }
        = new List<CompetitionTestListeningTask>();

    public ICollection<CompetitionTestReadingTask>
        ReadingTasks
    { get; set; }
        = new List<CompetitionTestReadingTask>();

    public ICollection<CompetitionTestWritingTask>
        WritingTasks
    { get; set; }
        = new List<CompetitionTestWritingTask>();

    public ICollection<CompetitionTestSpeakingTask>
        SpeakingTasks
    { get; set; }
        = new List<CompetitionTestSpeakingTask>();

    public ICollection<CompetitionTestAssignment>
        Assignments
    { get; set; }
        = new List<CompetitionTestAssignment>();
}