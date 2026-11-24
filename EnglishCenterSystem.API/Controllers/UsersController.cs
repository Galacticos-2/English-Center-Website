using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using EnglishCenterSystem.Application
.Features.Users.Services;

using EnglishCenterSystem.Application
.Features.Users.DTOs;

using EnglishCenterSystem.Application
.Common.Models;
using EnglishCenterSystem.API.Requests;

using System.Security.Claims;

namespace Project_Web_EnglishCenterSystem.Server.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _service;

    public UsersController(
        UserService service)
    {
        _service = service;
    }

    [HttpGet("students")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
       GetStudents()
    {
        var students =
            await _service
            .GetStudentsAsync();

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                students,
                "Students fetched"));
    }
    [HttpDelete("students/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
DeleteStudent(int id)
    {
        await _service
            .DeleteStudentAsync(id);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Student deleted"));
    }
    [HttpGet("teachers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
GetTeachers()
    {
        var teachers =
            await _service
            .GetTeachersAsync();

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                teachers,
                "Teachers fetched"));
    }
    [HttpPost("teachers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
    CreateTeacher(
    CreateTeacherDto dto)
    {
        await _service
            .CreateTeacherAsync(dto);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Teacher created"));
    }
    [HttpPut("teachers/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
    UpdateTeacher(
        int id,
        [FromBody]
        UpdateTeacherDto dto)
    {
        await _service
            .UpdateTeacherAsync(
                id,
                dto);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    null,
                    "Teacher updated"));
    }
    [HttpDelete("teachers/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
DeleteTeacher(int id)
    {
        await _service
            .DeleteTeacherAsync(id);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Teacher deleted"));
    }
    [HttpPost("students")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
        CreateStudent(
            CreateStudentDto dto)
    {
        await _service
            .CreateStudentAsync(dto);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Student created"));
    }

    [HttpGet("student-profile")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult>
    GetStudentProfile()
    {
        var userIdRaw =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdRaw,
            out var userId))
        {
            return Unauthorized();
        }

        var result =
            await _service
            .GetStudentProfileAsync(userId);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                result,
                "Student profile"));
    }
    [HttpGet("student-class-leaderboard")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentClassLeaderboard()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdRaw, out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.GetStudentClassLeaderboardAsync(userId);

        return Ok(ApiResponse<object>.SuccessResponse(
            result,
            "Class leaderboard"));
    }
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyInfo()
    {
        var userIdRaw =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdRaw,
            out var userId))
        {
            return Unauthorized();
        }

        var result =
            await _service
            .GetMyInfoAsync(userId);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                result,
                "My information"));
    }

    [HttpPost("me/avatar")]
    [Authorize]
    public async Task<IActionResult> UploadAvatar(
        [FromForm] UploadAvatarRequest request)
    {
        if (request.File == null || request.File.Length == 0 ||
            request.OriginalFile == null || request.OriginalFile.Length == 0)
        {
            throw new Exception("File is required");
        }

        var userIdRaw =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdRaw,
            out var userId))
        {
            return Unauthorized();
        }

        await using var stream =
            request.File.OpenReadStream();
        await using var originalStream = request.OriginalFile.OpenReadStream();

        var avatarUrl =
            await _service
            .UploadAvatarAsync(
                userId,
                stream,
                request.File.FileName,
                request.File.ContentType,
                originalStream,
                request.OriginalFile.FileName,
                request.OriginalFile.ContentType);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                new
                {
                    avatarUrl
                },
                "Upload avatar success"));
    }
    [HttpPut("students/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
    UpdateStudent(
        int id,
        [FromBody]
        UpdateStudentDto dto)
    {
        await _service
            .UpdateStudentAsync(
                id,
                dto);

        return Ok(
            ApiResponse<object>
                .SuccessResponse(
                    null,
                    "Student updated"));
    }
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
    GetUsers()
    {
        var users =
            await _service
            .GetUsersAsync();

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                users,
                "Users fetched"));
    }
}
