namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class UpdateExerciseSectionDto
{
    public string Title { get; set; } = null!;

    public int CourseId { get; set; }

    public int ClassRoomId { get; set; }

    public int ExerciseCategoryId { get; set; }
}