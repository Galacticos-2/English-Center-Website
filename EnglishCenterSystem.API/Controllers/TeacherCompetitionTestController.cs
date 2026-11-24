using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Competitions.DTOs;
using EnglishCenterSystem.Application.Features.Competitions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/teacher/competition-tests")]
[Authorize(Roles = "Teacher")]
public class TeacherCompetitionTestController
    : ControllerBase
{
    private readonly TeacherCompetitionTestService
        _service;

    public TeacherCompetitionTestController(
        TeacherCompetitionTestService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTests()
    {
        var result =
            await _service
                .GetMyCompetitionTestsAsync(
                    GetCurrentUserId());

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Teacher competition tests"));
    }
    [HttpGet(
    "{competitionTestId:int}/source-file")]
    public async Task<IActionResult>
    GetSourceFile(
        int competitionTestId)
    {
        var result =
            await _service
                .GetSourceFileAsync(
                    GetCurrentUserId(),
                    competitionTestId);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    result,
                    "Competition source file"));
    }
    [HttpGet("{competitionTestId:int}")]
    public async Task<IActionResult> GetById(
        int competitionTestId)
    {
        var result =
            await _service.GetByIdAsync(
                GetCurrentUserId(),
                competitionTestId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Competition test detail"));
    }

    [HttpPut("{competitionTestId:int}")]
    public async Task<IActionResult> Update(
        int competitionTestId,
        [FromBody]
        UpdateCompetitionTestDto dto)
    {
        await _service.UpdateAsync(
            GetCurrentUserId(),
            competitionTestId,
            dto);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                null,
                "Competition test updated"));
    }

    [HttpDelete("{competitionTestId:int}")]
    public async Task<IActionResult> Delete(
        int competitionTestId)
    {
        await _service.DeleteAsync(
            GetCurrentUserId(),
            competitionTestId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                null,
                "Competition test deleted"));
    }

    private int GetCurrentUserId()
    {
        return int.Parse(
            User.FindFirst(
                ClaimTypes.NameIdentifier)!
                .Value);
    }
}