using EnglishCenterSystem.Application.Features.Tests.DTOs;

namespace EnglishCenterSystem.Application.Features.Tests.DTOs.Teacher;

public class TeacherTestStudentResultDto
{
    public TeacherTestAttemptDto Attempt { get; set; } = default!;

    public TestResultDto Detail { get; set; } = default!;
}