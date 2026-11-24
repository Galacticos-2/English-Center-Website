using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.DTOs.Auth;
using EnglishCenterSystem.Application.Interfaces;
using System.Security.Claims;
namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);

        return Ok(
            ApiResponse<LoginResponseDto>
            .SuccessResponse(
                result,
                "Login success"));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request)
    {
        await _authService.RegisterAsync(request);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Register success"));
    }
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var user = new
        {
            UserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier),

            Email = User.FindFirstValue(
                ClaimTypes.Email),

            Role = User.FindFirstValue(
                ClaimTypes.Role)
        };

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                user,
                "Current user"));
    }
}