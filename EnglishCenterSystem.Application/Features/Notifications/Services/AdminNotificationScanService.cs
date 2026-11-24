using EnglishCenterSystem.Application
    .Features.Notifications.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishCenterSystem.Application
    .Features.Notifications.Services;

public class AdminNotificationScanService
{
    private readonly IUnitOfWork
        _unitOfWork;

    private readonly NotificationService
        _notificationService;

    private readonly ILogger
        <AdminNotificationScanService>
        _logger;

    public AdminNotificationScanService(
        IUnitOfWork unitOfWork,
        NotificationService
            notificationService,
        ILogger
            <AdminNotificationScanService>
            logger)
    {
        _unitOfWork =
            unitOfWork;

        _notificationService =
            notificationService;

        _logger =
            logger;
    }

   
    private async Task
ScanTeachersWithoutClassAsync(
    int creatorUserId,
    CancellationToken cancellationToken)
    {
        var teachers =
            await _unitOfWork.TeacherRoles
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    x.User.IsActive &&
                    !x.User.IsDeleted &&
                    !x.TeacherClasses.Any())
                .Select(x =>
                    new
                    {
                        x.UserId,

                        Name =
                            string.IsNullOrWhiteSpace(
                                x.User.FullName)
                                ? x.User.UserName
                                : x.User.FullName
                    })
                .ToListAsync(
                    cancellationToken);

        foreach (var teacher in teachers)
        {
            await _notificationService
                .CreateForAdminsIfNotExistsAsync(
                    new CreateAdminNotificationDto
                    {
                        CreatedByUserId =
                            creatorUserId,

                        Type =
                            NotificationType
                                .AdminTeacherUnassigned,

                        RelatedEntityId =
                            teacher.UserId,

                        DeduplicationKey =
                            $"AdminTeacherUnassigned:" +
                            $"{teacher.UserId}",

                        Title =
                            "Teacher has no assigned class",

                        Message =
                            $"Teacher {teacher.Name} " +
                            "has not been assigned " +
                            "to any class yet.",

                        ActionUrl =
                            "/admin/teachers"
                    },
                    cancellationToken);
        }
    }
    private async Task
ScanClassesWithoutTeacherAsync(
    int creatorUserId,
    CancellationToken cancellationToken)
    {
        var classes =
            await _unitOfWork.ClassRooms
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.IsOpen &&
                    !x.IsDeleted &&
                    !x.TeacherClasses.Any())
                .Select(x =>
                    new
                    {
                        x.ClassRoomId,
                        x.ClassName
                    })
                .ToListAsync(
                    cancellationToken);

        foreach (var classRoom in classes)
        {
            await _notificationService
                .CreateForAdminsIfNotExistsAsync(
                    new CreateAdminNotificationDto
                    {
                        CreatedByUserId =
                            creatorUserId,

                        Type =
                            NotificationType
                                .AdminClassWithoutTeacher,

                        RelatedEntityId =
                            classRoom.ClassRoomId,

                        ClassRoomId =
                            classRoom.ClassRoomId,

                        DeduplicationKey =
                            $"AdminClassWithoutTeacher:" +
                            $"{classRoom.ClassRoomId}",

                        Title =
                            "Class has no assigned teacher",

                        Message =
                            $"Class {classRoom.ClassName} " +
                            "does not have an assigned " +
                            "teacher yet.",

                        ActionUrl =
                            "/admin/courses"
                    },
                    cancellationToken);
        }
    }
    private async Task
