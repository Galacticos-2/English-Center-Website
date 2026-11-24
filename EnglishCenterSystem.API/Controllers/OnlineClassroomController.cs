using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.OnlineClassrooms.DTOs;
using EnglishCenterSystem.Application.Features.OnlineClassrooms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project_Web_EnglishCenterSystem.Server.Controllers;

[ApiController]
[Route("api/online-classrooms")]
[Authorize]
public class OnlineClassroomController(OnlineClassroomService service) : ControllerBase
{
    [HttpGet("teacher")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetTeacherClassrooms() =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.GetTeacherClassroomsAsync(GetUserId()),
            "Online classrooms fetched."));

    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentClassrooms() =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.GetStudentClassroomsAsync(GetUserId()),
            "Online classrooms fetched."));

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Create(CreateOnlineClassroomDto dto) =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.CreateAsync(GetUserId(), dto),
            "Online classroom created."));

    [HttpPut("{onlineClassroomId:int}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Update(int onlineClassroomId, UpdateOnlineClassroomDto dto) =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.UpdateAsync(GetUserId(), onlineClassroomId, dto),
            "Online classroom updated."));

    [HttpDelete("{onlineClassroomId:int}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Delete(int onlineClassroomId)
    {
        await service.DeleteAsync(GetUserId(), onlineClassroomId);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Online classroom deleted."));
    }

    private int GetUserId()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdRaw, out var userId))
        {
            throw new UnauthorizedAccessException();
        }

        return userId;
    }
}
