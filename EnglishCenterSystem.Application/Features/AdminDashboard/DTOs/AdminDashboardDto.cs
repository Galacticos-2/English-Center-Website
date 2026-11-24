namespace EnglishCenterSystem.Application
    .Features.AdminDashboard.DTOs;

public class AdminDashboardDto
{
    public AdminDashboardSummaryDto Summary
    {
        get;
        set;
    } = new();

    public List<AdminDashboardActivityDto> Activity
    {
        get;
        set;
    } = [];

    public List<AdminDashboardCourseDto> Courses
    {
        get;
        set;
    } = [];

    public List<AdminDashboardClassDto> Classes
    {
        get;
        set;
    } = [];
}

public class AdminDashboardSummaryDto
{
    public int TotalStudents
    {
        get;
        set;
    }

    public int TotalTeachers
    {
        get;
        set;
    }

    public int TotalCourses
    {
        get;
        set;
    }

    public int TotalClasses
    {
        get;
        set;
    }
}

public class AdminDashboardActivityDto
{
    public DateTime Date
    {
        get;
        set;
    }

    public string DateLabel
    {
        get;
        set;
    } = "";

    public int LessonCompletions
    {
        get;
        set;
    }

    public int ExerciseAttempts
    {
        get;
        set;
    }

    public int TestAttempts
    {
        get;
        set;
    }
}

public class AdminDashboardCourseDto
{
    public int CourseId
    {
        get;
        set;
    }

    public string Name
    {
        get;
        set;
    } = "";

    public string Tag
    {
        get;
        set;
    } = "";

    public int ClassCount
    {
        get;
        set;
    }

    public int StudentCount
    {
        get;
        set;
    }

    public int TeacherCount
    {
        get;
        set;
    }
}

public class AdminDashboardClassDto
{
    public int ClassRoomId
    {
        get;
        set;
    }

    public string ClassName
    {
        get;
        set;
    } = "";

    public string CourseName
    {
        get;
        set;
    } = "";

    public string TeacherName
    {
        get;
        set;
    } = "Chưa phân công";

    public int StudentCount
    {
        get;
        set;
    }

    public int Capacity
    {
        get;
        set;
    }
}