using System.Security.Claims;
using EnglishCenterSystem.API.Requests;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Application.Features.Tests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/student/tests")]
public class StudentTestController : ControllerBase
{
    private readonly StudentTestService _studentTestService;

    public StudentTestController(
        StudentTestService studentTestService)
    {
        _studentTestService = studentTestService;
    }

    [Authorize(Roles = "Student")]
    [HttpGet]
    public async Task<IActionResult> GetMyTests()
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentTestService
            .GetMyTestsAsync(studentUserId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Student tests"));
    }

    [Authorize(Roles = "Student")]
    [HttpPost("start")]
    public async Task<IActionResult> StartAttempt(
        [FromBody] StartTestAttemptDto dto)
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentTestService
            .StartAttemptAsync(studentUserId, dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Test attempt started"));
    }

    [Authorize(Roles = "Student")]
    [HttpPost("speaking-answer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitSpeakingAnswer(
        [FromForm] SubmitTestSpeakingAnswerRequest request)
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        if (request.AudioFile == null ||
            request.AudioFile.Length == 0)
        {
            return BadRequest(ApiResponse<object>.FailResponse(
                "Audio file is required"));
        }

        using var stream =
            request.AudioFile.OpenReadStream();

        var result = await _studentTestService
            .SubmitSpeakingAnswerAsync(
                studentUserId,
                request.TestAttemptId,
                request.SpeakingTaskId,
                stream,
                request.AudioFile.FileName,
                request.AudioFile.ContentType);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Speaking answer submitted"));
    }

    [Authorize(Roles = "Student")]
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitAttempt(
        [FromBody] SubmitTestAttemptDto dto)
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentTestService
            .SubmitAttemptAsync(studentUserId, dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Test attempt submitted"));
    }

    [Authorize(Roles = "Student")]
    [HttpGet("history")]
    public async Task<IActionResult> GetMyHistory()
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentTestService
            .GetMyHistoryAsync(studentUserId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Student test history"));
    }

    [Authorize(Roles = "Student")]
    [HttpGet("{testAttemptId}/result")]
    public async Task<IActionResult> GetAttemptResult(
        int testAttemptId)
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentTestService
            .GetAttemptResultAsync(
                studentUserId,
                testAttemptId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Test attempt result"));
    }
}