using EnglishCenterSystem.Application.Features.Notifications.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishCenterSystem.Application.Features.Notifications.Services;

public class StudentReminderScanService(
    IUnitOfWork unitOfWork,
    NotificationService notificationService,
    ILogger<StudentReminderScanService> logger)
{
    private static readonly TimeSpan DeadlineReminderWindow = TimeSpan.FromHours(2);
    private static readonly TimeSpan ClassroomReminderWindow = TimeSpan.FromMinutes(15);

    public async Task ScanAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await RunSafelyAsync("assignment deadlines", () => ScanDeadlineRemindersAsync(now, cancellationToken));
        await RunSafelyAsync("online classrooms", () => ScanOnlineClassroomRemindersAsync(now, cancellationToken));
        await RunSafelyAsync("learning streaks", () => ScanLearningStreakRemindersAsync(now, cancellationToken));
    }

    private async Task ScanDeadlineRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var exerciseAssignments = await unitOfWork.ExerciseAssignments.Query()
            .AsNoTracking()
            .Where(x => x.IsActive && x.DeadlineAt > now && x.DeadlineAt <= now.Add(DeadlineReminderWindow))
            .Select(x => new { x.ExerciseAssignmentId, x.ClassRoomId, x.DeadlineAt, x.Exercise.Title, x.Exercise.TeacherUserId })
            .ToListAsync(cancellationToken);

        foreach (var assignment in exerciseAssignments)
        {
            await CreateDeadlineRemindersAsync(assignment.ExerciseAssignmentId, assignment.ClassRoomId, assignment.TeacherUserId,
                assignment.Title, assignment.DeadlineAt!.Value, "/student/exercises", "Exercise", cancellationToken);
        }

        var testAssignments = await unitOfWork.TestAssignments.Query()
            .AsNoTracking()
            .Where(x => x.IsActive && x.DeadlineAt > now && x.DeadlineAt <= now.Add(DeadlineReminderWindow))
            .Select(x => new { x.TestAssignmentId, x.ClassRoomId, x.DeadlineAt, x.Test.Title, x.Test.TeacherUserId })
            .ToListAsync(cancellationToken);

        foreach (var assignment in testAssignments)
        {
            await CreateDeadlineRemindersAsync(assignment.TestAssignmentId, assignment.ClassRoomId, assignment.TeacherUserId,
                assignment.Title, assignment.DeadlineAt!.Value, "/student/tests", "Test", cancellationToken);
        }
    }

    private async Task CreateDeadlineRemindersAsync(int assignmentId, int classRoomId, int teacherUserId,
        string title, DateTime deadlineAt, string actionUrl, string assignmentType, CancellationToken cancellationToken)
    {
        var studentUserIds = await unitOfWork.StudentRoles.AsNoTracking()
            .Where(x => x.ClassRoomId == classRoomId && !x.IsDeleted && x.User.IsActive && !x.User.IsDeleted)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        foreach (var studentUserId in studentUserIds)
        {
            await notificationService.CreateForUserIfNotExistsAsync(new CreateUserNotificationDto
            {
                CreatedByUserId = teacherUserId,
                RecipientUserId = studentUserId,
                Type = NotificationType.AssignmentDeadlineReminder,
                RelatedEntityId = assignmentId,
                ClassRoomId = classRoomId,
                DeduplicationKey = $"DeadlineReminder:{assignmentType}:{assignmentId}:{studentUserId}",
                Title = $"{assignmentType} deadline in 2 hours",
                Message = $"{assignmentType} \"{title}\" is due at {ToVietnamTime(deadlineAt):HH:mm dd/MM/yyyy}.",
                ActionUrl = actionUrl
            }, cancellationToken);
        }
    }

    private async Task ScanOnlineClassroomRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var classrooms = await unitOfWork.OnlineClassrooms.AsNoTracking()
            .Where(x => !x.IsDeleted && x.StartsAt > now && x.StartsAt <= now.Add(ClassroomReminderWindow))
            .Select(x => new { x.OnlineClassroomId, x.ClassRoomId, x.TeacherUserId, x.Title, x.StartsAt })
            .ToListAsync(cancellationToken);

        foreach (var classroom in classrooms)
        {
            var studentUserIds = await unitOfWork.StudentRoles.AsNoTracking()
                .Where(x => x.ClassRoomId == classroom.ClassRoomId && !x.IsDeleted && x.User.IsActive && !x.User.IsDeleted)
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);
            foreach (var studentUserId in studentUserIds)
            {
                await notificationService.CreateForUserIfNotExistsAsync(new CreateUserNotificationDto
                {
                    CreatedByUserId = classroom.TeacherUserId,
                    RecipientUserId = studentUserId,
                    Type = NotificationType.OnlineClassroomReminder,
                    RelatedEntityId = classroom.OnlineClassroomId,
                    ClassRoomId = classroom.ClassRoomId,
                    DeduplicationKey = $"OnlineClassroomReminder:{classroom.OnlineClassroomId}:{studentUserId}",
                    Title = "Online class starts in 15 minutes",
                    Message = $"\"{classroom.Title}\" starts at {ToVietnamTime(classroom.StartsAt):HH:mm dd/MM/yyyy}.",
                    ActionUrl = "/student/online-classrooms"
                }, cancellationToken);
            }
        }
    }

    private async Task ScanLearningStreakRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var vietnamNow = ToVietnamTime(now);
        if (vietnamNow.Hour != 20)
        {
            return;
        }

        var today = DateOnly.FromDateTime(vietnamNow);
        var students = await unitOfWork.StudentExperiences.AsNoTracking()
            .Where(x => x.LastActivityDate != today && !x.StudentRole.IsDeleted && x.StudentRole.User.IsActive && !x.StudentRole.User.IsDeleted)
            .Select(x => new { x.StudentRole.UserId, x.CurrentStreakDays })
            .ToListAsync(cancellationToken);

        foreach (var student in students)
        {
            await notificationService.CreateForUserIfNotExistsAsync(new CreateUserNotificationDto
            {
                CreatedByUserId = student.UserId,
                RecipientUserId = student.UserId,
                Type = NotificationType.LearningStreakReminder,
                DeduplicationKey = $"LearningStreakReminder:{today:yyyyMMdd}:{student.UserId}",
                Title = "Keep your learning streak going",
                Message = student.CurrentStreakDays > 0
                    ? $"Complete a learning activity today to protect your {student.CurrentStreakDays}-day streak."
                    : "Complete a learning activity today to begin your learning streak.",
                ActionUrl = "/student/exercises"
            }, cancellationToken);
        }
    }

    private async Task RunSafelyAsync(string scanName, Func<Task> scanAction)
    {
        try { await scanAction(); }
        catch (Exception ex) { logger.LogError(ex, "Student reminder scan failed: {ScanName}", scanName); }
    }

    private static DateTime ToVietnamTime(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(
        DateTime.SpecifyKind(utc, DateTimeKind.Utc), GetVietnamTimeZone());

    private static TimeZoneInfo GetVietnamTimeZone() => TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
}
