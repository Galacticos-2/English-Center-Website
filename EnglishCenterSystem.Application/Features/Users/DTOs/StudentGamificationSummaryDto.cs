namespace EnglishCenterSystem.Application.Features.Users.DTOs;

public class StudentGamificationSummaryDto
{
    public int TotalExp { get; set; }
    public int CurrentStreakDays { get; set; }
    public List<int> UnlockedAchievementTypes { get; set; } = new();
}
