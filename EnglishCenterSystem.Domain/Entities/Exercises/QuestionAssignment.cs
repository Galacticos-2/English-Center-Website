using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class QuestionAssignment : BaseEntity
{
    public int QuestionAssignmentId { get; set; }

    public int QuestionId { get; set; }

    public int ClassRoomId { get; set; }
    public int GroupNumber { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public Question Question { get; set; } = null!;

    public ClassRoom ClassRoom { get; set; } = null!;
}
