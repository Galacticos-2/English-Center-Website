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

public class ExerciseRepository : IExerciseRepository
{
    private readonly AppDbContext _context;

    public ExerciseRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<Exercise> Query()
    {
        return _context.Exercises
            .Include(x => x.TeacherUser)
            .Include(x => x.QuestionGroups)
    .ThenInclude(x => x.ExerciseQuestions)
        .ThenInclude(x => x.Question)
            .ThenInclude(x => x.Options)
            .Include(x => x.ExerciseSection)
    .ThenInclude(x => x.Category)
            .Include(x => x.SpeakingTasks)
    .ThenInclude(x => x.SpeakingTask)
            .Include(x => x.ListeningTasks)
    .ThenInclude(x => x.ListeningTask)
        .ThenInclude(x => x.Questions)
            .ThenInclude(x => x.Options)
            .Include(x => x.ReadingTasks)
    .ThenInclude(x => x.ReadingTask)
        .ThenInclude(x => x.Questions)
            .ThenInclude(x => x.Options)
            .Include(x => x.WritingTasks)
    .ThenInclude(x => x.WritingTask)
            .Include(x => x.ExerciseQuestions)
                .ThenInclude(x => x.Question)
                    .ThenInclude(x => x.Options);
    }
    public IQueryable<Exercise> QueryBasic()
    {
        return _context.Exercises
            .AsNoTracking();
    }

    public async Task<List<string>> GetListeningObjectNamesAsync(
        int exerciseId,
        int teacherUserId)
    {
        var exerciseExists =
            await _context.Exercises
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.ExerciseId == exerciseId &&
                    x.TeacherUserId == teacherUserId);

        if (!exerciseExists)
        {
            return new List<string>();
        }

        var listeningFiles = await _context.ExerciseListeningTasks
            .IgnoreQueryFilters()
            .Where(x =>
                x.ExerciseId == exerciseId &&
                x.ListeningTask != null)
            .Select(x => new
            {
                x.ListeningTask!.AudioObjectName,
                x.ListeningTask.SourceQuestionFileObjectName
            })
            .ToListAsync();

