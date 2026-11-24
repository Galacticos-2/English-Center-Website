using System.ComponentModel.DataAnnotations;

namespace EnglishCenterSystem.Application.Features.OnlineClassrooms.DTOs;

public class CreateOnlineClassroomDto
{
    [Range(1, int.MaxValue)]
    public int ClassRoomId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = null!;

    [Required, Url, StringLength(500)]
    public string GoogleMeetUrl { get; set; } = null!;

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
}

public class UpdateOnlineClassroomDto : CreateOnlineClassroomDto
{
}

public class OnlineClassroomDto
{
    public int OnlineClassroomId { get; set; }
    public int ClassRoomId { get; set; }
    public string ClassName { get; set; } = null!;
    public string? CourseName { get; set; }
    public string TeacherName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string GoogleMeetUrl { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
}
