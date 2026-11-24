using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application
    .Features.Competitions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/teacher/competition-results")]
[Authorize(Roles = "Teacher")]
public class TeacherCompetitionResultController
    : ControllerBase
{
    private readonly TeacherCompetitionResultService
        _service;

    public TeacherCompetitionResultController(
        TeacherCompetitionResultService service)
    {
        _service = service;
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
        "attempts/{competitionTestAttemptId:int}")]
    public async Task<IActionResult> GetAttemptDetail(
        int competitionTestAttemptId)
    {
        var result =
            await _service.GetAttemptDetailAsync(
                GetCurrentUserId(),
                competitionTestAttemptId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Competition attempt detail"));
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