namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class CreateQuestionAssignmentDto
{
    public int ClassRoomId { get; set; }

    public List<int> QuestionIds { get; set; } = new();
    public int GroupNumber { get; set; } = 1;
}
