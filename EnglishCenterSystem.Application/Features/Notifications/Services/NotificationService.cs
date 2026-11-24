using EnglishCenterSystem.Application
    .Features.Notifications.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities;
using EnglishCenterSystem.Domain.Entities.Notifications;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace EnglishCenterSystem.Application
    .Features.Notifications.Services;

public class NotificationService
{
   
    private const int DefaultPage = 1;
    private const int MaximumBulkNotificationCount =
    200;

    private const int DefaultPageSize = 20;

    private const int MaximumPageSize = 100;

    private const int MaximumSearchLength = 100;
    private readonly IUnitOfWork
    _unitOfWork;

    private readonly INotificationRealtimeService
        _notificationRealtimeService;

    private readonly ILogger<NotificationService>
        _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        INotificationRealtimeService
            notificationRealtimeService,
        ILogger<NotificationService> logger)
    {
        _unitOfWork =
            unitOfWork;

        _notificationRealtimeService =
            notificationRealtimeService;

        _logger =
            logger;
    }

    public async Task<NotificationPagedResultDto>
    GetMyNotificationsAsync(
        int userId,
        NotificationQueryDto queryDto)
    {
        if (userId <= 0)
        {
            throw new Exception(
                "Invalid authenticated user");
        }

        queryDto ??=
            new NotificationQueryDto();

        var page =
            NormalizePage(
                queryDto.Page);

        var pageSize =
            NormalizePageSize(
                queryDto.PageSize);

        var search =
            NormalizeSearch(
                queryDto.Search);

        ValidateNotificationType(
            queryDto.Type);

        var query =
            _unitOfWork.Notifications
                .QueryUserNotifications()
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    !x.IsDeleted &&
                    !x.Notification.IsDeleted);

        if (queryDto.IsRead.HasValue)
        {
            var isRead =
                queryDto.IsRead.Value;

            query =
                query.Where(x =>
                    x.IsRead == isRead);
        }

        if (queryDto.Type.HasValue)
        {
            var notificationType =
                queryDto.Type.Value;

            query =
                query.Where(x =>
                    x.Notification.Type ==
                        notificationType);
        }

        if (!string.IsNullOrWhiteSpace(
            search))
        {
            query =
                query.Where(x =>
                    x.Notification.Title
                        .Contains(search) ||
                    x.Notification.Message
                        .Contains(search));
        }

        var totalItems =
            await query.CountAsync();

        var totalPages =
            CalculateTotalPages(
                totalItems,
                pageSize);

        var unreadCount =
            await _unitOfWork.Notifications
                .QueryUserNotifications()
                .AsNoTracking()
                .CountAsync(x =>
                    x.UserId == userId &&
                    !x.IsRead &&
                    !x.IsDeleted &&
                    !x.Notification.IsDeleted);

        var items =
            await query
                .OrderByDescending(x =>
                    x.CreatedAt)
                .ThenByDescending(x =>
                    x.UserNotificationId)
                .Skip(
                    (page - 1) *
                    pageSize)
                .Take(pageSize)
                .Select(x =>
                    new NotificationDto
                    {
                        UserNotificationId =
                            x.UserNotificationId,

                        NotificationId =
                            x.NotificationId,

                        Type =
                            x.Notification.Type,

                        TypeName =
                            x.Notification.Type
                                .ToString(),

                        Title =
                            x.Notification.Title,

                        Message =
                            x.Notification.Message,

                        ActionUrl =
                            x.ActionUrl,

                        RelatedEntityId =
                            x.Notification
                                .RelatedEntityId,

                        ClassRoomId =
                            x.Notification
                                .ClassRoomId,

                        ClassRoomName =
                            x.Notification
                                .ClassRoom == null
                                ? null
                                : x.Notification
                                    .ClassRoom
                                    .ClassName,

                        CreatedByUserId =
                            x.Notification
                                .CreatedByUserId,

                        CreatedByName =
                            string.IsNullOrWhiteSpace(
                                x.Notification
                                    .CreatedByUser
                                    .FullName)
                                ? x.Notification
                                    .CreatedByUser
                                    .UserName
                                : x.Notification
                                    .CreatedByUser
                                    .FullName,

                        IsRead =
                            x.IsRead,

                        ReadAt =
                            x.ReadAt,

                        CreatedAt =
                            x.CreatedAt
                    })
                .ToListAsync();

        return new NotificationPagedResultDto
        {
            Items =
                items,

            Page =
                page,

            PageSize =
                pageSize,

            TotalItems =
                totalItems,

            TotalPages =
                totalPages,

            UnreadCount =
                unreadCount
        };
    }
    private static int NormalizePage(
    int page)
    {
        return page <= 0
            ? DefaultPage
            : page;
    }
    private static int NormalizePageSize(
    int pageSize)
    {
        if (pageSize <= 0)
        {
            return DefaultPageSize;
        }

        return Math.Min(
            pageSize,
            MaximumPageSize);
    }
    private static string? NormalizeSearch(
    string? search)
    {
        if (string.IsNullOrWhiteSpace(
            search))
        {
            return null;
        }

        var normalized =
            search.Trim();

        if (normalized.Length >
            MaximumSearchLength)
        {
            normalized =
                normalized[
                    ..MaximumSearchLength];
        }

        return normalized;
    }
    private static int CalculateTotalPages(
    int totalItems,
    int pageSize)
    {
        if (totalItems <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(
            totalItems /
            (double)pageSize);
    }
    private static void ValidateNotificationType(
    NotificationType? notificationType)
    {
        if (!notificationType.HasValue)
        {
            return;
        }

        if (!Enum.IsDefined(
            typeof(NotificationType),
            notificationType.Value))
        {
            throw new Exception(
                "Invalid notification type");
        }
    }

    public async Task<int>
        GetUnreadCountAsync(
            int userId)
    {
        return await _unitOfWork.Notifications
            .QueryUserNotifications()
            .AsNoTracking()
            .CountAsync(x =>
                x.UserId == userId &&
                !x.IsRead &&
                !x.IsDeleted &&
                !x.Notification.IsDeleted);
    }

    public async Task MarkAsReadAsync(
        int userId,
        int userNotificationId)
    {
        var userNotification =
            await _unitOfWork.Notifications
                .QueryUserNotifications()
                .FirstOrDefaultAsync(x =>
                    x.UserNotificationId ==
                        userNotificationId &&
                    x.UserId ==
                        userId);

        if (userNotification == null)
        {
            throw new Exception(
                "Notification not found");
        }

        if (userNotification.IsRead)
        {
            return;
        }

        userNotification.IsRead = true;
        userNotification.ReadAt =
            DateTime.UtcNow;
        userNotification.UpdatedAt =
            DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        await SendUnreadCountSafelyAsync(
            userId);
    }

    public async Task MarkAllAsReadAsync(
    int userId)
    {
        var unreadNotifications =
            await _unitOfWork.Notifications
                .QueryUserNotifications()
                .Where(x =>
                    x.UserId == userId &&
                    !x.IsRead)
                .ToListAsync();

        if (unreadNotifications.Count == 0)
        {
            await SendUnreadCountSafelyAsync(
                userId);

            return;
        }

        var now =
            DateTime.UtcNow;

        foreach (var item in
            unreadNotifications)
        {
            item.IsRead = true;
            item.ReadAt = now;
            item.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync();

        await SendUnreadCountSafelyAsync(
            userId);
    }
    private async Task SendCreatedNotificationSafelyAsync(
    int userId,
    NotificationRealtimeDto notification,
    CancellationToken cancellationToken =
        default)
    {
        try
        {
            await _notificationRealtimeService
                .SendNotificationAsync(
                    userId,
                    notification,
                    cancellationToken);

            var unreadCount =
                await GetUnreadCountAsync(
                    userId);

            await _notificationRealtimeService
                .SendUnreadCountAsync(
                    userId,
                    unreadCount,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not send realtime notification to user {UserId}",
                userId);
        }
    }

    public async Task<int>
        CreateForClassAsync(
            CreateClassNotificationDto dto)
    {
        ValidateCreateDto(dto);

        var creatorExists =
            await _unitOfWork.Users
                .Query()
                .AnyAsync(x =>
                    x.UserId ==
                        dto.CreatedByUserId &&
                    x.IsActive &&
                    !x.IsDeleted);

        if (!creatorExists)
        {
            throw new Exception(
                "Notification creator not found");
        }

        var classRoom =
            await _unitOfWork.ClassRooms
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ClassRoomId ==
                        dto.ClassRoomId &&
                    !x.IsDeleted);

        if (classRoom == null)
        {
            throw new Exception(
                "Class room not found");
        }

        var studentRecipients =
            await _unitOfWork.StudentRoles
                .AsNoTracking()
                .Where(x =>
                    x.ClassRoomId ==
                        dto.ClassRoomId &&
                    !x.IsDeleted &&
                    x.User.IsActive &&
                    !x.User.IsDeleted)
                .Select(x =>
                    new
                    {
                        x.UserId
                    })
                .ToListAsync();

        var recipientMap =
            new Dictionary<int, string>();

        recipientMap[
            dto.CreatedByUserId
        ] = dto.TeacherActionUrl.Trim();

        foreach (var student in
            studentRecipients)
        {
            recipientMap[
                student.UserId
            ] = dto.StudentActionUrl.Trim();
        }

        var notification =
            new Notification
            {
                CreatedByUserId =
                    dto.CreatedByUserId,

                ClassRoomId =
                    dto.ClassRoomId,

                Type =
                    dto.Type,

                RelatedEntityId =
                    dto.RelatedEntityId,

                Title =
                    dto.Title.Trim(),

                Message =
                    dto.Message.Trim(),

                CreatedAt =
                    DateTime.UtcNow,

                IsDeleted =
                    false
            };

        foreach (var recipient in
            recipientMap)
        {
            notification.Recipients.Add(
                new UserNotification
                {
                    UserId =
                        recipient.Key,

                    ActionUrl =
                        recipient.Value,

                    IsRead =
                        false,

                    ReadAt =
                        null,

                    CreatedAt =
                        DateTime.UtcNow,

                    IsDeleted =
                        false
                });
        }

        await _unitOfWork.Notifications
     .AddAsync(notification);

        await _unitOfWork.SaveChangesAsync();

        var creator =
            await _unitOfWork.Users
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.UserId ==
                        dto.CreatedByUserId)
                .Select(x =>
                    new
                    {
                        x.FullName,
                        x.UserName
                    })
                .FirstAsync();

        foreach (var recipient in
            notification.Recipients)
        {
            var realtimeDto =
                new NotificationRealtimeDto
                {
                    UserNotificationId =
                        recipient.UserNotificationId,

                    NotificationId =
                        notification.NotificationId,

                    Type =
                        notification.Type,

                    TypeName =
                        notification.Type.ToString(),

                    Title =
                        notification.Title,

                    Message =
                        notification.Message,

                    ActionUrl =
                        recipient.ActionUrl,

                    RelatedEntityId =
                        notification.RelatedEntityId,

                    ClassRoomId =
                        notification.ClassRoomId,

                    ClassRoomName =
                        classRoom.ClassName,

                    CreatedByUserId =
                        notification.CreatedByUserId,

                    CreatedByName =
                        string.IsNullOrWhiteSpace(
                            creator.FullName)
                            ? creator.UserName
                            : creator.FullName,

                    IsRead =
                        false,

                    ReadAt =
                        null,

                    CreatedAt =
                        recipient.CreatedAt
                };

            await SendCreatedNotificationSafelyAsync(
                recipient.UserId,
                realtimeDto);
        }

        return notification.NotificationId;

        
    }

    public async Task<bool> CreateForUserIfNotExistsAsync(
        CreateUserNotificationDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.DeduplicationKey) ||
            string.IsNullOrWhiteSpace(dto.Title) ||
            string.IsNullOrWhiteSpace(dto.Message) ||
            string.IsNullOrWhiteSpace(dto.ActionUrl))
        {
            throw new Exception("Notification data is required");
        }

        var deduplicationKey = dto.DeduplicationKey.Trim();
        if (await _unitOfWork.Notifications
            .ExistsByDeduplicationKeyAsync(deduplicationKey))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var notification = new Notification
        {
            CreatedByUserId = dto.CreatedByUserId,
            ClassRoomId = dto.ClassRoomId,
            Type = dto.Type,
            RelatedEntityId = dto.RelatedEntityId,
            DeduplicationKey = deduplicationKey,
            Title = dto.Title.Trim(),
            Message = dto.Message.Trim(),
            CreatedAt = now,
            IsDeleted = false,
            Recipients =
            [
                new UserNotification
                {
                    UserId = dto.RecipientUserId,
                    ActionUrl = dto.ActionUrl.Trim(),
                    CreatedAt = now,
                    IsDeleted = false
                }
            ]
        };

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        await SendCreatedNotificationSafelyAsync(
            dto.RecipientUserId,
            new NotificationRealtimeDto
            {
                UserNotificationId = notification.Recipients.First().UserNotificationId,
                NotificationId = notification.NotificationId,
                Type = notification.Type,
                TypeName = notification.Type.ToString(),
                Title = notification.Title,
                Message = notification.Message,
                ActionUrl = notification.Recipients.First().ActionUrl,
                RelatedEntityId = notification.RelatedEntityId,
                ClassRoomId = notification.ClassRoomId,
                CreatedByUserId = notification.CreatedByUserId,
                CreatedByName = "English Center",
                IsRead = false,
                CreatedAt = now
            },
            cancellationToken);

        return true;
    }
    public async Task<bool>
