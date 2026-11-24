using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Exercises.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/exercise-results")]
public class ExerciseResultController : ControllerBase
{
    private readonly ExerciseResultService _exerciseResultService;

    public ExerciseResultController(
        ExerciseResultService exerciseResultService)
    {
        _exerciseResultService = exerciseResultService;
    }
    [Authorize(Roles = "Teacher")]
    [HttpPost("grade")]
    public async Task<IActionResult> GradeAttempt(
    [FromBody] GradeExerciseAttemptDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _exerciseResultService.GradeAttemptAsync(
            teacherUserId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Exercise attempt graded"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<IActionResult> GetTeacherResults(
        [FromQuery] int? exerciseAssignmentId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _exerciseResultService
            .GetTeacherResultsAsync(
                teacherUserId,
                exerciseAssignmentId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Exercise results"));
    }
}