namespace EnglishCenterSystem.Application.Features.Users.DTOs;

public class StudentClassLeaderboardDto
{
    public string? ClassName { get; set; }
    public List<StudentClassLeaderboardItemDto> Students { get; set; } = new();
}

public class StudentClassLeaderboardItemDto
{
    public int StudentRoleId { get; set; }
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public int TotalExp { get; set; }
    public int Rank { get; set; }
    public int Level { get; set; }
    public string Title { get; set; } = null!;
    public bool IsCurrentStudent { get; set; }
}
