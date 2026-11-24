using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application
    .Features.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnglishCenterSystem.Application
    .Features.Notifications.DTOs;
using EnglishCenterSystem.Application
    .Features.Notifications.DTOs;
namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController
    : ControllerBase
{
    private readonly NotificationService
        _notificationService;

    public NotificationController(
        NotificationService notificationService)
    {
        _notificationService =
            notificationService;
    }

    [HttpPut("read-many")]
    public async Task<IActionResult>
    MarkManyAsRead(
        [FromBody]
        NotificationIdsDto dto)
    {
        var affectedCount =
            await _notificationService
                .MarkManyAsReadAsync(
                    GetCurrentUserId(),
                    dto.UserNotificationIds);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    new
                    {
                        affectedCount
                    },
                    "Notifications marked as read"));
    }
    [HttpDelete(
    "{userNotificationId:int}")]
    public async Task<IActionResult>
    Delete(
        int userNotificationId)
    {
        await _notificationService
            .DeleteAsync(
                GetCurrentUserId(),
                userNotificationId);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    null,
                    "Notification deleted"));
    }
    [HttpPost("delete-many")]
    public async Task<IActionResult>
    DeleteMany(
        [FromBody]
        NotificationIdsDto dto)
    {
        var affectedCount =
            await _notificationService
                .DeleteManyAsync(
                    GetCurrentUserId(),
                    dto.UserNotificationIds);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    new
                    {
                        affectedCount
                    },
                    "Notifications deleted"));
    }
    [HttpGet("unread-count")]
    public async Task<IActionResult>
        GetUnreadCount()
    {
        var userId =
            GetCurrentUserId();

        var unreadCount =
            await _notificationService
                .GetUnreadCountAsync(
                    userId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                new
                {
                    unreadCount
                },
                "Unread notification count"));
    }

    [HttpPut(
        "{userNotificationId:int}/read")]
    public async Task<IActionResult>
        MarkAsRead(
            int userNotificationId)
    {
        var userId =
            GetCurrentUserId();

        await _notificationService
            .MarkAsReadAsync(
                userId,
                userNotificationId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                null,
                "Notification marked as read"));
    }
    [HttpGet]
    public async Task<IActionResult>
    GetMyNotifications(
        [FromQuery]
        NotificationQueryDto query)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _notificationService
                .GetMyNotificationsAsync(
                    userId,
                    query);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Notifications"));
    }
    [HttpPut("read-all")]
    public async Task<IActionResult>
        MarkAllAsRead()
    {
        var userId =
            GetCurrentUserId();

        await _notificationService
            .MarkAllAsReadAsync(
                userId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                null,
                "All notifications marked as read"));
    }

    private int GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            value,
            out var userId))
        {
            throw new Exception(
                "Invalid authenticated user");
        }

        return userId;
    }
}