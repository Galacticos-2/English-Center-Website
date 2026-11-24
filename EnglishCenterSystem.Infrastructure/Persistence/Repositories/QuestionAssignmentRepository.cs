using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class QuestionAssignmentRepository : IQuestionAssignmentRepository
{
    private readonly AppDbContext _context;

    public QuestionAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<QuestionAssignment> Query()
        => BuildQuery(_context.QuestionAssignments);

    public IQueryable<QuestionAssignment> QueryIncludingDeleted()
        => BuildQuery(_context.QuestionAssignments.IgnoreQueryFilters());

    private static IQueryable<QuestionAssignment> BuildQuery(
        IQueryable<QuestionAssignment> query)
        => query
            .Include(x => x.Question)
            .ThenInclude(x => x.Options)
            .Include(x => x.Question)
            .ThenInclude(x => x.QuestionCategory)
            .Include(x => x.Question)
            .ThenInclude(x => x.TeacherUser)
            .Include(x => x.ClassRoom)
            .ThenInclude(x => x.Course);

    public Task<QuestionAssignment?> GetByIdAsync(int questionAssignmentId)
        => Query().FirstOrDefaultAsync(x =>
            x.QuestionAssignmentId == questionAssignmentId);

    public Task AddAsync(QuestionAssignment assignment)
        => _context.QuestionAssignments.AddAsync(assignment).AsTask();

    public void Remove(QuestionAssignment assignment)
    {
        assignment.IsDeleted = true;
        assignment.IsActive = false;
    }
}
