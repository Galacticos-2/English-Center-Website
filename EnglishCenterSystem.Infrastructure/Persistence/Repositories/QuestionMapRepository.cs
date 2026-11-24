using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;
public class QuestionMapRepository : IQuestionMapRepository
{
    private readonly AppDbContext _context;
    public QuestionMapRepository(AppDbContext context) => _context = context;
    public IQueryable<QuestionMap> Query() => _context.QuestionMaps
        .Include(x => x.Groups).ThenInclude(x => x.Questions).ThenInclude(x => x.Question).ThenInclude(x => x.Options)
        .Include(x => x.Groups).ThenInclude(x => x.Assignments).ThenInclude(x => x.ClassRoom)
        .Include(x => x.Groups).ThenInclude(x => x.Questions).ThenInclude(x => x.Question).ThenInclude(x => x.QuestionCategory)
        .Include(x => x.Groups).ThenInclude(x => x.Questions).ThenInclude(x => x.Question).ThenInclude(x => x.TeacherUser);
    public Task<QuestionMap?> GetByIdAsync(int id) => Query().FirstOrDefaultAsync(x => x.QuestionMapId == id);
    public Task AddAsync(QuestionMap map) => _context.QuestionMaps.AddAsync(map).AsTask();
    public Task AddProgressAsync(StudentQuestionGroupProgress progress) => _context.StudentQuestionGroupProgresses.AddAsync(progress).AsTask();
}
