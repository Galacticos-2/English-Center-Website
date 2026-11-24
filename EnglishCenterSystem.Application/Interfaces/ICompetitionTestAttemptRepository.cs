using EnglishCenterSystem.Domain.Entities.Competitions;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ICompetitionTestAttemptRepository
{
    IQueryable<CompetitionTestAttempt> Query();

    IQueryable<CompetitionTestAttempt> QueryBasic();

    Task<CompetitionTestAttempt?> GetByIdAsync(
        int competitionTestAttemptId);

    Task<CompetitionTestAttempt?>
        GetByAssignmentAndStudentAsync(
            int competitionTestAssignmentId,
            int studentRoleId);
    Task<CompetitionTestAttempt?>
    GetTeacherAttemptDetailAsync(
        int teacherUserId,
        int competitionTestAttemptId);

    Task<CompetitionTestAttempt?> GetForSubmitAsync(
        int competitionTestAttemptId);

    Task<CompetitionTestAttempt?> GetForAiGradingAsync(
        int competitionTestAttemptId);

    Task<CompetitionTestAttempt?> GetResultAsync(
        int competitionTestAttemptId);

    Task AddAsync(
        CompetitionTestAttempt attempt);
}