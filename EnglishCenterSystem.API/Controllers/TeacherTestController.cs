using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Tests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnglishCenterSystem.Application.Features.Tests.DTOs.Teacher;
namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/teacher/tests")]
public class TeacherTestController : ControllerBase
{
    private readonly TeacherTestService _teacherTestService;

    public TeacherTestController(
        TeacherTestService teacherTestService)
    {
        _teacherTestService = teacherTestService;
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("{testId}/source-file")]
    public async Task<IActionResult> GetSourceFile(
        int testId)
    {
        var result = await _teacherTestService.GetSourceFileAsync(
            GetCurrentUserId(),
            testId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Test source file"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet("{testId}/attempts")]
    public async Task<IActionResult> GetAttempts(
        int testId)
    {
        var teacherUserId = GetCurrentUserId();

        var result = await _teacherTestService.GetAttemptsAsync(
            teacherUserId,
            testId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Teacher test attempts"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPut("{testId}")]
    public async Task<IActionResult> UpdateTest(
    int testId,
    [FromBody] UpdateTeacherTestDto dto)
    {
        var teacherUserId = GetCurrentUserId();

        await _teacherTestService.UpdateTestAsync(
            teacherUserId,
            testId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Test updated successfully"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{testId}")]
    public async Task<IActionResult> DeleteTest(
        int testId)
    {
        var teacherUserId = GetCurrentUserId();

        await _teacherTestService.DeleteTestAsync(
            teacherUserId,
            testId);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Test deleted successfully"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet("attempts/{testAttemptId}")]
    public async Task<IActionResult> GetAttemptDetail(
        int testAttemptId)
    {
        var teacherUserId = GetCurrentUserId();

        var result = await _teacherTestService.GetAttemptDetailAsync(
            teacherUserId,
            testAttemptId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Teacher test attempt detail"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet("{testId}/export")]
    public async Task<IActionResult> ExportResults(
    int testId)
    {
        var teacherUserId = GetCurrentUserId();

        var result = await _teacherTestService.ExportResultsAsync(
            teacherUserId,
            testId);

        return File(
            result.FileBytes,
            result.ContentType,
            result.FileName);
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<IActionResult> GetMyTests()
    {
        var teacherUserId = GetCurrentUserId();

        var result = await _teacherTestService.GetMyTestsAsync(
            teacherUserId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Teacher tests"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet("{testId}/analytics")]
    public async Task<IActionResult> GetAnalytics(
        int testId)
    {
        var teacherUserId = GetCurrentUserId();

        var result = await _teacherTestService.GetAnalyticsAsync(
            teacherUserId,
            testId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Teacher test analytics"));
    }

    private int GetCurrentUserId()
    {
        return int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
