using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Entities.Reading;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Domain.Entities.Writing;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using EnglishCenterSystem.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
namespace EnglishCenterSystem.Infrastructure.Persistence.UnitOfWorks;

public class UnitOfWork : IUnitOfWork
{

    private readonly AppDbContext _context;
    private readonly ICourseRepository
    _courseRepository;
    private readonly IExerciseAttemptRepository
    _exerciseAttemptRepository;
    private readonly
IStudentRoleRepository
_studentRoleRepository;
    private readonly ICompetitionTestRepository
    _competitionTestRepository;

    private readonly ICompetitionTestAssignmentRepository
        _competitionTestAssignmentRepository;

    private readonly ICompetitionTestAttemptRepository
        _competitionTestAttemptRepository;
    private readonly INotificationRepository
    _notificationRepository;

    private readonly ICompetitionResultRepository
        _competitionResultRepository;
    private readonly IWritingScoreRepository _writingScoreRepository;
    private readonly IExerciseRepository _exerciseRepository;
    private readonly IClassRoomRepository
        _classRoomRepository;
    private readonly IReadingTaskRepository _readingTaskRepository;
    private readonly IExerciseAssignmentRepository
    _exerciseAssignmentRepository;
    private readonly ITestRepository _testRepository;
    private readonly ITestAssignmentRepository _testAssignmentRepository;
    private readonly ITestAttemptRepository _testAttemptRepository;
    private readonly IWritingSubmissionRepository _writingSubmissionRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionAssignmentRepository _questionAssignmentRepository;
    private readonly IQuestionMapRepository _questionMapRepository;
    private readonly IWritingTaskRepository _writingTaskRepository;
    private readonly ISpeakingTaskRepository _speakingTaskRepository;
    private readonly IListeningTaskRepository _listeningTaskRepository;
    public IUserRepository Users
    {
        get;
    }
    public ITestRepository Tests => _testRepository;

    public ITestAssignmentRepository TestAssignments => _testAssignmentRepository;

    public ITestAttemptRepository TestAttempts => _testAttemptRepository;
    public void RemoveExerciseSection(ExerciseSection section)
    {
        _context.ExerciseSections.Remove(section);
    }
  
    public IQueryable<ExerciseCategory> ExerciseCategories
    => _context.ExerciseCategories;
    public IReadingTaskRepository ReadingTasks
    => _readingTaskRepository;
    public ICompetitionTestRepository CompetitionTests
    => _competitionTestRepository;
    public INotificationRepository Notifications
    => _notificationRepository;

    public ICompetitionTestAssignmentRepository
        CompetitionTestAssignments
        => _competitionTestAssignmentRepository;

    public ICompetitionTestAttemptRepository
        CompetitionTestAttempts
        => _competitionTestAttemptRepository;

    public ICompetitionResultRepository CompetitionResults
        => _competitionResultRepository;
    public IListeningTaskRepository ListeningTasks
    => _listeningTaskRepository;

    public ISpeakingTaskRepository SpeakingTasks
    => _speakingTaskRepository;
    public IWritingScoreRepository WritingScores
    => _writingScoreRepository;
    public IWritingSubmissionRepository WritingSubmissions
    => _writingSubmissionRepository;
    public IQueryable<ExerciseSection> ExerciseSections
        => _context.ExerciseSections
            .Include(x => x.Course)
            .Include(x => x.ClassRoom)
            .Include(x => x.Category);
    public ITeacherRoleRepository
TeacherRoleRepository
    => _teacherRoleRepository;
    public ICourseRepository Courses
    => _courseRepository;
    public IExerciseAttemptRepository ExerciseAttempts
    => _exerciseAttemptRepository;
    public ILessonRepository Lessons => _lessonRepository;
    public IQueryable<TeacherRole>
TeacherRoles
    => _context.TeacherRoles;
    public IClassRoomRepository ClassRooms
        => _classRoomRepository;
    public IStudentRoleRepository
StudentRoleRepository
    => _studentRoleRepository;

