using EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;

namespace EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Teacher;

public class TeacherCompetitionAttemptDetailDto
{
    public int CompetitionTestAssignmentId { get; set; }

    public int CompetitionTestAttemptId { get; set; }

    public int StudentRoleId { get; set; }

    public string StudentName { get; set; } = "";

    public string ClassRoomName { get; set; } = "";

    public int? Rank { get; set; }

    public CompetitionAttemptResultDto Result { get; set; }
        = null!;
}