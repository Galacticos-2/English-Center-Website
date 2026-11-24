using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Exercises.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/exercise-progress")]
public class ExerciseProgressController : ControllerBase
{
    private readonly ExerciseProgressService _exerciseProgressService;

    public ExerciseProgressController(
        ExerciseProgressService exerciseProgressService)
    {
        _exerciseProgressService = exerciseProgressService;
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("teacher")]
    public async Task<IActionResult> GetTeacherProgress()
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _exerciseProgressService
            .GetTeacherProgressAsync(teacherUserId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Teacher exercise progress"));
    }
}