    public IQueryable<StudentRole>
    StudentRoles
        => _context.StudentRoles;
    public IQueryable<EnglishCenterSystem.Domain.Entities.Gamification.StudentExperience>
        StudentExperiences => _context.StudentExperiences;
    public IQueryable<OnlineClassroom> OnlineClassrooms
        => _context.OnlineClassrooms
            .Include(x => x.ClassRoom)
            .ThenInclude(x => x.Course)
            .Include(x => x.TeacherUser);
    public IQueryable<VocabularyFlashcard> VocabularyFlashcards
        => _context.VocabularyFlashcards
            .Include(x => x.ClassRoom)
            .ThenInclude(x => x.Course)
            .Include(x => x.TeacherUser);
    public IQueryable<StudentFlashcardProgress> StudentFlashcardProgresses
        => _context.StudentFlashcardProgresses;
    public Task AddOnlineClassroomAsync(OnlineClassroom onlineClassroom)
        => _context.OnlineClassrooms.AddAsync(onlineClassroom).AsTask();
    public Task AddVocabularyFlashcardAsync(VocabularyFlashcard flashcard)
        => _context.VocabularyFlashcards.AddAsync(flashcard).AsTask();
    public Task AddStudentFlashcardProgressAsync(StudentFlashcardProgress progress)
        => _context.StudentFlashcardProgresses.AddAsync(progress).AsTask();
    public void RemoveStudentFlashcardProgresses(IEnumerable<StudentFlashcardProgress> progresses)
        => _context.StudentFlashcardProgresses.RemoveRange(progresses);
    public IExerciseAssignmentRepository ExerciseAssignments
    => _exerciseAssignmentRepository;
    public IQuestionRepository Questions
    => _questionRepository;
    public IQuestionAssignmentRepository QuestionAssignments
    => _questionAssignmentRepository;
    public IQuestionMapRepository QuestionMaps => _questionMapRepository;
    public IQueryable<StudentQuestionGroupProgress> StudentQuestionGroupProgresses
        => _context.StudentQuestionGroupProgresses;
    public IExerciseRepository Exercises
    => _exerciseRepository;
    public async Task AddExerciseSectionAsync(
    ExerciseSection section)
    {
        await _context.ExerciseSections.AddAsync(section);
    }
    public IWritingTaskRepository WritingTasks
    => _writingTaskRepository;
    public UnitOfWork(AppDbContext context)
    {
        _context = context;

        Users =
            new UserRepository(context);
        _teacherRoleRepository =
    new TeacherRoleRepository(
        context);
        _lessonRepository =
    new LessonRepository(context);
        _courseRepository =
            new CourseRepository(context);
        _studentRoleRepository =
    new StudentRoleRepository(context);
        _classRoomRepository =
            new ClassRoomRepository(context);
        _listeningTaskRepository =
    new ListeningTaskRepository(context);
        _exerciseRepository =
    new ExerciseRepository(context);
        _exerciseAttemptRepository =
    new ExerciseAttemptRepository(context);
        _writingScoreRepository =
    new WritingScoreRepository(context);
        _testRepository = new TestRepository(context);
        _testAssignmentRepository = new TestAssignmentRepository(context);
        _testAttemptRepository = new TestAttemptRepository(context);
        _exerciseAssignmentRepository =
    new ExerciseAssignmentRepository(context);
        _questionRepository =
    new QuestionRepository(context);
        _questionAssignmentRepository =
    new QuestionAssignmentRepository(context);
        _questionMapRepository = new QuestionMapRepository(context);
        _competitionTestRepository =
     new CompetitionTestRepository(context);
        _notificationRepository =
    new NotificationRepository(context);
        _competitionTestAssignmentRepository =
            new CompetitionTestAssignmentRepository(context);

        _competitionTestAttemptRepository =
            new CompetitionTestAttemptRepository(context);

        _competitionResultRepository =
            new CompetitionResultRepository(context);
        _writingTaskRepository =
    new WritingTaskRepository(context);
        _speakingTaskRepository =
    new SpeakingTaskRepository(context);
        _writingSubmissionRepository =
    new WritingSubmissionRepository(context);
        _readingTaskRepository =
    new ReadingTaskRepository(context);
    }
    private readonly
ITeacherRoleRepository
_teacherRoleRepository;
    public async Task<int>
        SaveChangesAsync()
    {
        return await _context
            .SaveChangesAsync();
    }
    public async Task HardDeleteStudentAsync(
    int userId)
    {
        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            /*
             * IgnoreQueryFilters rất quan trọng vì nhiều entity
             * trong hệ thống có filter IsDeleted.
             *
             * Nếu không dùng IgnoreQueryFilters, các bản ghi đã
             * soft-delete có thể bị ẩn nhưng vẫn còn khóa ngoại,
             * khiến việc hard-delete StudentRole thất bại.
             */
            var user =
                await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId);

            if (user == null)
            {
                throw new Exception(
                    "Không tìm thấy tài khoản học viên.");
            }

            if (user.Role != UserRole.Student)
            {
                throw new Exception(
                    "Tài khoản được chọn không phải học viên.");
            }

