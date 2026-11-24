using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Exercises.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/exercise-structure")]
public class ExerciseStructureController : ControllerBase
{
    private readonly ExerciseStructureService _service;

    public ExerciseStructureController(
        ExerciseStructureService service)
    {
        _service = service;
    }

    [Authorize(Roles = "Teacher,Student")]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _service.GetCategoriesAsync();

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Exercise categories"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpDelete("sections/{exerciseSectionId}")]
    public async Task<IActionResult> DeleteSection(
    int exerciseSectionId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _service.DeleteSectionAsync(
            teacherUserId,
            exerciseSectionId
        );

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Delete exercise section successfully"
        ));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPut("sections/{exerciseSectionId}")]
    public async Task<IActionResult> UpdateSection(
    int exerciseSectionId,
    [FromBody] UpdateExerciseSectionDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(
                ClaimTypes.NameIdentifier
            )!.Value
        );

        await _service.UpdateSectionAsync(
            teacherUserId,
            exerciseSectionId,
            dto
        );

        return Ok(
            ApiResponse<object>.SuccessResponse(
                null,
                "Exercise section updated"
            )
        );
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet("sections")]
    public async Task<IActionResult> GetSections(
    [FromQuery] int? courseId,
    [FromQuery] int? classRoomId,
    [FromQuery] int? exerciseCategoryId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _service.GetSectionsAsync(
            teacherUserId,
            courseId,
            classRoomId,
            exerciseCategoryId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Exercise sections"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost("sections")]
    public async Task<IActionResult> CreateSection(
        [FromBody] CreateExerciseSectionDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var sectionId = await _service.CreateSectionAsync(
            teacherUserId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                exerciseSectionId = sectionId
            },
            "Exercise section created"));
    }
}