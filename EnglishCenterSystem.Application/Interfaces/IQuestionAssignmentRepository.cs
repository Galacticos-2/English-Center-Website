using EnglishCenterSystem.Domain.Entities.Exercises;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IQuestionAssignmentRepository
{
    IQueryable<QuestionAssignment> Query();

    IQueryable<QuestionAssignment> QueryIncludingDeleted();

    Task<QuestionAssignment?> GetByIdAsync(int questionAssignmentId);

    Task AddAsync(QuestionAssignment assignment);

    void Remove(QuestionAssignment assignment);
}
