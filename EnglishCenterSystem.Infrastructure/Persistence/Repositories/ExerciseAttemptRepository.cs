using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class ExerciseAttemptRepository : IExerciseAttemptRepository
{
    private readonly AppDbContext _context;

    public ExerciseAttemptRepository(AppDbContext context)
    {
        _context = context;
    }
    public IQueryable<ExerciseAttempt> QueryBasic()
    {
        return _context.ExerciseAttempts
            .AsNoTracking();
    }
    public IQueryable<ExerciseAttempt> Query()
    {
        return _context.ExerciseAttempts
            .Include(x => x.ExerciseAssignment)
                .ThenInclude(x => x.Exercise)
                    .ThenInclude(x => x.ExerciseQuestions)
                        .ThenInclude(x => x.Question)
                            .ThenInclude(x => x.Options)
            .Include(x => x.ExerciseAssignment)
                .ThenInclude(x => x.Exercise)
                    .ThenInclude(x => x.QuestionGroups)
                        .ThenInclude(x => x.ExerciseQuestions)
                            .ThenInclude(x => x.Question)
                                .ThenInclude(x => x.Options)
                                .Include(x => x.ExerciseAssignment)
    .ThenInclude(x => x.Exercise)
        .ThenInclude(x => x.ListeningTasks)
            .ThenInclude(x => x.ListeningTask)
                .ThenInclude(x => x.Questions)
                    .ThenInclude(x => x.Options)
            .Include(x => x.ExerciseAssignment)
    .ThenInclude(x => x.Exercise)
        .ThenInclude(x => x.SpeakingTasks)
            .ThenInclude(x => x.SpeakingTask)
            .Include(x => x.ExerciseAssignment)
    .ThenInclude(x => x.Exercise)
        .ThenInclude(x => x.WritingTasks)
            .ThenInclude(x => x.WritingTask)
            .Include(x => x.ExerciseAssignment)
    .ThenInclude(x => x.Exercise)
        .ThenInclude(x => x.ReadingTasks)
            .ThenInclude(x => x.ReadingTask)
                .ThenInclude(x => x.Questions)
                    .ThenInclude(x => x.Options)
            .Include(x => x.ExerciseAssignment)
    .ThenInclude(x => x.ClassRoom)

.Include(x => x.Answers)
    .ThenInclude(x => x.Question)
        .ThenInclude(x => x.Options)

.Include(x => x.StudentRole)
    .ThenInclude(x => x.User);
    }
    public void RemoveRange(List<ExerciseAttempt> attempts)
    {
        _context.ExerciseAttempts.RemoveRange(attempts);
    }
    public async Task<ExerciseAttempt?> GetByIdAsync(
    int exerciseAttemptId)
    {
        return await _context.ExerciseAttempts
            .Include(x => x.ExerciseAssignment)
                .ThenInclude(x => x.Exercise)
                    .ThenInclude(x => x.ExerciseQuestions)
                        .ThenInclude(x => x.Question)
                            .ThenInclude(x => x.Options)
            .Include(x => x.ExerciseAssignment)
    .ThenInclude(x => x.Exercise)
        .ThenInclude(x => x.ReadingTasks)
            .ThenInclude(x => x.ReadingTask)
                .ThenInclude(x => x.Questions)
                    .ThenInclude(x => x.Options)
            .Include(x => x.ExerciseAssignment)
                .ThenInclude(x => x.Exercise)
                    .ThenInclude(x => x.QuestionGroups)
                        .ThenInclude(x => x.ExerciseQuestions)
                            .ThenInclude(x => x.Question)
                                .ThenInclude(x => x.Options)
            .Include(x => x.ExerciseAssignment)
    .ThenInclude(x => x.Exercise)
        .ThenInclude(x => x.ListeningTasks)
            .ThenInclude(x => x.ListeningTask)
                .ThenInclude(x => x.Questions)
                    .ThenInclude(x => x.Options)
        .Include(x => x.ExerciseAssignment)
    .ThenInclude(x => x.Exercise)
        .ThenInclude(x => x.SpeakingTasks)
            .ThenInclude(x => x.SpeakingTask)
            .Include(x => x.ExerciseAssignment)
    .ThenInclude(x => x.Exercise)
        .ThenInclude(x => x.WritingTasks)
            .ThenInclude(x => x.WritingTask)
            .Include(x => x.ExerciseAssignment)
    .ThenInclude(x => x.ClassRoom)

.Include(x => x.Answers)
    .ThenInclude(x => x.Question)
        .ThenInclude(x => x.Options)

.Include(x => x.StudentRole)
    .ThenInclude(x => x.User)

.FirstOrDefaultAsync(x =>
    x.ExerciseAttemptId ==
    exerciseAttemptId);
    }

    public async Task AddAsync(ExerciseAttempt attempt)
    {
        await _context.ExerciseAttempts.AddAsync(attempt);
    }
}
