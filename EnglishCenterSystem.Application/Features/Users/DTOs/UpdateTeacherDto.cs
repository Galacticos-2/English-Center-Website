namespace EnglishCenterSystem.Application
.Features.Users.DTOs;

public class UpdateTeacherDto
{
    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Password { get; set; }

    public string? ConfirmPassword { get; set; }
}
