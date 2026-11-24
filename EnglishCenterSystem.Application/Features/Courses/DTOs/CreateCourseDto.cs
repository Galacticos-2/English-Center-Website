namespace EnglishCenterSystem.Application
.Features.Courses.DTOs;

public class CreateCourseDto
{
    public string Name { get; set; }
        = null!;

    public string Description { get; set; }
        = null!;

    public string CourseType { get; set; }
        = null!;

    public int NumberOfClasses { get; set; }

    public List<CreateClassRoomDto> Classes { get; set; }
        = new();
}

public class CreateClassRoomDto
{
    public string ClassName { get; set; }
        = null!;

    public int Capacity { get; set; }

    public string Schedule { get; set; }
        = null!;

    public string Room { get; set; }
        = null!;
}