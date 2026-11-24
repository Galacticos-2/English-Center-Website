using EnglishCenterSystem.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace EnglishCenterSystem.API.Requests;

public class ImportAssignExerciseRequest
{
    public IFormFile File { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public CourseType CourseType { get; set; }

    public SkillType Skill { get; set; }

 

    public int ExerciseSectionId { get; set; }

    public int ClassRoomId { get; set; }

   

    public int MaxAttempts { get; set; } = 1;

    public DateTime? DeadlineAt { get; set; }

    public bool GenerateExplanations { get; set; }
}
