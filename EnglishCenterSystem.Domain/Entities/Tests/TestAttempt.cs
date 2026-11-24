using EnglishCenterSystem.Domain.Common;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Tests;

public class TestAttempt : BaseEntity
{
    public int TestAttemptId { get; set; }

    /// <summary>
    /// Có giá trị nếu đây là lượt làm Test thông thường.
    /// Null nếu đây là lượt làm Ranking.
    /// </summary>


    public int TestAssignmentId { get; set; }

    public TestAssignment TestAssignment { get; set; } = null!;

    public int StudentRoleId { get; set; }

    public int AttemptNumber { get; set; } = 1;

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public TestAttemptStatus Status { get; set; }
        = TestAttemptStatus.InProgress;

    public double Score { get; set; }

    public double TotalPoints { get; set; } = 10;

    public int CorrectAnswers { get; set; }

    public int TotalQuestions { get; set; }




    public StudentRole StudentRole { get; set; } = null!;


    public ICollection<TestAnswer> Answers { get; set; }
        = new List<TestAnswer>();
}