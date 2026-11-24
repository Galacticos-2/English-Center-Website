using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Exercises.Services;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExerciseController : ControllerBase
{
    private readonly ExerciseService _exerciseService;

    public ExerciseController(ExerciseService exerciseService)
    {
        _exerciseService = exerciseService;
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<IActionResult> GetExercises(
        [FromQuery] CourseType? courseType,
        [FromQuery] SkillType? skill
        
        )
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _exerciseService.GetExercisesAsync(
    teacherUserId,
    courseType,
    skill);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Exercises"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("{exerciseId}/source-file")]
    public async Task<IActionResult> GetSourceFile(
        int exerciseId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _exerciseService.GetSourceFileAsync(
            teacherUserId,
            exerciseId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Exercise source file"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("{exerciseId}")]
    public async Task<IActionResult> GetExerciseDetail(
        int exerciseId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _exerciseService.GetExerciseDetailAsync(
            teacherUserId,
            exerciseId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Exercise detail"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    public async Task<IActionResult> CreateExercise(
        [FromBody] CreateExerciseDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var exerciseId = await _exerciseService.CreateExerciseAsync(
            teacherUserId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                exerciseId
            },
            "Exercise created"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPut("{exerciseId}")]
    public async Task<IActionResult> UpdateExercise(
        int exerciseId,
        [FromBody] UpdateExerciseDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _exerciseService.UpdateExerciseAsync(
            teacherUserId,
            exerciseId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Exercise updated"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPut("{exerciseId}/metadata")]
    public async Task<IActionResult>
    UpdateExerciseMetadata(
        int exerciseId,
        [FromBody]
        UpdateExerciseMetadataDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(
                ClaimTypes.NameIdentifier)!
                .Value);

        await _exerciseService
            .UpdateExerciseMetadataAsync(
                teacherUserId,
                exerciseId,
                dto);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    null,
                    "Exercise updated"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpDelete("{exerciseId}")]
    public async Task<IActionResult> DeleteExercise(
        int exerciseId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _exerciseService.DeleteExerciseAsync(
            teacherUserId,
            exerciseId);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Exercise deleted"));
    }
}
