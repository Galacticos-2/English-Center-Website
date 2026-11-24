using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class OnlineClassroom : BaseEntity
{
    public int OnlineClassroomId { get; set; }
    public int ClassRoomId { get; set; }
    public int TeacherUserId { get; set; }
    public string Title { get; set; } = null!;
    public string GoogleMeetUrl { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public ClassRoom ClassRoom { get; set; } = null!;
    public User TeacherUser { get; set; } = null!;
}
