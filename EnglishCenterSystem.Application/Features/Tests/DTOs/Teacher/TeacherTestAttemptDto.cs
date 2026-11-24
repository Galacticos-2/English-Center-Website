namespace EnglishCenterSystem.Application.Features.Tests.DTOs.Teacher;

public class TeacherTestAttemptDto
{
    public int TestAttemptId { get; set; }

    public int StudentRoleId { get; set; }

    public string StudentName { get; set; } = "";

    public string ClassRoomName { get; set; } = "";

    public double Score { get; set; }

    public int CorrectAnswers { get; set; }

    public int TotalQuestions { get; set; }

    public string Status { get; set; } = "";

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }
}