namespace EnglishCenterSystem.Application
    .Features.TeacherStudents.DTOs;

public class TeacherStudentDashboardDto
{
    public List<TeacherStudentCourseFilterDto>
        Courses
    { get; set; } = [];

    public List<TeacherStudentClassFilterDto>
        Classes
    { get; set; } = [];

    public TeacherStudentDashboardSummaryDto
        Summary
    { get; set; } = new();

    public List<TeacherStudentDashboardItemDto>
        Students
    { get; set; } = [];
}

public class TeacherStudentCourseFilterDto
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = "";
}

public class TeacherStudentClassFilterDto
{
    public int ClassRoomId { get; set; }

    public string ClassName { get; set; } = "";

    public int CourseId { get; set; }

    public string CourseName { get; set; } = "";
}

public class TeacherStudentDashboardSummaryDto
{
    public string SelectedClassName { get; set; } =
        "Tất cả lớp";

    public string SelectedCourseName { get; set; } =
        "Tất cả khóa học";

    public int TotalStudents { get; set; }

    public double AverageLessonProgressPercent
    {
        get;
        set;
    }

    public double AverageExerciseCompletionPercent
    {
        get;
        set;
    }

    public double AverageTestScore { get; set; }
}

public class TeacherStudentDashboardItemDto
{
    public int StudentRoleId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public string? AvatarUrl { get; set; }

    public int ClassRoomId { get; set; }

    public string ClassRoomName { get; set; } = "";

    public int CourseId { get; set; }

    public string CourseName { get; set; } = "";

    public int CompletedLessons { get; set; }

    public int TotalLessons { get; set; }

    public double LessonProgressPercent
    {
        get;
        set;
    }

    public int CompletedExercises { get; set; }

    public int TotalAssignedExercises { get; set; }

    public double ExerciseCompletionPercent
    {
        get;
        set;
    }

    public int CompletedTests { get; set; }

    public int TotalAssignedTests { get; set; }

    public double? AverageTestScore { get; set; }
}