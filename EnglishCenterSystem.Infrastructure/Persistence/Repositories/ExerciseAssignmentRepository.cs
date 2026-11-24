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

public class ExerciseAssignmentRepository
    : IExerciseAssignmentRepository
{
    private readonly AppDbContext _context;

    public ExerciseAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<ExerciseAssignment> Query()
    {
        return _context.ExerciseAssignments
            .Include(x => x.Exercise)
                .ThenInclude(x => x.ExerciseQuestions)
                    .ThenInclude(x => x.Question)
                        .ThenInclude(x => x.Options)
            .Include(x => x.Exercise)
                .ThenInclude(x => x.QuestionGroups)
                    .ThenInclude(x => x.ExerciseQuestions)
                        .ThenInclude(x => x.Question)
                            .ThenInclude(x => x.Options)
            .Include(x => x.Exercise)
                .ThenInclude(x => x.ListeningTasks)
                    .ThenInclude(x => x.ListeningTask)
                        .ThenInclude(x => x.Questions)
                            .ThenInclude(x => x.Options)
            .Include(x => x.Exercise)
                .ThenInclude(x => x.ReadingTasks)
                    .ThenInclude(x => x.ReadingTask)
                        .ThenInclude(x => x.Questions)
                            .ThenInclude(x => x.Options)
            .Include(x => x.Exercise)
                .ThenInclude(x => x.SpeakingTasks)
                    .ThenInclude(x => x.SpeakingTask)
            .Include(x => x.Exercise)
                .ThenInclude(x => x.WritingTasks)
                    .ThenInclude(x => x.WritingTask)
            .Include(x => x.ClassRoom);
    }

    public async Task<ExerciseAssignment?> GetByIdAsync(
    int exerciseAssignmentId)
    {
        return await _context.ExerciseAssignments
            .Include(x => x.Exercise)
                .ThenInclude(x => x.ExerciseQuestions)
                    .ThenInclude(x => x.Question)
                        .ThenInclude(x => x.Options)
            .Include(x => x.Exercise)
                .ThenInclude(x => x.QuestionGroups)
                    .ThenInclude(x => x.ExerciseQuestions)
                        .ThenInclude(x => x.Question)
                            .ThenInclude(x => x.Options)
            .Include(x => x.Exercise)
                .ThenInclude(x => x.ListeningTasks)
                    .ThenInclude(x => x.ListeningTask)
                        .ThenInclude(x => x.Questions)
                            .ThenInclude(x => x.Options)
            .Include(x => x.Exercise)
                .ThenInclude(x => x.ReadingTasks)
                    .ThenInclude(x => x.ReadingTask)
                        .ThenInclude(x => x.Questions)
                            .ThenInclude(x => x.Options)
            .Include(x => x.Exercise)
                .ThenInclude(x => x.SpeakingTasks)
                    .ThenInclude(x => x.SpeakingTask)
            .Include(x => x.Exercise)
                .ThenInclude(x => x.WritingTasks)
                    .ThenInclude(x => x.WritingTask)
            .Include(x => x.ClassRoom)
            .FirstOrDefaultAsync(x =>
                x.ExerciseAssignmentId == exerciseAssignmentId);
    }

    public async Task AddAsync(ExerciseAssignment assignment)
    {
        await _context.ExerciseAssignments.AddAsync(assignment);
    }

    public void Remove(ExerciseAssignment assignment)
    {
        _context.ExerciseAssignments.Remove(assignment);
    }
}
