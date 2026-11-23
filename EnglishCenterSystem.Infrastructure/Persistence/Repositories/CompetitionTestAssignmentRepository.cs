using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using EnglishCenterSystem.Domain.Enums;
namespace EnglishCenterSystem.Infrastructure
    .Persistence.Repositories;

public class CompetitionTestAssignmentRepository
    : ICompetitionTestAssignmentRepository
{
    private readonly AppDbContext _context;

    public CompetitionTestAssignmentRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<CompetitionTestAssignment>
        QueryBasic()
    {
        return _context.CompetitionTestAssignments;
    }

    public IQueryable<CompetitionTestAssignment> Query()
    {
        return _context.CompetitionTestAssignments
            .AsSplitQuery()

            .Include(x => x.CompetitionTest)
                .ThenInclude(x => x.ListeningTasks)
                    .ThenInclude(x => x.ListeningTask)
                        .ThenInclude(x => x.Questions)
                            .ThenInclude(x => x.Options)

            .Include(x => x.CompetitionTest)
                .ThenInclude(x => x.ReadingTasks)
                    .ThenInclude(x => x.ReadingTask)
                        .ThenInclude(x => x.Questions)
                            .ThenInclude(x => x.Options)

            .Include(x => x.CompetitionTest)
                .ThenInclude(x => x.WritingTasks)
                    .ThenInclude(x => x.WritingTask)

            .Include(x => x.CompetitionTest)
                .ThenInclude(x => x.SpeakingTasks)
                    .ThenInclude(x => x.SpeakingTask)

            .Include(x => x.TeacherUser)

            .Include(x => x.ClassRoom)
                .ThenInclude(x => x.Course)

            .Include(x => x.Attempts)
                .ThenInclude(x => x.StudentRole)
                    .ThenInclude(x => x.User)

            .Include(x => x.Attempts)
                .ThenInclude(x => x.Answers)

            .Include(x => x.Results);
    }

    public async Task<CompetitionTestAssignment?>
        GetByIdAsync(
            int competitionTestAssignmentId)
    {
        return await Query()
            .FirstOrDefaultAsync(x =>
                x.CompetitionTestAssignmentId ==
                competitionTestAssignmentId);
    }

    public async Task<CompetitionTestAssignment?>
        GetDetailAsync(
            int competitionTestAssignmentId)
    {
        return await _context
            .CompetitionTestAssignments
            .AsSplitQuery()

            .Include(x => x.CompetitionTest)

            .Include(x => x.TeacherUser)

            .Include(x => x.ClassRoom)
                .ThenInclude(x => x.Course)

            .Include(x => x.ClassRoom)
                .ThenInclude(x => x.Students)

            .Include(x => x.Attempts)
                .ThenInclude(x => x.StudentRole)
                    .ThenInclude(x => x.User)

            .Include(x => x.Attempts)
                .ThenInclude(x => x.Answers)

            .Include(x => x.Results)

            .FirstOrDefaultAsync(x =>
                x.CompetitionTestAssignmentId ==
                competitionTestAssignmentId);
    }
    public async Task
    HardDeleteCompetitionTestAsync(
        int competitionTestId,
        int teacherUserId)
    {
        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            var competitionTest =
                await _context
                    .CompetitionTests
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x =>
                        x.CompetitionTestId ==
                            competitionTestId &&
                        x.TeacherUserId ==
                            teacherUserId);

            if (competitionTest == null)
            {
                throw new Exception(
                    "Competition test not found");
            }

            var assignmentIds =
                await _context
                    .CompetitionTestAssignments
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.CompetitionTestId ==
                            competitionTestId)
                    .Select(x =>
                        x.CompetitionTestAssignmentId)
                    .ToListAsync();

            var attemptIds =
                assignmentIds.Count == 0
                    ? new List<int>()
                    : await _context
                        .CompetitionTestAttempts
                        .IgnoreQueryFilters()
                        .Where(x =>
                            assignmentIds.Contains(
                                x.CompetitionTestAssignmentId))
                        .Select(x =>
                            x.CompetitionTestAttemptId)
                        .ToListAsync();

            /*
             * 1. CompetitionTestAnswer
             */
            if (attemptIds.Count > 0)
            {
                await _context
                    .CompetitionTestAnswers
                    .IgnoreQueryFilters()
                    .Where(x =>
                        attemptIds.Contains(
                            x.CompetitionTestAttemptId))
                    .ExecuteDeleteAsync();

                /*
                 * 2. CompetitionResult
                 */
                await _context
                    .CompetitionResults
                    .IgnoreQueryFilters()
                    .Where(x =>
                        attemptIds.Contains(
                            x.CompetitionTestAttemptId))
                    .ExecuteDeleteAsync();

                /*
                 * 3. CompetitionTestAttempt
                 */
                await _context
                    .CompetitionTestAttempts
                    .IgnoreQueryFilters()
                    .Where(x =>
                        attemptIds.Contains(
                            x.CompetitionTestAttemptId))
                    .ExecuteDeleteAsync();
            }

            /*
             * 4. Notification và UserNotification.
             *
             * Notification dùng RelatedEntityId là
             * CompetitionTestAssignmentId.
             */
            if (assignmentIds.Count > 0)
            {
                var notificationIds =
                    await _context
                        .Notifications
                        .IgnoreQueryFilters()
                        .Where(x =>
                            x.Type ==
                                NotificationType
                                    .CompetitionAssigned &&
                            x.RelatedEntityId != null &&
                            assignmentIds.Contains(
                                x.RelatedEntityId.Value))
                        .Select(x =>
                            x.NotificationId)
                        .ToListAsync();

                if (notificationIds.Count > 0)
                {
                    await _context
                        .UserNotifications
                        .IgnoreQueryFilters()
                        .Where(x =>
                            notificationIds.Contains(
                                x.NotificationId))
                        .ExecuteDeleteAsync();

                    await _context
                        .Notifications
                        .IgnoreQueryFilters()
                        .Where(x =>
                            notificationIds.Contains(
                                x.NotificationId))
                        .ExecuteDeleteAsync();
                }

                /*
                 * 5. CompetitionTestAssignment
                 */
                await _context
                    .CompetitionTestAssignments
                    .IgnoreQueryFilters()
                    .Where(x =>
                        assignmentIds.Contains(
                            x.CompetitionTestAssignmentId))
                    .ExecuteDeleteAsync();
            }

            /*
             * 6. Các bảng liên kết task của CompetitionTest
             */
            await _context
                .CompetitionTestListeningTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        competitionTestId)
                .ExecuteDeleteAsync();

            await _context
                .CompetitionTestReadingTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        competitionTestId)
                .ExecuteDeleteAsync();

            await _context
                .CompetitionTestWritingTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        competitionTestId)
                .ExecuteDeleteAsync();

            await _context
                .CompetitionTestSpeakingTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        competitionTestId)
                .ExecuteDeleteAsync();

            /*
             * 7. CompetitionTest
             */
            await _context
                .CompetitionTests
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        competitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId)
                .ExecuteDeleteAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }
    public async Task ReplaceCompetitionTestAsync(
    int oldCompetitionTestId,
    int newCompetitionTestId,
    int teacherUserId)
    {
        if (oldCompetitionTestId ==
            newCompetitionTestId)
        {
            throw new Exception(
                "Old and new competition tests cannot be the same");
        }

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            var oldTest =
                await _context
                    .CompetitionTests
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x =>
                        x.CompetitionTestId ==
                            oldCompetitionTestId &&
                        x.TeacherUserId ==
                            teacherUserId);

            if (oldTest == null)
            {
                throw new Exception(
                    "Old competition test not found");
            }

            var newTest =
                await _context
                    .CompetitionTests
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x =>
                        x.CompetitionTestId ==
                            newCompetitionTestId &&
                        x.TeacherUserId ==
                            teacherUserId);

            if (newTest == null)
            {
                throw new Exception(
                    "New competition test not found");
            }

            var assignments =
                await _context
                    .CompetitionTestAssignments
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.CompetitionTestId ==
                            oldCompetitionTestId)
                    .ToListAsync();

            var assignmentIds =
                assignments
                    .Select(x =>
                        x.CompetitionTestAssignmentId)
                    .ToList();

            var attemptIds =
                assignmentIds.Count == 0
                    ? new List<int>()
                    : await _context
                        .CompetitionTestAttempts
                        .IgnoreQueryFilters()
                        .Where(x =>
                            assignmentIds.Contains(
                                x.CompetitionTestAssignmentId))
                        .Select(x =>
                            x.CompetitionTestAttemptId)
                        .ToListAsync();

            /*
             * Xóa dữ liệu làm bài cũ.
             */
            if (attemptIds.Count > 0)
            {
                await _context
                    .CompetitionTestAnswers
                    .IgnoreQueryFilters()
                    .Where(x =>
                        attemptIds.Contains(
                            x.CompetitionTestAttemptId))
                    .ExecuteDeleteAsync();

                await _context
                    .CompetitionResults
                    .IgnoreQueryFilters()
                    .Where(x =>
                        attemptIds.Contains(
                            x.CompetitionTestAttemptId))
                    .ExecuteDeleteAsync();

                await _context
                    .CompetitionTestAttempts
                    .IgnoreQueryFilters()
                    .Where(x =>
                        attemptIds.Contains(
                            x.CompetitionTestAttemptId))
                    .ExecuteDeleteAsync();
            }

            var now =
                DateTime.UtcNow;

            /*
             * Giữ assignment nhưng chuyển sang đề mới.
             * Học sinh sẽ làm lại từ đầu.
             */
            foreach (var assignment in assignments)
            {
                assignment.CompetitionTestId =
                    newCompetitionTestId;

                assignment.RankedAt =
                    null;

                assignment.Status =
                    now >= assignment.DeadlineAt
                        ? CompetitionStatus.Closed
                        : now >= assignment.StartsAt
                            ? CompetitionStatus.Open
                            : CompetitionStatus.Scheduled;

                assignment.IsActive =
                    true;

                assignment.IsDeleted =
                    false;

                assignment.UpdatedAt =
                    now;
            }

            await _context.SaveChangesAsync();

            /*
             * Xóa các link task của đề cũ.
             */
            await _context
                .CompetitionTestListeningTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        oldCompetitionTestId)
                .ExecuteDeleteAsync();

            await _context
                .CompetitionTestReadingTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        oldCompetitionTestId)
                .ExecuteDeleteAsync();

            await _context
                .CompetitionTestWritingTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        oldCompetitionTestId)
                .ExecuteDeleteAsync();

            await _context
                .CompetitionTestSpeakingTasks
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        oldCompetitionTestId)
                .ExecuteDeleteAsync();

            await _context
                .CompetitionTests
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CompetitionTestId ==
                        oldCompetitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId)
                .ExecuteDeleteAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }
    public async Task<CompetitionTestAssignment?>
    GetForStudentStartAsync(
        int competitionTestAssignmentId)
    {
        return await _context.CompetitionTestAssignments
            .AsSplitQuery()

            .Include(x => x.CompetitionTest)
                .ThenInclude(x => x.ListeningTasks
                    .OrderBy(link => link.OrderIndex))
                    .ThenInclude(x => x.ListeningTask)
                        .ThenInclude(x => x.Questions
                            .OrderBy(question => question.OrderIndex))
                            .ThenInclude(x => x.Options
                                .OrderBy(option => option.OrderIndex))

            .Include(x => x.CompetitionTest)
                .ThenInclude(x => x.ReadingTasks
                    .OrderBy(link => link.OrderIndex))
                    .ThenInclude(x => x.ReadingTask)
                        .ThenInclude(x => x.Questions
                            .OrderBy(question => question.OrderIndex))
                            .ThenInclude(x => x.Options
                                .OrderBy(option => option.OrderIndex))

            .Include(x => x.CompetitionTest)
                .ThenInclude(x => x.WritingTasks
                    .OrderBy(link => link.OrderIndex))
                    .ThenInclude(x => x.WritingTask)

            .Include(x => x.CompetitionTest)
                .ThenInclude(x => x.SpeakingTasks
                    .OrderBy(link => link.OrderIndex))
                    .ThenInclude(x => x.SpeakingTask)

            .Include(x => x.ClassRoom)

            .FirstOrDefaultAsync(x =>
                x.CompetitionTestAssignmentId ==
                    competitionTestAssignmentId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.CompetitionTest.IsActive &&
                !x.CompetitionTest.IsDeleted);
    }
    public async Task AddAsync(
        CompetitionTestAssignment assignment)
    {
        await _context.CompetitionTestAssignments
            .AddAsync(assignment);
    }

    public void Remove(
        CompetitionTestAssignment assignment)
    {
        assignment.IsDeleted = true;
        assignment.IsActive = false;
        assignment.UpdatedAt = DateTime.UtcNow;
    }
}
