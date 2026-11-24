using EnglishCenterSystem.Application
    .Common.Models;

using EnglishCenterSystem.Application
    .Features.AdminDashboard.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController
    : ControllerBase
{
    private readonly AdminDashboardService
        _adminDashboardService;

    public AdminDashboardController(
        AdminDashboardService
            adminDashboardService)
    {
        _adminDashboardService =
            adminDashboardService;
    }

    [HttpGet]
    public async Task<IActionResult>
        GetDashboard(
            [FromQuery] int days = 7)
    {
        var result =
            await _adminDashboardService
                .GetDashboardAsync(days);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    result,
                    "Admin dashboard"));
    }
}
