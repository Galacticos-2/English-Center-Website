using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Exercises.Services;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using EnglishCenterSystem.Infrastructure.BackgroundJobs;
using EnglishCenterSystem.API.Requests;
namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/student/exercises")]
public class StudentExerciseController : ControllerBase
{
    private readonly StudentExerciseService _studentExerciseService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    public StudentExerciseController(
        StudentExerciseService studentExerciseService,
         IBackgroundJobClient backgroundJobClient)
    {
        _studentExerciseService = studentExerciseService;
        _backgroundJobClient = backgroundJobClient;
    }

    [Authorize(Roles = "Student")]
    [HttpGet]
    public async Task<IActionResult> GetMyExercises()
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentExerciseService
            .GetMyExercisesAsync(studentUserId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Student exercises"));
    }
    [Authorize(Roles = "Student")]
    [HttpGet("skill-progress")]
    public async Task<IActionResult> GetMySkillProgress()
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentExerciseService
            .GetMySkillProgressAsync(studentUserId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Student exercise skill progress"));
    }
    [Authorize(Roles = "Student")]
    [HttpPost("speaking-answer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitSpeakingAnswer(
    [FromForm] SubmitSpeakingAnswerRequest request)
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        if (request.AudioFile == null ||
            request.AudioFile.Length == 0)
        {
            return BadRequest(ApiResponse<object>.FailResponse(
                "Audio file is required"));
        }

        using var stream = request.AudioFile.OpenReadStream();

        var result = await _studentExerciseService
            .SubmitSpeakingAnswerAsync(
                studentUserId,
                request.ExerciseAttemptId,
                request.SpeakingTaskId,
                stream,
                request.AudioFile.FileName,
                request.AudioFile.ContentType);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Speaking answer submitted"));
    }
    [Authorize(Roles = "Student")]
    [HttpGet("sections")]
    public async Task<IActionResult> GetMyExerciseSections(
    [FromQuery] SkillType? skill)
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentExerciseService
            .GetMyExerciseSectionsAsync(
                studentUserId,
                skill);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Student exercise sections"));
    }
    [Authorize(Roles = "Student")]
    [HttpGet("{exerciseAttemptId}/result")]
    public async Task<IActionResult> GetAttemptResult(
    int exerciseAttemptId)
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentExerciseService
            .GetAttemptResultAsync(
                studentUserId,
                exerciseAttemptId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Exercise attempt result"));
    }
    [Authorize(Roles = "Student")]
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitAttempt(
    [FromBody] SubmitExerciseAttemptDto dto)
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentExerciseService
            .SubmitAttemptAsync(studentUserId, dto);
        foreach (var writingSubmissionId in result.PendingWritingSubmissionIds)
        {
            _backgroundJobClient.Enqueue<
                WritingGradingBackgroundJobService>(
                service => service.ProcessAsync(
                    writingSubmissionId));
        }

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Exercise attempt submitted"));
    }
    [Authorize(Roles = "Student")]
    [HttpGet("history")]
    public async Task<IActionResult> GetMyHistory()
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentExerciseService
            .GetMyHistoryAsync(studentUserId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Student exercise history"));
    }
    [Authorize(Roles = "Student")]
    [HttpGet("progress")]
    public async Task<IActionResult> GetMyProgress()
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentExerciseService
            .GetMyProgressAsync(studentUserId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Student exercise progress"));
    }
    [Authorize(Roles = "Student")]
    [HttpPost("start")]
    public async Task<IActionResult> StartAttempt(
        [FromBody] StartExerciseAttemptDto dto)
    {
        var studentUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _studentExerciseService
            .StartAttemptAsync(studentUserId, dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Exercise attempt started"));
    }
}