using System.Security.Claims;
using EnglishCenterSystem.API.Requests;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Student;
using EnglishCenterSystem.Application
    .Features.Competitions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/student/competitions")]
[Authorize(Roles = "Student")]
public class StudentCompetitionController
    : ControllerBase
{
    private readonly StudentCompetitionService
        _service;

    public StudentCompetitionController(
        StudentCompetitionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult>
        GetMyCompetitions()
    {
        var result =
            await _service
                .GetMyCompetitionsAsync(
                    GetCurrentUserId());

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Student competitions"));
    }

    [HttpPost(
        "{competitionTestAssignmentId:int}/start")]
    public async Task<IActionResult> Start(
        int competitionTestAssignmentId)
    {
        var result =
            await _service.StartAsync(
                GetCurrentUserId(),
                competitionTestAssignmentId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Competition attempt started"));
    }

    [HttpPost("speaking-answer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult>
        SubmitSpeakingAnswer(
            [FromForm]
            SubmitCompetitionSpeakingAnswerRequest request)
    {
        if (request.AudioFile == null ||
            request.AudioFile.Length == 0)
        {
            return BadRequest(
                ApiResponse<object>.FailResponse(
                    "Audio file is required"));
        }

        await using var stream =
            request.AudioFile.OpenReadStream();

        var result =
            await _service
                .SubmitSpeakingAnswerAsync(
                    GetCurrentUserId(),
                    request
                        .CompetitionTestAttemptId,
                    request.SpeakingTaskId,
                    stream,
                    request.AudioFile.FileName,
                    request.AudioFile.ContentType);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Competition speaking answer saved"));
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit(
        [FromBody]
        SubmitCompetitionAttemptDto dto)
    {
        var result =
            await _service.SubmitAsync(
                GetCurrentUserId(),
                dto);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Competition attempt submitted"));
    }
    [HttpGet(
    "{competitionTestAssignmentId:int}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(
    int competitionTestAssignmentId)
    {
        var result =
            await _service.GetLeaderboardAsync(
                GetCurrentUserId(),
                competitionTestAssignmentId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Competition leaderboard"));
    }
    [HttpGet(
        "attempts/{competitionTestAttemptId:int}/result")]
    public async Task<IActionResult> GetResult(
        int competitionTestAttemptId)
    {
        var result =
            await _service.GetMyResultAsync(
                GetCurrentUserId(),
                competitionTestAttemptId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Competition result"));
    }

    private int GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            value,
            out var userId))
        {
            throw new Exception(
                "Invalid authenticated user");
        }

        return userId;
    }
}