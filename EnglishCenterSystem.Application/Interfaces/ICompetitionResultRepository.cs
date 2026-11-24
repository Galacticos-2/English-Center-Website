using EnglishCenterSystem.Domain.Entities.Competitions;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ICompetitionResultRepository
{
    IQueryable<CompetitionResult> Query();

    IQueryable<CompetitionResult> QueryBasic();

    Task<CompetitionResult?> GetByIdAsync(
        int competitionResultId);

    Task AddAsync(
        CompetitionResult result);

    Task AddRangeAsync(
        IEnumerable<CompetitionResult> results);

    void RemoveRange(
        IEnumerable<CompetitionResult> results);
}