        var promptImageObjectNames =
            await _context.ExerciseListeningTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.ExerciseId == exerciseId &&
                    x.ListeningTask != null)
                .SelectMany(x =>
                    x.ListeningTask!.Questions)
                .Where(x =>
                    x.PromptImageObjectName != null)
                .Select(x =>
                    x.PromptImageObjectName!)
                .ToListAsync();

        return listeningFiles
            .SelectMany(x => new[]
            {
                x.AudioObjectName,
                x.SourceQuestionFileObjectName
            })
            .Concat(promptImageObjectNames)
            .Where(x =>
                !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList();
    }

    public async Task<Exercise?> GetByIdAsync(int exerciseId)
    {
        return await _context.Exercises
            .Include(x => x.ExerciseQuestions)
                .ThenInclude(x => x.Question)
                    .ThenInclude(x => x.Options)
            .Include(x => x.QuestionGroups)
    .ThenInclude(x => x.ExerciseQuestions)
        .ThenInclude(x => x.Question)
            .ThenInclude(x => x.Options)
            .Include(x => x.TeacherUser)
            .Include(x => x.ExerciseSection)
    .ThenInclude(x => x.Category)
            .Include(x => x.SpeakingTasks)
    .ThenInclude(x => x.SpeakingTask)
    .Include(x => x.ListeningTasks)
    .ThenInclude(x => x.ListeningTask)
        .ThenInclude(x => x.Questions)
            .ThenInclude(x => x.Options)
    .Include(x => x.WritingTasks)
    .ThenInclude(x => x.WritingTask)
    .Include(x => x.ReadingTasks)
    .ThenInclude(x => x.ReadingTask)
        .ThenInclude(x => x.Questions)
            .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.ExerciseId == exerciseId);
    }

    public async Task AddAsync(Exercise exercise)
    {
        await _context.Exercises.AddAsync(exercise);
    }
    public async Task HardDeleteAsync(
    int exerciseId,
    int teacherUserId)
    {
        var exerciseExists =
            await _context.Exercises
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.ExerciseId == exerciseId &&
                    x.TeacherUserId == teacherUserId);

        if (!exerciseExists)
        {
            throw new Exception(
                "Exercise not found");
        }

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            var assignmentIds =
                await _context.ExerciseAssignments
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.ExerciseId == exerciseId)
                    .Select(x =>
                        x.ExerciseAssignmentId)
                    .ToListAsync();

            var attemptIds =
                await _context.ExerciseAttempts
                    .IgnoreQueryFilters()
                    .Where(x =>
                        assignmentIds.Contains(
                            x.ExerciseAssignmentId))
                    .Select(x => x.ExerciseAttemptId)
                    .ToListAsync();

            /*
             * WritingScore phải xóa trước
             * WritingSubmission.
             */
            var writingSubmissionIds =
                await _context.WritingSubmissions
                    .IgnoreQueryFilters()
                    .Where(x =>
                        attemptIds.Contains(
                            x.ExerciseAttemptId))
                    .Select(x =>
                        x.WritingSubmissionId)
                    .ToListAsync();

            if (writingSubmissionIds.Count > 0)
            {
                await _context.WritingScores
                    .IgnoreQueryFilters()
                    .Where(x =>
                        writingSubmissionIds.Contains(
                            x.WritingSubmissionId))
                    .ExecuteDeleteAsync();
            }

            /*
             * Xóa submission trước attempt vì
             * WritingSubmission đang tham chiếu
             * ExerciseAttempt.
             */
            if (attemptIds.Count > 0)
            {
                await _context.WritingSubmissions
                    .IgnoreQueryFilters()
                    .Where(x =>
                        attemptIds.Contains(
                            x.ExerciseAttemptId))
                    .ExecuteDeleteAsync();

                await _context.ExerciseAnswers
                    .IgnoreQueryFilters()
                    .Where(x =>
                        attemptIds.Contains(
                            x.ExerciseAttemptId))
                    .ExecuteDeleteAsync();

                await _context.ExerciseAttempts
                    .IgnoreQueryFilters()
                    .Where(x =>
                        attemptIds.Contains(
                            x.ExerciseAttemptId))
                    .ExecuteDeleteAsync();
            }

            /*
             * Sau khi attempt đã bị xóa thì mới
             * xóa được assignment.
             */
            if (assignmentIds.Count > 0)
            {
                await _context.ExerciseAssignments
                    .IgnoreQueryFilters()
                    .Where(x =>
                        assignmentIds.Contains(
                            x.ExerciseAssignmentId))
                    .ExecuteDeleteAsync();
            }

            /*
             * Xóa các bảng liên kết task.
             * Không xóa task gốc ở đây vì task có
             * thể đang được Test hoặc Competition
             * sử dụng.
             */
            await _context.ExerciseListeningTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.ExerciseId == exerciseId)
                .ExecuteDeleteAsync();

            await _context.ExerciseReadingTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.ExerciseId == exerciseId)
                .ExecuteDeleteAsync();

            await _context.ExerciseSpeakingTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.ExerciseId == exerciseId)
                .ExecuteDeleteAsync();

            await _context.ExerciseWritingTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.ExerciseId == exerciseId)
                .ExecuteDeleteAsync();

            /*
             * Xóa câu hỏi nằm trong group trước.
             */
            var questionGroupIds =
                await _context.ExerciseQuestionGroups
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.ExerciseId == exerciseId)
                    .Select(x =>
                        x.ExerciseQuestionGroupId)
                    .ToListAsync();

            if (questionGroupIds.Count > 0)
            {
                await _context.ExerciseQuestions
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.ExerciseQuestionGroupId != null &&
                        questionGroupIds.Contains(
                            x.ExerciseQuestionGroupId.Value))
                    .ExecuteDeleteAsync();
            }

            /*
             * Xóa tất cả ExerciseQuestion còn lại
             * thuộc Exercise.
             */
            await _context.ExerciseQuestions
                .IgnoreQueryFilters()
                .Where(x =>
                    x.ExerciseId == exerciseId)
                .ExecuteDeleteAsync();

            await _context.ExerciseQuestionGroups
                .IgnoreQueryFilters()
                .Where(x =>
                    x.ExerciseId == exerciseId)
                .ExecuteDeleteAsync();

            /*
             * Cuối cùng mới xóa Exercise.
             */
            await _context.Exercises
                .IgnoreQueryFilters()
                .Where(x =>
                    x.ExerciseId == exerciseId &&
                    x.TeacherUserId == teacherUserId)
                .ExecuteDeleteAsync();
            _context.ChangeTracker.Clear();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public void Remove(Exercise exercise)
    {
        _context.Exercises.Remove(exercise);
    }
}
