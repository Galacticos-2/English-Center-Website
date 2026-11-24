using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Teacher;
using EnglishCenterSystem.Application
    .Features.Competitions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/teacher/competitions")]
[Authorize(Roles = "Teacher")]
public class TeacherCompetitionAssignmentController
    : ControllerBase
{
    private readonly
        TeacherCompetitionAssignmentService
        _service;

    public TeacherCompetitionAssignmentController(
        TeacherCompetitionAssignmentService service)
    {
        _service = service;
    }

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions()
    {
        var result =
            await _service.GetOptionsAsync(
                GetCurrentUserId());

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Competition assignment options"));
    }
    [HttpPut(
    "{competitionTestAssignmentId:int}")]
    public async Task<IActionResult> Update(
    int competitionTestAssignmentId,
    [FromBody]
    UpdateCompetitionAssignmentDto dto)
    {
        var teacherUserId =
            GetCurrentUserId();

        await _service.UpdateAsync(
            teacherUserId,
            competitionTestAssignmentId,
            dto);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    null,
                    "Competition updated"));
    }
    [HttpDelete(
    "{competitionTestAssignmentId:int}/hard-delete")]
    public async Task<IActionResult> HardDelete(
    int competitionTestAssignmentId)
    {
        var teacherUserId =
            GetCurrentUserId();

        await _service.HardDeleteAsync(
            teacherUserId,
            competitionTestAssignmentId);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    null,
                    "Competition permanently deleted"));
    }
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody]
        CreateCompetitionAssignmentDto dto)
    {
        var competitionTestAssignmentId =
            await _service.CreateAsync(
                GetCurrentUserId(),
                dto);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                new
                {
                    competitionTestAssignmentId
                },
                "Competition created successfully"));
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCompetitions()
    {
        var result =
            await _service
                .GetMyAssignmentsAsync(
                    GetCurrentUserId());

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Teacher competitions"));
    }

    [HttpGet(
        "{competitionTestAssignmentId:int}")]
    public async Task<IActionResult> GetDetail(
        int competitionTestAssignmentId)
    {
        var result =
            await _service.GetDetailAsync(
                GetCurrentUserId(),
                competitionTestAssignmentId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Competition detail"));
    }

    [HttpDelete(
        "{competitionTestAssignmentId:int}")]
    public async Task<IActionResult> Cancel(
        int competitionTestAssignmentId)
    {
        await _service.CancelAsync(
            GetCurrentUserId(),
            competitionTestAssignmentId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                null,
                "Competition cancelled"));
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