ScanStudentsWithoutClassAsync(
    int creatorUserId,
    CancellationToken cancellationToken)
    {
        var students =
            await _unitOfWork.StudentRoles
                .AsNoTracking()
                .Where(x =>
                    x.ClassRoomId == null &&
                    !x.IsDeleted &&
                    x.User.IsActive &&
                    !x.User.IsDeleted)
                .Select(x =>
                    new
                    {
                        x.UserId,

                        Name =
                            string.IsNullOrWhiteSpace(
                                x.User.FullName)
                                ? x.User.UserName
                                : x.User.FullName
                    })
                .ToListAsync(
                    cancellationToken);

        foreach (var student in students)
        {
            await _notificationService
                .CreateForAdminsIfNotExistsAsync(
                    new CreateAdminNotificationDto
                    {
                        CreatedByUserId =
                            creatorUserId,

                        Type =
                            NotificationType
                                .AdminStudentUnassigned,

                        RelatedEntityId =
                            student.UserId,

                        DeduplicationKey =
                            $"AdminStudentUnassigned:" +
                            $"{student.UserId}",

                        Title =
                            "Student has no assigned class",

                        Message =
                            $"Student {student.Name} " +
                            "has not been assigned to a class yet.",

                        ActionUrl =
                            "/admin/students"
                    },
                    cancellationToken);
        }
    }
    private async Task
ScanClassesAtCapacityAsync(
    int creatorUserId,
    CancellationToken cancellationToken)
    {
        var classes =
            await _unitOfWork.ClassRooms
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.IsOpen &&
                    !x.IsDeleted &&
                    x.Capacity > 0)
                .Select(x =>
                    new
                    {
                        x.ClassRoomId,
                        x.ClassName,
                        x.Capacity,

                        StudentCount =
                            _unitOfWork.StudentRoles
                                .AsNoTracking()
                                .Count(student =>
                                    student.ClassRoomId ==
                                        x.ClassRoomId &&
                                    !student.IsDeleted &&
                                    student.User.IsActive &&
                                    !student.User.IsDeleted)
                    })
                .Where(x =>
                    x.StudentCount >=
                        x.Capacity)
                .ToListAsync(
                    cancellationToken);

        foreach (var classRoom in classes)
        {
            await _notificationService
                .CreateForAdminsIfNotExistsAsync(
                    new CreateAdminNotificationDto
                    {
                        CreatedByUserId =
                            creatorUserId,

                        Type =
                            NotificationType
                                .AdminClassAtCapacity,

                        RelatedEntityId =
                            classRoom.ClassRoomId,

                        ClassRoomId =
                            classRoom.ClassRoomId,

                        DeduplicationKey =
                            $"AdminClassAtCapacity:" +
                            $"{classRoom.ClassRoomId}",

                        Title =
                            "Class has reached maximum capacity",

                        Message =
                            $"Class {classRoom.ClassName} " +
                            $"has {classRoom.StudentCount}/" +
                            $"{classRoom.Capacity} students. " +
                            "Consider opening a new class.",

                        ActionUrl =
                            "/admin/courses"
                    },
                    cancellationToken);
        }
    }
    public async Task ScanAsync(
    CancellationToken cancellationToken =
        default)
    {
        var creatorUserId =
            await GetSystemAdminUserIdAsync(
                cancellationToken);

        await RunScanSafelyAsync(
            "teachers without class",
            () =>
                ScanTeachersWithoutClassAsync(
                    creatorUserId,
                    cancellationToken));

        await RunScanSafelyAsync(
            "classes without teacher",
            () =>
                ScanClassesWithoutTeacherAsync(
                    creatorUserId,
                    cancellationToken));

        await RunScanSafelyAsync(
            "students without class",
            () =>
                ScanStudentsWithoutClassAsync(
                    creatorUserId,
                    cancellationToken));

        await RunScanSafelyAsync(
            "classes at capacity",
            () =>
                ScanClassesAtCapacityAsync(
                    creatorUserId,
                    cancellationToken));
    }

    private async Task RunScanSafelyAsync(
        string scanName,
        Func<Task> scanAction)
    {
        try
        {
            await scanAction();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Admin notification scan failed: {ScanName}",
                scanName);
        }
    }
    private async Task<int>
    GetSystemAdminUserIdAsync(
        CancellationToken cancellationToken)
    {
        var adminUserId =
            await _unitOfWork.Users
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.Role ==
                        UserRole.Admin &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderBy(x =>
                    x.UserId)
                .Select(x =>
                    x.UserId)
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (adminUserId <= 0)
        {
            throw new Exception(
                "No active Admin account found");
        }

        return adminUserId;
    }
}
