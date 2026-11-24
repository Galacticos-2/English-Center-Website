namespace EnglishCenterSystem.Application.Features.Lessons.DTOs;

public class LessonCategoryStatisticDto
{
    public int LessonCategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int SectionCount { get; set; }

    public int LessonCount { get; set; }
}