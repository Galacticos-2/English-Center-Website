using System.Linq;
using EnglishCenterSystem.Application
    .Features.TeacherDashboard.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application
    .Features.TeacherDashboard.Services;

public class TeacherDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public TeacherDashboardService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TeacherDashboardDto> GetDashboardAsync(
        int teacherUserId,
        int days = 7)
    {
        if (days <= 0)
        {
            days = 7;
        }

        if (days > 31)
        {
            days = 31;
        }

        var teacher = await _unitOfWork
            .TeacherRoleRepository
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == teacherUserId);

        if (teacher == null)
        {
            throw new Exception("Teacher not found");
        }

        var classes = teacher.TeacherClasses
            .Where(x => x.ClassRoom != null)
            .Select(x => new
            {
                x.ClassRoomId,
                x.ClassRoom.ClassName
            })
            .Distinct()
            .OrderBy(x => x.ClassName)
            .ToList();

        var classRoomIds = classes
            .Select(x => x.ClassRoomId)
            .Distinct()
            .ToList();

        if (classRoomIds.Count == 0)
        {
            return BuildEmptyDashboard(days);
        }

        /*
         * Backend và database lưu UTC.
         * Mốc ngày của biểu đồ được tính theo giờ Việt Nam UTC+7.
         */
        var vietnamNow =
            DateTime.UtcNow.AddHours(7);

        var endVietnamDateExclusive =
            vietnamNow.Date.AddDays(1);

        var startVietnamDate =
            endVietnamDateExclusive.AddDays(-days);

        var startUtc =
            startVietnamDate.AddHours(-7);

        var endUtcExclusive =
            endVietnamDateExclusive.AddHours(-7);

        var dates = Enumerable
            .Range(0, days)
            .Select(index =>
                startVietnamDate.AddDays(index))
            .ToList();

        /*
         * Không dùng Task.WhenAll vì các repository đang dùng chung
         * một AppDbContext. EF Core không hỗ trợ nhiều query đồng thời
         * trên cùng một DbContext.
         */
        var summary = await GetSummaryAsync(
            teacherUserId,
            classRoomIds);

        var exerciseAttempts =
            await GetExerciseAttemptsAsync(
                teacherUserId,
                classRoomIds,
                startUtc,
                endUtcExclusive);

        var testAttempts =
            await GetTestAttemptsAsync(
                teacherUserId,
                classRoomIds,
                startUtc,
                endUtcExclusive);

        var activity = dates
            .Select(date =>
            {
                var exerciseCount =
                    exerciseAttempts.Count(x =>
                        x.VietnamDate == date);

                var testCount =
                    testAttempts.Count(x =>
                        x.VietnamDate == date);

                return new TeacherDashboardActivityDto
                {
                    Date = date,
                    DateLabel = date.ToString("dd/MM"),
                    ExerciseAttempts = exerciseCount,
                    TestAttempts = testCount
                };
            })
            .ToList();

        var classActivities = classes
            .Select(classRoom =>
            {
                var data = dates
                    .Select(date =>
                    {
                        var exerciseCount =
                            exerciseAttempts.Count(x =>
                                x.ClassRoomId ==
                                    classRoom.ClassRoomId &&
                                x.VietnamDate == date);

                        var testCount =
                            testAttempts.Count(x =>
                                x.ClassRoomId ==
                                    classRoom.ClassRoomId &&
                                x.VietnamDate == date);

                        return new
                            TeacherDashboardClassActivityPointDto
                        {
                            Date = date,
                            DateLabel =
                                date.ToString("dd/MM"),

                            ExerciseAttempts =
                                exerciseCount,

                            TestAttempts =
                                testCount,

                            TotalAttempts =
                                exerciseCount + testCount
                        };
                    })
                    .ToList();

                return new TeacherDashboardClassActivityDto
                {
                    ClassRoomId =
                        classRoom.ClassRoomId,

                    ClassName =
                        classRoom.ClassName,

                    Data = data
                };
            })
            .ToList();

        return new TeacherDashboardDto
        {
            Summary = summary,
            Activity = activity,
            ClassActivities = classActivities
        };
    }

    private async Task<TeacherDashboardSummaryDto>
        GetSummaryAsync(
            int teacherUserId,
            List<int> classRoomIds)
    {
        /*
         * Tổng bài học:
         * - Thuộc section do giáo viên hiện tại tạo.
         * - Section thuộc một trong các lớp giáo viên được phân công.
         */
        var totalLessons = await _unitOfWork.Lessons
     .QueryLessonsBasic()
     .Where(x =>
         x.Section.TeacherUserId ==
             teacherUserId &&
         x.Section.ClassRoomId.HasValue &&
         classRoomIds.Contains(
             x.Section.ClassRoomId.Value))
     .Select(x => x.LessonId)
     .Distinct()
     .CountAsync();

        /*
         * Tổng bài tập:
         * - Do giáo viên hiện tại tạo.
         * - Thuộc section của lớp giáo viên hoặc đã giao cho lớp đó.
         * - Một bài được giao nhiều lớp vẫn chỉ đếm một lần.
         */
        var totalExercises = await _unitOfWork.Exercises
            .QueryBasic()
            .Where(x =>
                x.TeacherUserId == teacherUserId &&
                x.IsActive &&
                (
                    (
                        x.ExerciseSection != null &&
                        classRoomIds.Contains(
                            x.ExerciseSection.ClassRoomId)
                    )
                    ||
                    x.Assignments.Any(assignment =>
                        assignment.IsActive &&
                        classRoomIds.Contains(
                            assignment.ClassRoomId))
                ))
            .Select(x => x.ExerciseId)
            .Distinct()
            .CountAsync();

        /*
         * Tổng đề thi:
         * - Chỉ truy vấn bảng Test.
         * - Không truy vấn CompetitionTest nên Competition không được tính.
         */
        var totalTests = await _unitOfWork.Tests
            .QueryBasic()
            .Where(x =>
                x.TeacherUserId == teacherUserId &&
                x.IsActive &&
                x.Assignments.Any(assignment =>
                    assignment.IsActive &&
                    classRoomIds.Contains(
                        assignment.ClassRoomId)))
            .Select(x => x.TestId)
            .Distinct()
            .CountAsync();

        /*
         * StudentRole.ClassRoomId trong hệ thống hiện tại là nullable int,
         * nên phần này vẫn cần HasValue và Value.
         */
        var totalStudents = await _unitOfWork.StudentRoles
            .AsNoTracking()
            .Where(x =>
                x.ClassRoomId.HasValue &&
                classRoomIds.Contains(
                    x.ClassRoomId.Value))
            .Select(x => x.StudentRoleId)
            .Distinct()
            .CountAsync();

        return new TeacherDashboardSummaryDto
        {
            TotalLessons = totalLessons,
            TotalExercises = totalExercises,
            TotalTests = totalTests,
            TotalStudents = totalStudents
        };
    }

    private async Task<List<DashboardAttemptRecord>>
        GetExerciseAttemptsAsync(
            int teacherUserId,
            List<int> classRoomIds,
            DateTime startUtc,
            DateTime endUtcExclusive)
    {
        var records = await _unitOfWork
            .ExerciseAttempts
            .QueryBasic()
            .Where(x =>
                x.StartedAt >= startUtc &&
                x.StartedAt < endUtcExclusive &&

                classRoomIds.Contains(
                    x.ExerciseAssignment.ClassRoomId) &&

                x.ExerciseAssignment.Exercise
                    .TeacherUserId == teacherUserId &&

                x.ExerciseAssignment.IsActive &&

                x.ExerciseAssignment.Exercise.IsActive)
            .Select(x => new
            {
                x.ExerciseAttemptId,

                ClassRoomId =
                    x.ExerciseAssignment.ClassRoomId,

                x.StartedAt
            })
            .ToListAsync();

        return records
            .Select(x => new DashboardAttemptRecord
            {
                AttemptId =
                    x.ExerciseAttemptId,

                ClassRoomId =
                    x.ClassRoomId,

                VietnamDate =
                    x.StartedAt.AddHours(7).Date
            })
            .ToList();
    }

    private async Task<List<DashboardAttemptRecord>>
        GetTestAttemptsAsync(
            int teacherUserId,
            List<int> classRoomIds,
            DateTime startUtc,
            DateTime endUtcExclusive)
    {
        var records = await _unitOfWork
            .TestAttempts
            .QueryBasic()
            .Where(x =>
                x.StartedAt >= startUtc &&
                x.StartedAt < endUtcExclusive &&

                classRoomIds.Contains(
                    x.TestAssignment.ClassRoomId) &&

                x.TestAssignment.Test
                    .TeacherUserId == teacherUserId &&

                x.TestAssignment.IsActive &&

                x.TestAssignment.Test.IsActive)
            .Select(x => new
            {
                x.TestAttemptId,

                ClassRoomId =
                    x.TestAssignment.ClassRoomId,

                x.StartedAt
            })
            .ToListAsync();

        return records
            .Select(x => new DashboardAttemptRecord
            {
                AttemptId =
                    x.TestAttemptId,

                ClassRoomId =
                    x.ClassRoomId,

                VietnamDate =
                    x.StartedAt.AddHours(7).Date
            })
            .ToList();
    }

    private TeacherDashboardDto BuildEmptyDashboard(
        int days)
    {
        var vietnamToday =
            DateTime.UtcNow.AddHours(7).Date;

        var startDate =
            vietnamToday.AddDays(-(days - 1));

        return new TeacherDashboardDto
        {
            Summary =
                new TeacherDashboardSummaryDto(),

            Activity = Enumerable
                .Range(0, days)
                .Select(index =>
                {
                    var date =
                        startDate.AddDays(index);

                    return new TeacherDashboardActivityDto
                    {
                        Date = date,

                        DateLabel =
                            date.ToString("dd/MM"),

                        ExerciseAttempts = 0,

                        TestAttempts = 0
                    };
                })
                .ToList(),

            ClassActivities =
                new List<TeacherDashboardClassActivityDto>()
        };
    }

    private class DashboardAttemptRecord
    {
        public int AttemptId { get; set; }

        public int ClassRoomId { get; set; }

        public DateTime VietnamDate { get; set; }
    }
}