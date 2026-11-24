using EnglishCenterSystem.Domain.Entities.Competitions;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ICompetitionTestAssignmentRepository
{
    IQueryable<CompetitionTestAssignment> Query();

    IQueryable<CompetitionTestAssignment> QueryBasic();

    Task<CompetitionTestAssignment?> GetByIdAsync(
        int competitionTestAssignmentId);

    Task<CompetitionTestAssignment?> GetDetailAsync(
        int competitionTestAssignmentId);
    Task HardDeleteCompetitionTestAsync(
     int competitionTestId,
     int teacherUserId);

    Task ReplaceCompetitionTestAsync(
        int oldCompetitionTestId,
        int newCompetitionTestId,
        int teacherUserId);
    Task AddAsync(
        CompetitionTestAssignment assignment);
    Task<CompetitionTestAssignment?> GetForStudentStartAsync(
    int competitionTestAssignmentId);
    void Remove(
        CompetitionTestAssignment assignment);
}