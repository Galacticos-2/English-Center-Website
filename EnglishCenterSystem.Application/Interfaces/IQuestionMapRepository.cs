using EnglishCenterSystem.Domain.Entities.Exercises;
namespace EnglishCenterSystem.Application.Interfaces;
public interface IQuestionMapRepository
{
    IQueryable<QuestionMap> Query();
    Task<QuestionMap?> GetByIdAsync(int questionMapId);
    Task AddAsync(QuestionMap map);
    Task AddProgressAsync(StudentQuestionGroupProgress progress);
}
