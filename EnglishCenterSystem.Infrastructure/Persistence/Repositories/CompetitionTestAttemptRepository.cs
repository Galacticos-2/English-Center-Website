using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure
    .Persistence.Repositories;

public class CompetitionTestAttemptRepository
    : ICompetitionTestAttemptRepository
{
    private readonly AppDbContext _context;

    public CompetitionTestAttemptRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<CompetitionTestAttempt> QueryBasic()
    {
        return _context.CompetitionTestAttempts;
    }

    public IQueryable<CompetitionTestAttempt> Query()
    {
        return _context.CompetitionTestAttempts
            .AsSplitQuery()
            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
            .Include(x => x.StudentRole)
                .ThenInclude(x => x.User)
            .Include(x => x.Answers)
            .Include(x => x.Result);
    }

    public async Task<CompetitionTestAttempt?>
        GetByIdAsync(
            int competitionTestAttemptId)
    {
        return await Query()
            .FirstOrDefaultAsync(x =>
                x.CompetitionTestAttemptId ==
                competitionTestAttemptId);
    }

    public async Task<CompetitionTestAttempt?>
        GetByAssignmentAndStudentAsync(
            int competitionTestAssignmentId,
            int studentRoleId)
    {
        return await _context.CompetitionTestAttempts
            .AsSplitQuery()

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ListeningTasks)
                        .ThenInclude(x => x.ListeningTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.PassageImages)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.WritingTasks)
                        .ThenInclude(x => x.WritingTask)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.SpeakingTasks)
                        .ThenInclude(x => x.SpeakingTask)

            .Include(x => x.Answers)

            .FirstOrDefaultAsync(x =>
                x.CompetitionTestAssignmentId ==
                    competitionTestAssignmentId &&
                x.StudentRoleId ==
                    studentRoleId &&
                !x.IsDeleted);
    }

    public async Task<CompetitionTestAttempt?>
        GetForSubmitAsync(
            int competitionTestAttemptId)
    {
        return await _context.CompetitionTestAttempts
            .AsSplitQuery()

            .Include(x => x.Answers)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ListeningTasks)
                        .ThenInclude(x => x.ListeningTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.PassageImages)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.WritingTasks)
                        .ThenInclude(x => x.WritingTask)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.SpeakingTasks)
                        .ThenInclude(x => x.SpeakingTask)

            .FirstOrDefaultAsync(x =>
                x.CompetitionTestAttemptId ==
                    competitionTestAttemptId &&
                !x.IsDeleted);
    }

    public async Task<CompetitionTestAttempt?>
        GetResultAsync(
            int competitionTestAttemptId)
    {
        return await _context.CompetitionTestAttempts
            .AsNoTracking()
            .AsSplitQuery()

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ListeningTasks)
                        .ThenInclude(x => x.ListeningTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.PassageImages)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.WritingTasks)
                        .ThenInclude(x => x.WritingTask)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.SpeakingTasks)
                        .ThenInclude(x => x.SpeakingTask)

            .Include(x => x.Answers)
                .ThenInclude(x => x.ListeningQuestion)
                    .ThenInclude(x => x.Options)

            .Include(x => x.Answers)
                .ThenInclude(x => x.ReadingQuestion)
                    .ThenInclude(x => x.Options)

            .Include(x => x.Answers)
                .ThenInclude(x => x.SelectedListeningOption)

            .Include(x => x.Answers)
                .ThenInclude(x => x.SelectedReadingOption)

            .Include(x => x.Answers)
                .ThenInclude(x => x.WritingTask)

            .Include(x => x.Answers)
                .ThenInclude(x => x.SpeakingTask)

            .Include(x => x.Result)

            .FirstOrDefaultAsync(x =>
                x.CompetitionTestAttemptId ==
                    competitionTestAttemptId &&
                !x.IsDeleted);
    }
    public async Task<CompetitionTestAttempt?>
    GetTeacherAttemptDetailAsync(
        int teacherUserId,
        int competitionTestAttemptId)
    {
        return await _context.CompetitionTestAttempts
            .AsNoTracking()
            .AsSplitQuery()

            .Include(x => x.StudentRole)
                .ThenInclude(x => x.User)

            .Include(x => x.StudentRole)
                .ThenInclude(x => x.ClassRoom)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.ClassRoom)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ListeningTasks)
                        .ThenInclude(x => x.ListeningTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.Questions)
                                .ThenInclude(x => x.Options)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.ReadingTasks)
                        .ThenInclude(x => x.ReadingTask)
                            .ThenInclude(x => x.PassageImages)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.WritingTasks)
                        .ThenInclude(x => x.WritingTask)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.SpeakingTasks)
                        .ThenInclude(x => x.SpeakingTask)

            .Include(x => x.Answers)
                .ThenInclude(x => x.ListeningQuestion)
                    .ThenInclude(x => x.Options)

            .Include(x => x.Answers)
                .ThenInclude(x => x.ReadingQuestion)
                    .ThenInclude(x => x.Options)

            .Include(x => x.Answers)
                .ThenInclude(x => x.SelectedListeningOption)

            .Include(x => x.Answers)
                .ThenInclude(x => x.SelectedReadingOption)

            .Include(x => x.Answers)
                .ThenInclude(x => x.WritingTask)

            .Include(x => x.Answers)
                .ThenInclude(x => x.SpeakingTask)

            .Include(x => x.Result)

            .FirstOrDefaultAsync(x =>
                x.CompetitionTestAttemptId ==
                    competitionTestAttemptId &&
                x.Assignment.TeacherUserId ==
                    teacherUserId &&
                !x.IsDeleted);
    }
    public async Task<CompetitionTestAttempt?>
    GetForAiGradingAsync(
        int competitionTestAttemptId)
    {
        return await _context.CompetitionTestAttempts
            .AsSplitQuery()

            .Include(x => x.Answers)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.WritingTasks)
                        .ThenInclude(x => x.WritingTask)

            .Include(x => x.Assignment)
                .ThenInclude(x => x.CompetitionTest)
                    .ThenInclude(x => x.SpeakingTasks)
                        .ThenInclude(x => x.SpeakingTask)

            .FirstOrDefaultAsync(x =>
                x.CompetitionTestAttemptId ==
                    competitionTestAttemptId &&
                !x.IsDeleted);
    }
    public async Task AddAsync(
        CompetitionTestAttempt attempt)
    {
        await _context.CompetitionTestAttempts
            .AddAsync(attempt);
    }
}