CreateForAdminsIfNotExistsAsync(
    CreateAdminNotificationDto dto,
    CancellationToken cancellationToken =
        default)
    {
        ValidateCreateAdminDto(
            dto);

        var normalizedKey =
            dto.DeduplicationKey.Trim();

        var existed =
            await _unitOfWork.Notifications
                .ExistsByDeduplicationKeyAsync(
                    normalizedKey);

        if (existed)
        {
            return false;
        }

        var creator =
            await _unitOfWork.Users
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.UserId ==
                        dto.CreatedByUserId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .Select(x =>
                    new
                    {
                        x.UserId,
                        x.FullName,
                        x.UserName
                    })
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (creator == null)
        {
            throw new Exception(
                "Notification creator not found");
        }

        var adminUsers =
            await _unitOfWork.Users
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.Role ==
                        UserRole.Admin &&
                    x.IsActive &&
                    !x.IsDeleted)
                .Select(x =>
                    new
                    {
                        x.UserId
                    })
                .ToListAsync(
                    cancellationToken);

        if (adminUsers.Count == 0)
        {
            return false;
        }

        ClassRoom? classRoom =
            null;

        if (dto.ClassRoomId.HasValue)
        {
            classRoom =
                await _unitOfWork.ClassRooms
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.ClassRoomId ==
                            dto.ClassRoomId.Value &&
                            !x.IsDeleted,
                        cancellationToken);

            if (classRoom == null)
            {
                throw new Exception(
                    "Class room not found");
            }
        }

        var now =
            DateTime.UtcNow;

        var notification =
            new Notification
            {
                CreatedByUserId =
                    dto.CreatedByUserId,

                ClassRoomId =
                    dto.ClassRoomId,

                Type =
                    dto.Type,

                RelatedEntityId =
                    dto.RelatedEntityId,

                DeduplicationKey =
                    normalizedKey,

                Title =
                    dto.Title.Trim(),

                Message =
                    dto.Message.Trim(),

                CreatedAt =
                    now,

                IsDeleted =
                    false
            };

        foreach (var admin in adminUsers)
        {
            notification.Recipients.Add(
                new UserNotification
                {
                    UserId =
                        admin.UserId,

                    ActionUrl =
                        dto.ActionUrl.Trim(),

                    IsRead =
                        false,

                    ReadAt =
                        null,

                    CreatedAt =
                        now,

                    IsDeleted =
                        false
                });
        }

        try
        {
            await _unitOfWork.Notifications
                .AddAsync(
                    notification);

            await _unitOfWork
                .SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            /*
             * Unique index DeduplicationKey có thể
             * chặn job chạy trùng đồng thời.
             */
            var nowExists =
                await _unitOfWork.Notifications
                    .ExistsByDeduplicationKeyAsync(
                        normalizedKey);

            if (nowExists)
            {
                return false;
            }

            throw;
        }

        var creatorName =
            string.IsNullOrWhiteSpace(
                creator.FullName)
                ? creator.UserName
                : creator.FullName;

        foreach (var recipient in
            notification.Recipients)
        {
            var realtimeDto =
                new NotificationRealtimeDto
                {
                    UserNotificationId =
                        recipient
                            .UserNotificationId,

                    NotificationId =
                        notification
                            .NotificationId,

                    Type =
                        notification.Type,

                    TypeName =
                        notification.Type
                            .ToString(),

                    Title =
                        notification.Title,

                    Message =
                        notification.Message,

                    ActionUrl =
                        recipient.ActionUrl,

                    RelatedEntityId =
                        notification
                            .RelatedEntityId,

                    ClassRoomId =
                        notification.ClassRoomId,

                    ClassRoomName =
                        classRoom?.ClassName,

                    CreatedByUserId =
                        notification
                            .CreatedByUserId,

                    CreatedByName =
                        creatorName,

                    IsRead =
                        false,

                    ReadAt =
                        null,

                    CreatedAt =
                        recipient.CreatedAt
                };

            await SendCreatedNotificationSafelyAsync(
                recipient.UserId,
                realtimeDto,
                cancellationToken);
        }

        return true;
    }
    private static void ValidateCreateAdminDto(
    CreateAdminNotificationDto dto)
    {
        if (dto.CreatedByUserId <= 0)
        {
            throw new Exception(
                "Notification creator is required");
        }

        if (!Enum.IsDefined(
            typeof(NotificationType),
            dto.Type))
        {
            throw new Exception(
                "Invalid notification type");
        }

        if (string.IsNullOrWhiteSpace(
            dto.DeduplicationKey))
        {
            throw new Exception(
                "Notification deduplication key is required");
        }

        if (
            dto.DeduplicationKey
                .Trim()
                .Length > 200
        )
        {
            throw new Exception(
                "Notification deduplication key cannot exceed 200 characters");
        }

        if (string.IsNullOrWhiteSpace(
            dto.Title))
        {
            throw new Exception(
                "Notification title is required");
        }

        if (dto.Title.Trim().Length > 200)
        {
            throw new Exception(
                "Notification title cannot exceed 200 characters");
        }

        if (string.IsNullOrWhiteSpace(
            dto.Message))
        {
            throw new Exception(
                "Notification message is required");
        }

        if (
            dto.Message.Trim().Length >
            1000
        )
        {
            throw new Exception(
                "Notification message cannot exceed 1000 characters");
        }

        if (string.IsNullOrWhiteSpace(
            dto.ActionUrl))
        {
            throw new Exception(
                "Notification action URL is required");
        }

        if (
            dto.ActionUrl.Trim().Length >
            500
        )
        {
            throw new Exception(
                "Notification action URL cannot exceed 500 characters");
        }

        ValidateInternalUrl(
            dto.ActionUrl);
    }
    private static List<int>
    NormalizeUserNotificationIds(
        IEnumerable<int> userNotificationIds)
    {
        if (userNotificationIds == null)
        {
            return new List<int>();
        }

        return userNotificationIds
            .Where(x => x > 0)
            .Distinct()
            .Take(
                MaximumBulkNotificationCount)
            .ToList();
    }
    public async Task DeleteAsync(
    int userId,
    int userNotificationId)
    {
        if (userId <= 0)
        {
            throw new Exception(
                "Invalid authenticated user");
        }

        if (userNotificationId <= 0)
        {
            throw new Exception(
                "Invalid notification");
        }

        var userNotification =
            await _unitOfWork.Notifications
                .QueryUserNotifications()
                .FirstOrDefaultAsync(x =>
                    x.UserNotificationId ==
                        userNotificationId &&
                    x.UserId == userId &&
                    !x.IsDeleted);

        if (userNotification == null)
        {
            throw new Exception(
                "Notification not found");
        }

        _unitOfWork.Notifications
            .RemoveUserNotification(
                userNotification);

        await _unitOfWork
            .SaveChangesAsync();

        await SendUnreadCountSafelyAsync(
            userId);
    }
    public async Task<int>
    MarkManyAsReadAsync(
        int userId,
        IEnumerable<int>
            userNotificationIds)
    {
        if (userId <= 0)
        {
            throw new Exception(
                "Invalid authenticated user");
        }

        var ids =
            NormalizeUserNotificationIds(
                userNotificationIds);

        if (ids.Count == 0)
        {
            return 0;
        }

        var userNotifications =
            await _unitOfWork.Notifications
                .QueryUserNotifications()
                .Where(x =>
                    x.UserId == userId &&
                    ids.Contains(
                        x.UserNotificationId) &&
                    !x.IsDeleted &&
                    !x.Notification.IsDeleted &&
                    !x.IsRead)
                .ToListAsync();

        if (userNotifications.Count == 0)
        {
            return 0;
        }

        var now =
            DateTime.UtcNow;

        foreach (var item in
            userNotifications)
        {
            item.IsRead = true;
            item.ReadAt = now;
            item.UpdatedAt = now;
        }

        await _unitOfWork
            .SaveChangesAsync();

        await SendUnreadCountSafelyAsync(
            userId);

        return userNotifications.Count;
    }
  
    public async Task<int>
    DeleteManyAsync(
        int userId,
        IEnumerable<int>
            userNotificationIds)
    {
        if (userId <= 0)
        {
            throw new Exception(
                "Invalid authenticated user");
        }

        var ids =
            NormalizeUserNotificationIds(
                userNotificationIds);

        if (ids.Count == 0)
        {
            return 0;
        }

        var userNotifications =
            await _unitOfWork.Notifications
                .QueryUserNotifications()
                .Where(x =>
                    x.UserId == userId &&
                    ids.Contains(
                        x.UserNotificationId) &&
                    !x.IsDeleted)
                .ToListAsync();

        if (userNotifications.Count == 0)
        {
            return 0;
        }

        foreach (var item in
            userNotifications)
        {
            _unitOfWork.Notifications
                .RemoveUserNotification(
                    item);
        }

        await _unitOfWork
            .SaveChangesAsync();

        await SendUnreadCountSafelyAsync(
            userId);

        return userNotifications.Count;
    }
    private async Task SendUnreadCountSafelyAsync(
    int userId,
    CancellationToken cancellationToken =
        default)
    {
        try
        {
            var unreadCount =
                await GetUnreadCountAsync(
                    userId);

            await _notificationRealtimeService
                .SendUnreadCountAsync(
                    userId,
                    unreadCount,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not send realtime unread count to user {UserId}",
                userId);
        }
    }
    private static void ValidateCreateDto(
        CreateClassNotificationDto dto)
    {
        if (dto.CreatedByUserId <= 0)
        {
            throw new Exception(
                "Notification creator is required");
        }

        if (dto.ClassRoomId <= 0)
        {
            throw new Exception(
                "Class room is required");
        }

        if (!Enum.IsDefined(dto.Type))
        {
            throw new Exception(
                "Invalid notification type");
        }

        if (string.IsNullOrWhiteSpace(
            dto.Title))
        {
            throw new Exception(
                "Notification title is required");
        }

        if (dto.Title.Trim().Length > 200)
        {
            throw new Exception(
                "Notification title cannot exceed 200 characters");
        }

        if (string.IsNullOrWhiteSpace(
            dto.Message))
        {
            throw new Exception(
                "Notification message is required");
        }

        if (dto.Message.Trim().Length > 1000)
        {
            throw new Exception(
                "Notification message cannot exceed 1000 characters");
        }

        if (string.IsNullOrWhiteSpace(
            dto.TeacherActionUrl))
        {
            throw new Exception(
                "Teacher action URL is required");
        }

        if (string.IsNullOrWhiteSpace(
            dto.StudentActionUrl))
        {
            throw new Exception(
                "Student action URL is required");
        }

        if (dto.TeacherActionUrl.Trim().Length >
            500)
        {
            throw new Exception(
                "Teacher action URL cannot exceed 500 characters");
        }

        if (dto.StudentActionUrl.Trim().Length >
            500)
        {
            throw new Exception(
                "Student action URL cannot exceed 500 characters");
        }

        ValidateInternalUrl(
            dto.TeacherActionUrl);

        ValidateInternalUrl(
            dto.StudentActionUrl);
    }

    private static void ValidateInternalUrl(
        string actionUrl)
    {
        var value =
            actionUrl.Trim();

        if (!value.StartsWith('/'))
        {
            throw new Exception(
                "Notification action URL must be an internal route");
        }

        if (value.StartsWith("//"))
        {
            throw new Exception(
                "Notification action URL is invalid");
        }

        if (value.Contains(
            "://",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "External notification URLs are not allowed");
        }
    }
}
