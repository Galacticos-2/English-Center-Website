
using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Exercises.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExerciseAssignmentController : ControllerBase
{
    private readonly ExerciseAssignmentService
        _exerciseAssignmentService;

    public ExerciseAssignmentController(
        ExerciseAssignmentService exerciseAssignmentService)
    {
        _exerciseAssignmentService = exerciseAssignmentService;
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<IActionResult> GetAssignments()
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result =
            await _exerciseAssignmentService.GetAssignmentsAsync(
                teacherUserId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Exercise assignments"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    public async Task<IActionResult> CreateAssignment(
        [FromBody] CreateExerciseAssignmentDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _exerciseAssignmentService.CreateAssignmentAsync(
            teacherUserId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Exercise assigned to class"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{exerciseAssignmentId}")]
    public async Task<IActionResult> DeleteAssignment(
        int exerciseAssignmentId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _exerciseAssignmentService.DeleteAssignmentAsync(
            teacherUserId,
            exerciseAssignmentId);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Exercise assignment deleted"));
    }
}