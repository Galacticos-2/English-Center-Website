using EnglishCenterSystem.Application
    .Features.TeacherStudents.DTOs;

using EnglishCenterSystem.Application.Interfaces;

using EnglishCenterSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application
    .Features.TeacherStudents.Services;

public class TeacherStudentDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public TeacherStudentDashboardService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TeacherStudentDashboardDto>
        GetDashboardAsync(
            int teacherUserId,
            int? classRoomId,
            int? courseId,
            string? search,
            string? sortBy)
    {
        var teacherClasses =
            await _unitOfWork
                .TeacherRoleRepository
                .Query()
                .Where(x =>
                    x.UserId == teacherUserId)
                .SelectMany(x =>
                    x.TeacherClasses)
                .Select(x => new
                {
                    x.ClassRoomId,

                    ClassName =
                        x.ClassRoom.ClassName,

                    CourseId =
                        x.ClassRoom.CourseId,

                    CourseName =
                        x.ClassRoom.Course.Name
                })
                .Distinct()
                .OrderBy(x => x.CourseName)
                .ThenBy(x => x.ClassName)
                .ToListAsync();

        var result =
            new TeacherStudentDashboardDto
            {
                Courses = teacherClasses
                    .GroupBy(x => new
                    {
                        x.CourseId,
                        x.CourseName
                    })
                    .Select(group =>
                        new TeacherStudentCourseFilterDto
                        {
                            CourseId =
                                group.Key.CourseId,

                            CourseName =
                                group.Key.CourseName
                        })
                    .OrderBy(x => x.CourseName)
                    .ToList(),

                Classes = teacherClasses
                    .Select(x =>
                        new TeacherStudentClassFilterDto
                        {
                            ClassRoomId =
                                x.ClassRoomId,

                            ClassName =
                                x.ClassName,

                            CourseId =
                                x.CourseId,

                            CourseName =
                                x.CourseName
                        })
                    .ToList()
            };

        if (teacherClasses.Count == 0)
        {
            return result;
        }

        var accessibleClasses =
            teacherClasses.AsEnumerable();

        if (courseId.HasValue)
        {
            accessibleClasses =
                accessibleClasses.Where(x =>
                    x.CourseId ==
                    courseId.Value);
        }

        if (classRoomId.HasValue)
        {
            accessibleClasses =
                accessibleClasses.Where(x =>
                    x.ClassRoomId ==
                    classRoomId.Value);
        }

        var selectedClasses =
            accessibleClasses.ToList();

        var selectedClassRoomIds =
            selectedClasses
                .Select(x => x.ClassRoomId)
                .Distinct()
                .ToList();

        if (selectedClassRoomIds.Count == 0)
        {
            result.Summary.SelectedClassName =
                classRoomId.HasValue
                    ? "Không tìm thấy lớp"
                    : "Tất cả lớp";

            return result;
        }

        var students =
            await _unitOfWork
                .StudentRoleRepository
                .Query()
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.ClassRoom)
                    .ThenInclude(x =>
                        x!.Course)
                .Where(x =>
                    x.ClassRoomId.HasValue &&
                    selectedClassRoomIds.Contains(
                        x.ClassRoomId.Value))
                .OrderBy(x =>
                    x.User.FullName)
                .ToListAsync();

        var studentRoleIds =
            students
                .Select(x => x.StudentRoleId)
                .ToList();

        /*
         * BÀI HỌC CỦA GIÁO VIÊN
         *
         * Chỉ tính lesson:
         * - thuộc lớp đang lọc
         * - do chính giáo viên hiện tại tạo
         */
        var lessonRows =
            await _unitOfWork
                .Lessons
                .QueryLessonsBasic()
                .Where(lesson =>
                    lesson.Section
                        .ClassRoomId.HasValue &&

                    selectedClassRoomIds.Contains(
                        lesson.Section
                            .ClassRoomId.Value) &&

                    lesson.Section
                        .TeacherUserId ==
                    teacherUserId)
                .Select(lesson => new
                {
                    lesson.LessonId,

                    ClassRoomId =
                        lesson.Section
                            .ClassRoomId!.Value
                })
                .ToListAsync();

        var lessonIds =
            lessonRows
                .Select(x => x.LessonId)
                .ToList();

        var lessonProgressRows =
            lessonIds.Count == 0 ||
            studentRoleIds.Count == 0
                ? []
                : await _unitOfWork
                    .Lessons
                    .QueryLessonsBasic()
                    .Where(lesson =>
                        lessonIds.Contains(
                            lesson.LessonId))
                    .SelectMany(lesson =>
                        lesson.StudentProgresses
                            .Where(progress =>
                                progress.IsCompleted &&

                                studentRoleIds.Contains(
                                    progress
                                        .StudentRoleId))
                            .Select(progress =>
                                new
                                {
                                    progress
                                        .StudentRoleId,

                                    progress.LessonId
                                }))
                    .ToListAsync();

        /*
         * BÀI TẬP ĐÃ GIAO
         */
        var exerciseAssignments =
            await _unitOfWork
                .ExerciseAssignments
                .Query()
                .Where(assignment =>
                    assignment.IsActive &&

                    assignment.Exercise
                        .TeacherUserId ==
                    teacherUserId &&

                    selectedClassRoomIds.Contains(
                        assignment.ClassRoomId))
                .Select(assignment => new
                {
                    assignment
                        .ExerciseAssignmentId,

                    assignment.ClassRoomId
                })
                .ToListAsync();

        var exerciseAssignmentIds =
            exerciseAssignments
                .Select(x =>
                    x.ExerciseAssignmentId)
                .ToList();

        var exerciseAttempts =
            exerciseAssignmentIds.Count == 0 ||
            studentRoleIds.Count == 0
                ? []
                : await _unitOfWork
                    .ExerciseAttempts
                    .QueryBasic()
                    .Where(attempt =>
                        exerciseAssignmentIds.Contains(
                            attempt
                                .ExerciseAssignmentId) &&

                        studentRoleIds.Contains(
                            attempt.StudentRoleId) &&

                        (
                            attempt.Status ==
                                ExerciseAttemptStatus
                                    .Submitted ||

                            attempt.Status ==
                                ExerciseAttemptStatus
                                    .Reviewed ||

                            attempt.Status ==
                                ExerciseAttemptStatus
                                    .AiGraded
                        ))
                    .Select(attempt => new
                    {
                        attempt.StudentRoleId,

                        attempt
                            .ExerciseAssignmentId,

                        attempt.Score
                    })
                    .ToListAsync();

        /*
         * ĐỀ THI ĐÃ GIAO
         */
        var testAssignments =
            await _unitOfWork
                .TestAssignments
                .QueryBasic()
                .Where(assignment =>
                    assignment.IsActive &&

                    assignment.Test.IsActive &&

                    assignment.Test
                        .TeacherUserId ==
                    teacherUserId &&

                    selectedClassRoomIds.Contains(
                        assignment.ClassRoomId))
                .Select(assignment => new
                {
                    assignment.TestAssignmentId,

                    assignment.ClassRoomId
                })
                .ToListAsync();

        var testAssignmentIds =
            testAssignments
                .Select(x =>
                    x.TestAssignmentId)
                .ToList();

        var testAttempts =
            testAssignmentIds.Count == 0 ||
            studentRoleIds.Count == 0
                ? []
                : await _unitOfWork
                    .TestAttempts
                    .QueryBasic()
                    .Where(attempt =>
                        testAssignmentIds.Contains(
                            attempt
                                .TestAssignmentId) &&

                        studentRoleIds.Contains(
                            attempt.StudentRoleId) &&

                        (
                            attempt.Status ==
                                TestAttemptStatus
                                    .Submitted ||

                            attempt.Status ==
                                TestAttemptStatus
                                    .AiGraded
                        ))
                    .Select(attempt => new
                    {
                        attempt.StudentRoleId,

                        attempt.TestAssignmentId,

                        attempt.Score
                    })
                    .ToListAsync();

        /*
         * ĐỀ THI ĐẤU ĐÃ GIAO
         *
         * Điểm trung bình trên màn học viên giáo viên
         * lấy từ competition test, không lấy từ test thường.
         */
        var competitionTestAssignments =
            await _unitOfWork
                .CompetitionTestAssignments
                .QueryBasic()
                .Where(assignment =>
                    assignment.IsActive &&

                    assignment.TeacherUserId ==
                    teacherUserId &&

                    selectedClassRoomIds.Contains(
                        assignment.ClassRoomId))
                .Select(assignment => new
                {
                    assignment
                        .CompetitionTestAssignmentId,

                    assignment.ClassRoomId
                })
                .ToListAsync();

        var competitionTestAssignmentIds =
            competitionTestAssignments
                .Select(x =>
                    x.CompetitionTestAssignmentId)
                .ToList();

        var competitionTestAttempts =
            competitionTestAssignmentIds.Count == 0 ||
            studentRoleIds.Count == 0
                ? []
                : await _unitOfWork
                    .CompetitionTestAttempts
                    .QueryBasic()
                    .Where(attempt =>
                        competitionTestAssignmentIds
                            .Contains(
                                attempt
                                    .CompetitionTestAssignmentId) &&

                        studentRoleIds.Contains(
                            attempt.StudentRoleId) &&

                        (
                            attempt.Status ==
                                CompetitionAttemptStatus
                                    .Submitted ||

                            attempt.Status ==
                                CompetitionAttemptStatus
                                    .AiGraded ||

                            attempt.Status ==
                                CompetitionAttemptStatus
                                    .Reviewed
                        ))
                    .Select(attempt => new
                    {
                        attempt.StudentRoleId,

                        attempt
                            .CompetitionTestAssignmentId,

                        attempt.Score
                    })
                    .ToListAsync();

        var dashboardStudents =
            students.Select(student =>
            {
                var studentClassRoomId =
                    student.ClassRoomId!.Value;

                var studentLessonIds =
                    lessonRows
                        .Where(x =>
                            x.ClassRoomId ==
                            studentClassRoomId)
                        .Select(x => x.LessonId)
                        .Distinct()
                        .ToList();

                var completedLessonCount =
                    lessonProgressRows
                        .Where(x =>
                            x.StudentRoleId ==
                            student.StudentRoleId &&

                            studentLessonIds.Contains(
                                x.LessonId))
                        .Select(x => x.LessonId)
                        .Distinct()
                        .Count();

                var totalLessonCount =
                    studentLessonIds.Count;

                var studentExerciseAssignmentIds =
                    exerciseAssignments
                        .Where(x =>
                            x.ClassRoomId ==
                            studentClassRoomId)
                        .Select(x =>
                            x.ExerciseAssignmentId)
                        .Distinct()
                        .ToList();

                var completedExerciseCount =
                    exerciseAttempts
                        .Where(x =>
                            x.StudentRoleId ==
                            student.StudentRoleId &&

                            studentExerciseAssignmentIds
                                .Contains(
                                    x.ExerciseAssignmentId))
                        .Select(x =>
                            x.ExerciseAssignmentId)
                        .Distinct()
                        .Count();

                var studentTestAssignmentIds =
                    testAssignments
                        .Where(x =>
                            x.ClassRoomId ==
                            studentClassRoomId)
                        .Select(x =>
                            x.TestAssignmentId)
                        .Distinct()
                        .ToList();

                /*
                 * Mỗi đề lấy attempt có điểm cao nhất.
                 */
                var bestTestScores =
                    testAttempts
                        .Where(x =>
                            x.StudentRoleId ==
                            student.StudentRoleId &&

                            studentTestAssignmentIds
                                .Contains(
                                    x.TestAssignmentId))
                        .GroupBy(x =>
                            x.TestAssignmentId)
                        .Select(group =>
                            group.Max(x => x.Score))
                        .ToList();

                var studentCompetitionTestAssignmentIds =
                    competitionTestAssignments
                        .Where(x =>
                            x.ClassRoomId ==
                            studentClassRoomId)
                        .Select(x =>
                            x.CompetitionTestAssignmentId)
                        .Distinct()
                        .ToList();

                /*
                 * Mỗi competition test lấy attempt có điểm cao nhất.
                 */
                var bestCompetitionTestScores =
                    competitionTestAttempts
                        .Where(x =>
                            x.StudentRoleId ==
                            student.StudentRoleId &&

                            studentCompetitionTestAssignmentIds
                                .Contains(
                                    x.CompetitionTestAssignmentId))
                        .GroupBy(x =>
                            x.CompetitionTestAssignmentId)
                        .Select(group =>
                            group.Max(x => x.Score))
                        .ToList();

                return new
                    TeacherStudentDashboardItemDto
                {
                    StudentRoleId =
                        student.StudentRoleId,

                    UserId =
                        student.UserId,

                    FullName =
                        student.User.FullName ??
                        student.User.UserName ??
                        "",

                    Email =
                        student.User.Email ?? "",

                    AvatarUrl =
                        student.User.AvatarUrl,

                    ClassRoomId =
                        studentClassRoomId,

                    ClassRoomName =
                        student.ClassRoom
                            ?.ClassName ?? "",

                    CourseId =
                        student.ClassRoom
                            ?.CourseId ?? 0,

                    CourseName =
                        student.ClassRoom
                            ?.Course?.Name ?? "",

                    CompletedLessons =
                        completedLessonCount,

                    TotalLessons =
                        totalLessonCount,

                    LessonProgressPercent =
                        CalculatePercent(
                            completedLessonCount,
                            totalLessonCount),

                    CompletedExercises =
                        completedExerciseCount,

                    TotalAssignedExercises =
                        studentExerciseAssignmentIds
                            .Count,

                    ExerciseCompletionPercent =
                        CalculatePercent(
                            completedExerciseCount,

                            studentExerciseAssignmentIds
                                .Count),

                    CompletedTests =
                        bestTestScores.Count,

                    TotalAssignedTests =
                        studentTestAssignmentIds
                            .Count,

                    AverageTestScore =
                        bestCompetitionTestScores.Count == 0
                            ? null
                            : Math.Round(
                                bestCompetitionTestScores
                                    .Average(),
                                2)
                };
            })
            .ToList();

        /*
         * Summary không bị ảnh hưởng bởi ô tìm kiếm.
         * Tìm kiếm chỉ lọc danh sách bên dưới.
         */
        result.Summary =
            new TeacherStudentDashboardSummaryDto
            {
                SelectedClassName =
                    classRoomId.HasValue
                        ? selectedClasses
                            .FirstOrDefault()
                            ?.ClassName ??
                            "Không xác định"
                        : "Tất cả lớp",

                SelectedCourseName =
                    courseId.HasValue
                        ? selectedClasses
                            .FirstOrDefault()
                            ?.CourseName ??
                            "Không xác định"
                        : "Tất cả khóa học",

                TotalStudents =
                    dashboardStudents.Count,

                AverageLessonProgressPercent =
                    dashboardStudents.Count == 0
                        ? 0
                        : Math.Round(
                            dashboardStudents.Average(
                                x =>
                                    x.LessonProgressPercent),
                            2),

                AverageExerciseCompletionPercent =
                    dashboardStudents.Count == 0
                        ? 0
                        : Math.Round(
                            dashboardStudents.Average(
                                x =>
                                    x.ExerciseCompletionPercent),
                            2),

                AverageTestScore =
                    dashboardStudents
                        .Where(x =>
                            x.AverageTestScore.HasValue)
                        .Select(x =>
                            x.AverageTestScore!.Value)
                        .DefaultIfEmpty(0)
                        .Average()
            };

        var filteredStudents =
            dashboardStudents.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword =
                search.Trim().ToLowerInvariant();

            filteredStudents =
                filteredStudents.Where(student =>
                    student.FullName
                        .ToLowerInvariant()
                        .Contains(keyword) ||

                    student.Email
                        .ToLowerInvariant()
                        .Contains(keyword));
        }

        filteredStudents =
            sortBy?.Trim().ToLowerInvariant()
            switch
            {
                "lesson-progress" =>
                    filteredStudents
                        .OrderByDescending(x =>
                            x.LessonProgressPercent)
                        .ThenBy(x =>
                            x.FullName),

                "exercise-completion" =>
                    filteredStudents
                        .OrderByDescending(x =>
                            x.ExerciseCompletionPercent)
                        .ThenBy(x =>
                            x.FullName),

                "test-score" =>
                    filteredStudents
                        .OrderByDescending(x =>
                            x.AverageTestScore.HasValue)
                        .ThenByDescending(x =>
                            x.AverageTestScore)
                        .ThenBy(x =>
                            x.FullName),

                _ =>
                    filteredStudents
                        .OrderBy(x =>
                            x.FullName)
            };

        result.Students =
            filteredStudents.ToList();

        result.Summary.AverageTestScore =
            Math.Round(
                result.Summary
                    .AverageTestScore,
                2);

        return result;
    }

    private static double CalculatePercent(
        int completed,
        int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return Math.Round(
            (double)completed /
            total *
            100,
            2);
    }
}
