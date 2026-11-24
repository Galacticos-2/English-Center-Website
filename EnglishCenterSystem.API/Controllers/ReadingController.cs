using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Reading.DTOs;
using EnglishCenterSystem.Application.Features.Reading.Services;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/reading")]
public class ReadingController : ControllerBase
{
    private readonly ReadingService _readingService;

    public ReadingController(ReadingService readingService)
    {
        _readingService = readingService;
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<IActionResult> GetReadingTasks(
        [FromQuery] CourseType? courseType)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _readingService
            .GetReadingTasksAsync(
                teacherUserId,
                courseType);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Reading tasks"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("{readingTaskId}")]
    public async Task<IActionResult> GetReadingTaskDetail(
        int readingTaskId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _readingService
            .GetReadingTaskDetailAsync(
                teacherUserId,
                readingTaskId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Reading task detail"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    public async Task<IActionResult> CreateReadingTask(
        [FromBody] CreateReadingTaskDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var readingTaskId = await _readingService
            .CreateReadingTaskAsync(
                teacherUserId,
                dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                readingTaskId
            },
            "Reading task created"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPut("{readingTaskId}")]
    public async Task<IActionResult> UpdateReadingTask(
        int readingTaskId,
        [FromBody] UpdateReadingTaskDto dto)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _readingService.UpdateReadingTaskAsync(
            teacherUserId,
            readingTaskId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Reading task updated"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{readingTaskId}")]
    public async Task<IActionResult> DeleteReadingTask(
        int readingTaskId)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _readingService.DeleteReadingTaskAsync(
            teacherUserId,
            readingTaskId);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Reading task deleted"));
    }
}