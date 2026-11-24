namespace EnglishCenterSystem.Application
.Features.Courses.DTOs;

public class ClassRoomFormDto
{
    public string ClassName { get; set; } = null!;

    public int Capacity { get; set; }

    public string Schedule { get; set; } = null!;

    public string Room { get; set; } = null!;
}
