using EnglishCenterSystem.Application
    .Features.AdminDashboard.DTOs;

using EnglishCenterSystem.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application
    .Features.AdminDashboard.Services;

public class AdminDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminDashboardService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminDashboardDto>
    GetDashboardAsync(
        int days = 7)
    {
        days = days switch
        {
            7 or 14 or 30 or 60 => days,
            _ => 7
        };

        var summary =
            await GetSummaryAsync();

        var activity =
            await GetActivityAsync(days);

        var courses =
            await GetCourseStatisticsAsync();

        var classes =
            await GetClassStatusesAsync();

        return new AdminDashboardDto
        {
            Summary = summary,
            Activity = activity,
            Courses = courses,
            Classes = classes
        };
    }
    private async Task<
    List<AdminDashboardCourseDto>>
    GetCourseStatisticsAsync()
    {
        /*
         * Lấy danh sách khóa học.
         */
        var courses =
            await _unitOfWork.Courses
                .Query()
                .AsNoTracking()
                .OrderBy(course =>
                    course.Name)
                .Select(course =>
                    new
                    {
                        course.CourseId,
                        course.Name,
                        course.CourseType
                    })
                .ToListAsync();

        /*
         * Lấy danh sách lớp để xác định:
         * - mỗi khóa có bao nhiêu lớp;
         * - mỗi lớp thuộc khóa nào.
         */
        var classRooms =
            await _unitOfWork.ClassRooms
                .Query()
                .AsNoTracking()
                .Select(classRoom =>
                    new
                    {
                        classRoom.ClassRoomId,
                        classRoom.CourseId
                    })
                .ToListAsync();

        /*
         * Đếm học viên đang nằm trong từng lớp.
         */
        var students =
            await _unitOfWork.StudentRoles
                .AsNoTracking()
                .Where(student =>
                    student.ClassRoomId != null &&
                    !student.IsDeleted &&
                    student.User.IsActive &&
                    !student.User.IsDeleted)
                .Select(student =>
                    new
                    {
                        ClassRoomId =
                            student.ClassRoomId!.Value
                    })
                .ToListAsync();

        /*
         * Lấy quan hệ giáo viên - lớp.
         *
         * Không đếm trực tiếp số TeacherClass vì một
         * giáo viên có thể dạy nhiều lớp trong cùng khóa.
         */
        var teacherAssignments =
            await _unitOfWork.TeacherRoles
                .AsNoTracking()
                .Where(teacher =>
                    !teacher.IsDeleted &&
                    teacher.User.IsActive &&
                    !teacher.User.IsDeleted)
                .SelectMany(
                    teacher =>
                        teacher.TeacherClasses,
                    (
                        teacher,
                        teacherClass
                    ) =>
                        new
                        {
                            teacher.UserId,
                            teacherClass.ClassRoomId
                        })
                .ToListAsync();

        var result =
            courses
                .Select(course =>
                {
                    var courseClassIds =
                        classRooms
                            .Where(classRoom =>
                                classRoom.CourseId ==
                                course.CourseId)
                            .Select(classRoom =>
                                classRoom.ClassRoomId)
                            .ToHashSet();

                    var studentCount =
                        students.Count(student =>
                            courseClassIds.Contains(
                                student.ClassRoomId));

                    var teacherCount =
                        teacherAssignments
                            .Where(assignment =>
                                courseClassIds.Contains(
                                    assignment
                                        .ClassRoomId))
                            .Select(assignment =>
                                assignment.UserId)
                            .Distinct()
                            .Count();

                    return new AdminDashboardCourseDto
                    {
                        CourseId =
                            course.CourseId,

                        Name =
                            course.Name,

                        Tag =
                            course.CourseType
                                .ToString(),

                        ClassCount =
                            courseClassIds.Count,

                        StudentCount =
                            studentCount,

                        TeacherCount =
                            teacherCount
                    };
                })
                .ToList();

        return result;
    }
    private async Task<
    List<AdminDashboardClassDto>>
    GetClassStatusesAsync()
    {
        var classes =
            await _unitOfWork.ClassRooms
                .Query()
                .AsNoTracking()
                .OrderBy(classRoom =>
                    classRoom.ClassName)
                .Select(classRoom =>
                    new
                    {
                        classRoom.ClassRoomId,
                        classRoom.ClassName,
                        classRoom.Capacity,

                        CourseName =
                            classRoom.Course.Name,

                        TeacherName =
                            classRoom.TeacherClasses
                                .Select(teacherClass =>
                                    string.IsNullOrWhiteSpace(
                                        teacherClass
                                            .TeacherRole
                                            .User
                                            .FullName)
                                        ? teacherClass
                                            .TeacherRole
                                            .User
                                            .UserName
                                        : teacherClass
                                            .TeacherRole
                                            .User
                                            .FullName)
                                .FirstOrDefault()
                    })
                .ToListAsync();

        var studentCounts =
            await _unitOfWork.StudentRoles
                .AsNoTracking()
                .Where(student =>
                    student.ClassRoomId != null &&
                    !student.IsDeleted &&
                    student.User.IsActive &&
                    !student.User.IsDeleted)
                .GroupBy(student =>
                    student.ClassRoomId!.Value)
                .Select(group =>
                    new
                    {
                        ClassRoomId =
                            group.Key,

                        StudentCount =
                            group.Count()
                    })
                .ToDictionaryAsync(
                    item =>
                        item.ClassRoomId,

                    item =>
                        item.StudentCount);

        return classes
            .Select(classRoom =>
                new AdminDashboardClassDto
                {
                    ClassRoomId =
                        classRoom.ClassRoomId,

                    ClassName =
                        classRoom.ClassName,

                    CourseName =
                        classRoom.CourseName,

                    TeacherName =
                        classRoom.TeacherName
                        ?? "Chưa phân công",

                    StudentCount =
                        studentCounts
                            .GetValueOrDefault(
                                classRoom.ClassRoomId),

                    Capacity =
                        classRoom.Capacity
                })
            .ToList();
    }
    private async Task<AdminDashboardSummaryDto>
        GetSummaryAsync()
    {
        /*
         * Tổng học viên:
         * chỉ tính học viên đã được xếp vào lớp.
         */
        var totalStudents =
            await _unitOfWork.StudentRoles
                .AsNoTracking()
                .CountAsync(student =>
                    student.ClassRoomId != null &&
                    !student.User.IsDeleted);

        /*
         * Tổng giáo viên:
         * chỉ tính giáo viên đã được phân ít nhất một lớp.
         *
         * Không cộng theo TeacherClass vì một giáo viên
         * có thể dạy nhiều lớp và sẽ bị đếm trùng.
         */
        var totalTeachers =
            await _unitOfWork.TeacherRoles
                .AsNoTracking()
                .CountAsync(teacher =>
                    !teacher.User.IsDeleted &&
                    teacher.TeacherClasses.Any());

        var totalCourses =
            await _unitOfWork.Courses
                .Query()
                .AsNoTracking()
                .CountAsync();

        var totalClasses =
            await _unitOfWork.ClassRooms
                .Query()
                .AsNoTracking()
                .CountAsync();

        return new AdminDashboardSummaryDto
        {
            TotalStudents = totalStudents,
            TotalTeachers = totalTeachers,
            TotalCourses = totalCourses,
            TotalClasses = totalClasses
        };
    }

    private async Task<List<AdminDashboardActivityDto>>
        GetActivityAsync(
            int days)
    {
        /*
         * Backend và database của hệ thống đang lưu UTC.
         * Biểu đồ cần gom dữ liệu theo ngày Việt Nam UTC+7.
         */
        var vietnamTimeZone =
            GetVietnamTimeZone();

        var vietnamNow =
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                vietnamTimeZone);

        var firstVietnamDate =
            vietnamNow.Date.AddDays(-(days - 1));

        var endVietnamDate =
            vietnamNow.Date.AddDays(1);

        var startUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                firstVietnamDate,
                vietnamTimeZone);

        var endUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                endVietnamDate,
                vietnamTimeZone);

        /*
         * Lượt học bài:
         * một bản ghi tiến độ đã hoàn thành được tính
         * là một lượt hoàn thành bài học.
         */
        var lessonTimes =
            await _unitOfWork.Lessons
                .QueryLessonsBasic()
                .SelectMany(lesson =>
                    lesson.StudentProgresses)
                .AsNoTracking()
                .Where(progress =>
                    progress.IsCompleted &&
                    progress.CreatedAt >= startUtc &&
                    progress.CreatedAt < endUtc)
                .Select(progress =>
                    progress.CreatedAt)
                .ToListAsync();

        /*
         * Lượt làm bài tập:
         * dùng StartedAt, tức là số lượt học viên
         * bắt đầu làm bài tập trong ngày.
         */
        var exerciseTimes =
            await _unitOfWork.ExerciseAttempts
                .QueryBasic()
                .Where(attempt =>
                    attempt.StartedAt >= startUtc &&
                    attempt.StartedAt < endUtc)
                .Select(attempt =>
                    attempt.StartedAt)
                .ToListAsync();

        /*
         * Lượt làm đề thi:
         * cũng tính theo thời điểm bắt đầu attempt.
         */
        var testTimes =
            await _unitOfWork.TestAttempts
                .QueryBasic()
                .Where(attempt =>
                    attempt.StartedAt >= startUtc &&
                    attempt.StartedAt < endUtc)
                .Select(attempt =>
                    attempt.StartedAt)
                .ToListAsync();

        var lessonCountByDate =
            GroupUtcTimesByVietnamDate(
                lessonTimes,
                vietnamTimeZone);

        var exerciseCountByDate =
            GroupUtcTimesByVietnamDate(
                exerciseTimes,
                vietnamTimeZone);

        var testCountByDate =
            GroupUtcTimesByVietnamDate(
                testTimes,
                vietnamTimeZone);

        var result =
            new List<AdminDashboardActivityDto>();

        for (var index = 0;
             index < days;
             index++)
        {
            var date =
                firstVietnamDate.AddDays(index);

            result.Add(
                new AdminDashboardActivityDto
                {
                    Date = date,

                    DateLabel =
                        date.ToString("dd/MM"),

                    LessonCompletions =
                        lessonCountByDate
                            .GetValueOrDefault(
                                date),

                    ExerciseAttempts =
                        exerciseCountByDate
                            .GetValueOrDefault(
                                date),

                    TestAttempts =
                        testCountByDate
                            .GetValueOrDefault(
                                date)
                });
        }

        return result;
    }

    private static Dictionary<DateTime, int>
        GroupUtcTimesByVietnamDate(
            IEnumerable<DateTime> utcTimes,
            TimeZoneInfo vietnamTimeZone)
    {
        return utcTimes
            .Select(utcTime =>
                TimeZoneInfo
                    .ConvertTimeFromUtc(
                        DateTime.SpecifyKind(
                            utcTime,
                            DateTimeKind.Utc),
                        vietnamTimeZone)
                    .Date)
            .GroupBy(date => date)
            .ToDictionary(
                group => group.Key,
                group => group.Count());
    }

    private static TimeZoneInfo
        GetVietnamTimeZone()
    {
        try
        {
            /*
             * Linux / Docker / production.
             */
            return TimeZoneInfo
                .FindSystemTimeZoneById(
                    "Asia/Ho_Chi_Minh");
        }
        catch (
            TimeZoneNotFoundException)
        {
            /*
             * Windows.
             */
            return TimeZoneInfo
                .FindSystemTimeZoneById(
                    "SE Asia Standard Time");
        }
    }
}
