public class TeacherTestAnalyticsDto
{
    public int TotalStudents { get; set; }

    public int SubmittedStudents { get; set; }

    public int PendingStudents { get; set; }

    public double AverageScore { get; set; }

    public double HighestScore { get; set; }

    public double LowestScore { get; set; }

    public double PassRate { get; set; }

    public double CompletionRate { get; set; }
}