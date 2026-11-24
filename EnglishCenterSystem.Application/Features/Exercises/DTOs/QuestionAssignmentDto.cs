namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class QuestionAssignmentDto
{
    public int QuestionAssignmentId { get; set; }

    public int QuestionId { get; set; }

    public int ClassRoomId { get; set; }

    public string ClassRoomName { get; set; } = null!;

    public bool IsActive { get; set; }
    public int GroupNumber { get; set; }
}
