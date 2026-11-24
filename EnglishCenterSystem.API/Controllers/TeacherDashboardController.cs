using System.Security.Claims;
using EnglishCenterSystem.Application
    .Common.Models;
using EnglishCenterSystem.Application
    .Features.TeacherDashboard.DTOs;
using EnglishCenterSystem.Application
    .Features.TeacherDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/teacher/dashboard")]
[Authorize(Roles = "Teacher")]
public class TeacherDashboardController : ControllerBase
{
    private readonly TeacherDashboardService
        _teacherDashboardService;

    public TeacherDashboardController(
        TeacherDashboardService teacherDashboardService)
    {
        _teacherDashboardService =
            teacherDashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int days = 7)
    {
        var teacherUserId = GetCurrentUserId();

        var result =
            await _teacherDashboardService
                .GetDashboardAsync(
                    teacherUserId,
                    days);

        return Ok(new ApiResponse<TeacherDashboardDto>
        {
            Success = true,
            Message =
                "Teacher dashboard loaded successfully",
            Data = result
        });
    }

    private int GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)
            ??
            User.FindFirstValue("userId")
            ??
            User.FindFirstValue("sub");

        if (!int.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Invalid user identity");
        }

        return userId;
    }
}