namespace EnglishCenterSystem.Application
    .Features.TeacherDashboard.DTOs;

public class TeacherDashboardDto
{
    public TeacherDashboardSummaryDto Summary { get; set; }
        = new();

    public List<TeacherDashboardActivityDto> Activity { get; set; }
        = new();

    public List<TeacherDashboardClassActivityDto> ClassActivities
    { get; set; } = new();
}

public class TeacherDashboardSummaryDto
{
    public int TotalLessons { get; set; }

    public int TotalExercises { get; set; }

    public int TotalTests { get; set; }

    public int TotalStudents { get; set; }
}

public class TeacherDashboardActivityDto
{
    public DateTime Date { get; set; }

    public string DateLabel { get; set; } = "";

    public int ExerciseAttempts { get; set; }

    public int TestAttempts { get; set; }
}

public class TeacherDashboardClassActivityDto
{
    public int ClassRoomId { get; set; }

    public string ClassName { get; set; } = "";

    public List<TeacherDashboardClassActivityPointDto> Data
    { get; set; } = new();
}

public class TeacherDashboardClassActivityPointDto
{
    public DateTime Date { get; set; }

    public string DateLabel { get; set; } = "";

    public int ExerciseAttempts { get; set; }

    public int TestAttempts { get; set; }

    public int TotalAttempts { get; set; }
}