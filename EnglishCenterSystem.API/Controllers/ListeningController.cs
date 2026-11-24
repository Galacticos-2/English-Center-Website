using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Listening.DTOs;
using EnglishCenterSystem.Application.Features.Listening.Services;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/listening")]
public class ListeningController : ControllerBase
{
    private readonly ListeningService _listeningService;

    public ListeningController(ListeningService listeningService)
    {
        _listeningService = listeningService;
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<IActionResult> GetListeningTasks(
        [FromQuery] CourseType? courseType
        )
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _listeningService.GetListeningTasksAsync(
            teacherUserId,
            courseType
            );

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Listening tasks"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("{listeningTaskId}")]
    public async Task<IActionResult> GetListeningTaskDetail(
        int listeningTaskId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result =
            await _listeningService.GetListeningTaskDetailAsync(
                teacherUserId,
                listeningTaskId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Listening task detail"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    public async Task<IActionResult> CreateListeningTask(
        [FromBody] CreateListeningTaskDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var listeningTaskId =
            await _listeningService.CreateListeningTaskAsync(
                teacherUserId,
                dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                listeningTaskId
            },
            "Listening task created"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPut("{listeningTaskId}")]
    public async Task<IActionResult> UpdateListeningTask(
        int listeningTaskId,
        [FromBody] UpdateListeningTaskDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _listeningService.UpdateListeningTaskAsync(
            teacherUserId,
            listeningTaskId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Listening task updated"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{listeningTaskId}")]
    public async Task<IActionResult> DeleteListeningTask(
        int listeningTaskId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _listeningService.DeleteListeningTaskAsync(
            teacherUserId,
            listeningTaskId);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Listening task deleted"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost("{listeningTaskId}/audio")]
    public async Task<IActionResult> UploadAudio(
        int listeningTaskId,
        IFormFile file)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        using var stream = file.OpenReadStream();

        await _listeningService.UploadAudioAsync(
            teacherUserId,
            listeningTaskId,
            stream,
            file.FileName,
            file.ContentType);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Listening audio uploaded"));
    }
}