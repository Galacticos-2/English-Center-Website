using EnglishCenterSystem.Domain.Entities.Competitions;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ICompetitionTestRepository
{
    IQueryable<CompetitionTest> Query();

    IQueryable<CompetitionTest> QueryBasic();

    Task<CompetitionTest?> GetByIdAsync(
        int competitionTestId);

    Task AddAsync(
        CompetitionTest competitionTest);

    void Remove(
        CompetitionTest competitionTest);

    Task HardDeleteAsync(
        int competitionTestId,
        int teacherUserId);
}