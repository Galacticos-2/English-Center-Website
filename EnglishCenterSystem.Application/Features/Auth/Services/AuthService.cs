using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.DTOs.Auth;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IJwtService _jwtService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request)
    {
        var user = await _unitOfWork.Users
            .GetByUserNameAsync(request.UserName);

        if (user == null)
        {
            throw new Exception("Username does not exist");
        }

        var isCorrectPassword =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

        if (!isCorrectPassword)
        {
            throw new Exception("Wrong password");
        }

        var token = _jwtService.GenerateToken(user);

        return new LoginResponseDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Token = token
        };
    }

    public async Task RegisterAsync(
        RegisterRequestDto request)
    {
        var isEmailExists = await _unitOfWork.Users
            .ExistsByEmailAsync(request.Email);

        if (isEmailExists)
        {
            throw new Exception("Email already exists");
        }

        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt
                .HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber,
            Birthday = request.Birthday,
            Role = Enum.Parse<UserRole>(request.Role)
        };

        await _unitOfWork.Users.AddAsync(user);

        await _unitOfWork.SaveChangesAsync();
    }
}