            var studentRole =
                await _context.StudentRoles
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId);

            /*
             * Có thể xảy ra trường hợp User tồn tại nhưng
             * StudentRole đã bị xóa trước đó.
             */
            if (studentRole == null)
            {
                await DeleteUserNotificationDataAsync(
                    userId);

                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return;
            }

            var studentRoleId =
                studentRole.StudentRoleId;

            /*
             * =====================================================
             * 1. LẤY ID CÁC ATTEMPT CỦA HỌC VIÊN
             * =====================================================
             */

            var testAttemptIds =
                await _context.TestAttempts
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.StudentRoleId ==
                        studentRoleId)
                    .Select(x =>
                        x.TestAttemptId)
                    .ToListAsync();

            var exerciseAttemptIds =
                await _context.ExerciseAttempts
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.StudentRoleId ==
                        studentRoleId)
                    .Select(x =>
                        x.ExerciseAttemptId)
                    .ToListAsync();

            var competitionAttemptIds =
                await _context
                    .CompetitionTestAttempts
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.StudentRoleId ==
                        studentRoleId)
                    .Select(x =>
                        x.CompetitionTestAttemptId)
                    .ToListAsync();

            /*
             * =====================================================
             * 2. XÓA DỮ LIỆU TEST
             *
             * TestAnswer
             * → TestAttempt
             * =====================================================
             */

            if (testAttemptIds.Count > 0)
            {
                var testAnswers =
                    await _context.TestAnswers
                        .IgnoreQueryFilters()
                        .Where(x =>
                            testAttemptIds.Contains(
                                x.TestAttemptId))
                        .ToListAsync();

                if (testAnswers.Count > 0)
                {
                    _context.TestAnswers
                        .RemoveRange(testAnswers);
                }

                var testAttempts =
                    await _context.TestAttempts
                        .IgnoreQueryFilters()
                        .Where(x =>
                            testAttemptIds.Contains(
                                x.TestAttemptId))
                        .ToListAsync();

                if (testAttempts.Count > 0)
                {
                    _context.TestAttempts
                        .RemoveRange(testAttempts);
                }
            }

            /*
             * =====================================================
             * 3. XÓA DỮ LIỆU BÀI TẬP
             *
             * WritingScore
             * → WritingSubmission
             * → ExerciseAnswer
             * → ExerciseAttempt
             * =====================================================
             */

            if (exerciseAttemptIds.Count > 0)
            {
                var writingSubmissionIds =
                    await _context
                        .WritingSubmissions
                        .IgnoreQueryFilters()
                        .Where(x =>
                            exerciseAttemptIds.Contains(
                                x.ExerciseAttemptId))
                        .Select(x =>
                            x.WritingSubmissionId)
                        .ToListAsync();

                if (writingSubmissionIds.Count > 0)
                {
                    var writingScores =
                        await _context.WritingScores
                            .IgnoreQueryFilters()
                            .Where(x =>
                                writingSubmissionIds
                                    .Contains(
                                        x.WritingSubmissionId))
                            .ToListAsync();

                    if (writingScores.Count > 0)
                    {
                        _context.WritingScores
                            .RemoveRange(
                                writingScores);
                    }

                    var writingSubmissions =
                        await _context
                            .WritingSubmissions
                            .IgnoreQueryFilters()
                            .Where(x =>
                                writingSubmissionIds
                                    .Contains(
                                        x.WritingSubmissionId))
                            .ToListAsync();

                    if (
                        writingSubmissions.Count >
                        0
                    )
                    {
                        _context
                            .WritingSubmissions
                            .RemoveRange(
                                writingSubmissions);
                    }
                }

                var exerciseAnswers =
                    await _context.ExerciseAnswers
                        .IgnoreQueryFilters()
                        .Where(x =>
                            exerciseAttemptIds.Contains(
                                x.ExerciseAttemptId))
                        .ToListAsync();

                if (exerciseAnswers.Count > 0)
                {
                    _context.ExerciseAnswers
                        .RemoveRange(
                            exerciseAnswers);
                }

                var exerciseAttempts =
                    await _context.ExerciseAttempts
                        .IgnoreQueryFilters()
                        .Where(x =>
                            exerciseAttemptIds.Contains(
                                x.ExerciseAttemptId))
                        .ToListAsync();

                if (exerciseAttempts.Count > 0)
                {
                    _context.ExerciseAttempts
                        .RemoveRange(
                            exerciseAttempts);
                }
            }

            /*
             * =====================================================
             * 4. XÓA DỮ LIỆU ĐỀ THI ĐẤU
             *
             * CompetitionResult phải được xóa trước Attempt vì:
             * CompetitionResult → Attempt đang dùng Restrict.
             *
             * CompetitionTestAnswer
             * → CompetitionResult
             * → CompetitionTestAttempt
             * =====================================================
             */

            if (competitionAttemptIds.Count > 0)
            {
                var competitionAnswers =
                    await _context
                        .CompetitionTestAnswers
                        .IgnoreQueryFilters()
                        .Where(x =>
                            competitionAttemptIds.Contains(
                                x.CompetitionTestAttemptId))
                        .ToListAsync();

                if (
                    competitionAnswers.Count >
                    0
                )
                {
                    _context
                        .CompetitionTestAnswers
                        .RemoveRange(
                            competitionAnswers);
                }

                var competitionResults =
                    await _context
                        .CompetitionResults
                        .IgnoreQueryFilters()
                        .Where(x =>
                            x.StudentRoleId ==
                                studentRoleId ||
                            competitionAttemptIds.Contains(
                                x.CompetitionTestAttemptId))
                        .ToListAsync();

                if (
                    competitionResults.Count >
                    0
                )
                {
                    _context
                        .CompetitionResults
                        .RemoveRange(
                            competitionResults);
                }

                var competitionAttempts =
                    await _context
                        .CompetitionTestAttempts
                        .IgnoreQueryFilters()
                        .Where(x =>
                            competitionAttemptIds.Contains(
                                x.CompetitionTestAttemptId))
                        .ToListAsync();

                if (
                    competitionAttempts.Count >
                    0
                )
                {
                    _context
                        .CompetitionTestAttempts
                        .RemoveRange(
                            competitionAttempts);
                }
            }
            else
            {
                /*
                 * Phòng trường hợp CompetitionResult còn tồn tại
                 * nhưng attempt đã bị xóa hoặc dữ liệu không đồng bộ.
                 */
                var orphanCompetitionResults =
                    await _context
                        .CompetitionResults
                        .IgnoreQueryFilters()
                        .Where(x =>
                            x.StudentRoleId ==
                            studentRoleId)
                        .ToListAsync();

                if (
                    orphanCompetitionResults
                        .Count > 0
                )
                {
                    _context
                        .CompetitionResults
                        .RemoveRange(
                            orphanCompetitionResults);
                }
            }

            /*
             * =====================================================
             * 5. XÓA TIẾN ĐỘ BÀI HỌC
             * =====================================================
             */

            var lessonProgresses =
                await _context
                    .StudentLessonProgresses
                    .Where(x =>
                        x.StudentRoleId ==
                        studentRoleId)
                    .ToListAsync();

            if (lessonProgresses.Count > 0)
            {
                _context
                    .StudentLessonProgresses
                    .RemoveRange(
                        lessonProgresses);
            }

            /*
             * =====================================================
             * 6. XÓA THÔNG BÁO LIÊN QUAN ĐẾN USER
             * =====================================================
             */

            await DeleteUserNotificationDataAsync(
                userId);

            /*
             * =====================================================
             * 7. XÓA STUDENT ROLE VÀ USER
             * =====================================================
             */

            _context.StudentRoles.Remove(
                studentRole);

            _context.Users.Remove(user);

            /*
             * Chỉ SaveChanges một lần.
             * Nếu bất kỳ câu lệnh DELETE nào thất bại,
             * transaction sẽ rollback toàn bộ.
             */
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }
    private async Task
    DeleteUserNotificationDataAsync(
        int userId)
    {
        /*
         * Xóa các bản ghi nhận thông báo của user.
         *
         * UserNotification → User đang là Restrict,
         * nên phải xóa trước User.
         */
        var userNotifications =
            await _context.UserNotifications
                .IgnoreQueryFilters()
                .Where(x =>
                    x.UserId == userId)
                .ToListAsync();

        if (userNotifications.Count > 0)
        {
            _context.UserNotifications
                .RemoveRange(
                    userNotifications);
        }

        /*
         * Thông thường Student không tạo Notification.
         * Tuy nhiên xử lý luôn để tránh FK CreatedByUserId
         * chặn việc xóa User nếu dữ liệu bất thường tồn tại.
         *
         * Khi xóa Notification, recipients của notification
         * được cascade theo cấu hình AppDbContext.
         */
        var createdNotifications =
            await _context.Notifications
                .IgnoreQueryFilters()
                .Where(x =>
                    x.CreatedByUserId ==
                    userId)
                .ToListAsync();

        if (createdNotifications.Count > 0)
        {
            _context.Notifications
                .RemoveRange(
                    createdNotifications);
        }
    }
}
