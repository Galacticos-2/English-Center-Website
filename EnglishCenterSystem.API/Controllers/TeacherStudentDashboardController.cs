using System.Security.Claims;

using EnglishCenterSystem.Application
    .Common.Models;

using EnglishCenterSystem.Application
    .Features.TeacherStudents.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/teacher/student-dashboard")]
[Authorize(Roles = "Teacher")]
public class TeacherStudentDashboardController
    : ControllerBase
{
    private readonly
        TeacherStudentDashboardService
        _service;

    public TeacherStudentDashboardController(
        TeacherStudentDashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int? classRoomId,
        [FromQuery] int? courseId,
        [FromQuery] string? search,
        [FromQuery] string? sortBy)
    {
        var teacherUserId =
            int.Parse(
                User.FindFirst(
                    ClaimTypes.NameIdentifier)!
                    .Value);

        var result =
            await _service.GetDashboardAsync(
                teacherUserId,
                classRoomId,
                courseId,
                search,
                sortBy);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    result,
                    "Teacher student dashboard"));
    }
}