using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Lessons.DTOs;
using EnglishCenterSystem.Application.Features.Lessons.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IO;
namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LessonController : ControllerBase
{
    private readonly LessonService _lessonService;

    public LessonController(LessonService lessonService)
    {
        _lessonService = lessonService;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _lessonService.GetCategoriesAsync();

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Lesson categories"));
    }


    [Authorize(Roles = "Student")]
    [HttpGet("my-sections")]
    public async Task<IActionResult> GetMySections(
    [FromQuery] int? categoryId)
    {
        var userId = int.Parse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var result = await _lessonService.GetMySectionsAsync(
            userId,
            categoryId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "My lesson sections"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPost("sections")]
    public async Task<IActionResult> CreateSection(
    [FromBody] CreateLessonSectionDto dto)
    {
        var userIdRaw =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdRaw, out var teacherUserId))
        {
            return Unauthorized();
        }

        await _lessonService.CreateSectionAsync(
            teacherUserId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Lesson section created"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    public async Task<IActionResult> CreateLesson(
    [FromBody] CreateLessonDto dto)
    {
        var teacherUserId =
            int.Parse(
                User.FindFirst(
                    ClaimTypes.NameIdentifier)!
                    .Value);

        var lessonId =
            await _lessonService.CreateLessonAsync(
                teacherUserId,
                dto);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                new
                {
                    lessonId
                },
                "Lesson created"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet("sections")]
    public async Task<IActionResult> GetSections(
    [FromQuery] int courseId,
    [FromQuery] int classRoomId,
    [FromQuery] int? categoryId)
    {
        var userId = int.Parse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var result = await _lessonService.GetTeacherSectionsAsync(
            userId,
            courseId,
            classRoomId,
            categoryId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Teacher lesson sections"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPost("{lessonId}/pdf")]
    public async Task<IActionResult> UploadPdf(
    int lessonId,
    IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new Exception("A PDF, DOC, or DOCX file is required");
        }

        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant();

        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => throw new Exception("Only PDF, DOC, or DOCX files are allowed")
        };

        await using var stream = file.OpenReadStream();

        var teacherUserId =
            int.Parse(
                User.FindFirst(
                    ClaimTypes.NameIdentifier)!
                    .Value);

        await _lessonService.UploadDocumentAsync(
            teacherUserId,
            lessonId,
            stream,
            file.FileName,
            contentType);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Document uploaded"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet("my-classes")]
    public async Task<IActionResult> GetMyClasses()
    {
        var userId = int.Parse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var result = await _lessonService.GetTeacherClassesAsync(userId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Teacher classes"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPost("{lessonId}/video")]
    [RequestSizeLimit(1024L * 1024L * 1024L)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024L * 1024L)]
    public async Task<IActionResult> UploadVideo(
    int lessonId,
    IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new Exception("Video file is required");
        }

        await using var stream = file.OpenReadStream();

        var teacherUserId =
            int.Parse(
                User.FindFirst(
                    ClaimTypes.NameIdentifier)!
                    .Value);

        await _lessonService.UploadVideoAsync(
            teacherUserId,
            lessonId,
            stream,
            file.FileName,
            file.ContentType);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Video uploaded"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{lessonId}")]
    public async Task<IActionResult> DeleteLesson(
        int lessonId)
    {
        var teacherUserId =
            int.Parse(
                User.FindFirst(
                    ClaimTypes.NameIdentifier)!
                    .Value);

        await _lessonService.DeleteLessonAsync(
            teacherUserId,
            lessonId);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Lesson deleted"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPut("{lessonId}")]
    public async Task<IActionResult> UpdateLesson(
    int lessonId,
    [FromBody] UpdateLessonDto dto)
    {
        var teacherUserId =
            int.Parse(
                User.FindFirst(
                    ClaimTypes.NameIdentifier)!
                    .Value);

        await _lessonService.UpdateLessonAsync(
            teacherUserId,
            lessonId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Lesson updated"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("sections/{sectionId}")]
    public async Task<IActionResult> DeleteSection(
        int sectionId)
    {
        var teacherUserId =
            int.Parse(
                User.FindFirst(
                    ClaimTypes.NameIdentifier)!
                    .Value);

        await _lessonService.DeleteSectionAsync(
            teacherUserId,
            sectionId);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Lesson section deleted"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet("statistics")]
    public async Task<IActionResult> GetLessonStatistics(
    [FromQuery] int courseId,
    [FromQuery] int classRoomId)
    {
        var userIdRaw =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdRaw,
            out var teacherUserId))
        {
            return Unauthorized();
        }

        var result =
            await _lessonService
                .GetTeacherLessonStatisticsAsync(
                    teacherUserId,
                    courseId,
                    classRoomId);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                result,
                "Teacher lesson statistics"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPut("sections/{sectionId}")]
    public async Task<IActionResult> UpdateSection(
    int sectionId,
    [FromBody] UpdateLessonSectionDto dto)
    {
        var teacherUserId =
            int.Parse(
                User.FindFirst(
                    ClaimTypes.NameIdentifier)!
                    .Value);

        await _lessonService.UpdateSectionAsync(
            teacherUserId,
            sectionId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Lesson section updated"));
    }
    [Authorize(Roles = "Student")]
    [HttpPost("{lessonId}/complete")]
    public async Task<IActionResult> MarkLessonCompleted(
    int lessonId)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await _lessonService.MarkLessonCompletedAsync(
            userId,
            lessonId);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Lesson marked as completed"));
    }
}
