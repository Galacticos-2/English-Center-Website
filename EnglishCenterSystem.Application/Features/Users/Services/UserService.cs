using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using EnglishCenterSystem.Application
.Features.Users.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EnglishCenterSystem.Application
.Features.Users.Services;

public class UserService
{
    private readonly IUnitOfWork _uow;
    private readonly IMinioService _minioService;
    private readonly IStudentGamificationService _studentGamificationService;

    public UserService(IUnitOfWork uow,
    IMinioService minioService,
    IStudentGamificationService studentGamificationService)
    {
        _uow = uow;
        _minioService = minioService;
        _studentGamificationService = studentGamificationService;
    }
    public async Task<List<UserDto>>
GetStudentsAsync()
    {
        var students = _uow.Users
            .Query()
            .Where(x => x.Role == UserRole.Student)
            .ToList();

        return students.Select(x =>
        {
            var classRoom =
                x.StudentRole?.ClassRoom;

            var course = x.StudentRole?.Course;

            return new UserDto
            {
                UserId = x.UserId,
                UserName = x.UserName,
                FullName = x.FullName,
                Email = x.Email,
                CreatedAt = x.CreatedAt,
                PhoneNumber = x.PhoneNumber,
                Role = x.Role.ToString(),
                IsActive = x.IsActive,

                CourseId = x.StudentRole?.CourseId,

                CourseName =
        course?.Name,

                ClassName =
        classRoom?.ClassName
            };
        }).ToList();
    }
    public async Task DeleteStudentAsync(
    int userId)
    {
        var user =
            await _uow.Users
                .GetByIdAsync(userId);

        if (user == null)
        {
            throw new Exception(
                "Không tìm thấy học viên.");
        }

        if (user.Role != UserRole.Student)
        {
            throw new Exception(
                "Tài khoản được chọn không phải học viên.");
        }

        await _uow.HardDeleteStudentAsync(
            userId);
    }
    public async Task UpdateStudentAsync(
    int userId,
    UpdateStudentDto dto)
    {
        var user =
            await _uow.Users
                .GetByIdAsync(userId);

        if (user == null)
        {
            throw new Exception(
                "Không tìm thấy học viên.");
        }

        if (
            user.Role !=
            UserRole.Student
        )
        {
            throw new Exception(
                "Tài khoản được chọn không phải học viên.");
        }

        var normalizedUserName =
            dto.UserName.Trim();

        var normalizedFullName =
            dto.FullName.Trim();

        var normalizedEmail =
            dto.Email.Trim();

        if (
            string.IsNullOrWhiteSpace(
                normalizedUserName
            )
        )
        {
            throw new Exception(
                "Tên tài khoản không được để trống.");
        }

        if (
            string.IsNullOrWhiteSpace(
                normalizedFullName
            )
        )
        {
            throw new Exception(
                "Họ và tên không được để trống.");
        }

        if (
            string.IsNullOrWhiteSpace(
                normalizedEmail
            )
        )
        {
            throw new Exception(
                "Email không được để trống.");
        }

        var duplicateEmail =
            _uow.Users
                .Query()
                .IgnoreQueryFilters()
                .Any(x =>
                    x.UserId != userId &&
                    x.Email ==
                        normalizedEmail);

        if (duplicateEmail)
        {
            throw new Exception(
                "Email đã tồn tại.");
        }

        var duplicateUserName =
            _uow.Users
                .Query()
                .IgnoreQueryFilters()
                .Any(x =>
                    x.UserId != userId &&
                    x.UserName ==
                        normalizedUserName);

        if (duplicateUserName)
        {
            throw new Exception(
                "Tên tài khoản đã tồn tại.");
        }

        user.UserName =
            normalizedUserName;

        user.FullName =
            normalizedFullName;

        user.Email =
            normalizedEmail;

        user.PhoneNumber =
            string.IsNullOrWhiteSpace(
                dto.PhoneNumber
            )
                ? null
                : dto.PhoneNumber.Trim();

        var hasNewPassword =
            !string.IsNullOrWhiteSpace(
                dto.Password
            );

        if (hasNewPassword)
        {
            if (
                dto.Password !=
                dto.ConfirmPassword
            )
            {
                throw new Exception(
                    "Mật khẩu xác nhận không khớp.");
            }

            if (
                dto.Password!.Length <
                6
            )
            {
                throw new Exception(
                    "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            user.PasswordHash =
                BCrypt.Net.BCrypt
                    .HashPassword(
                        dto.Password
                    );
        }

        await _uow.SaveChangesAsync();
    }
    public async Task<UserDto> GetMyInfoAsync(int userId)
    {
        var user = await _uow.Users.GetProfileByIdAsync(userId);

        if (user == null)
        {
            throw new Exception("User not found");
        }

        var classRoom = user.StudentRole?.ClassRoom;

        return new UserDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Birthday = user.Birthday,
            AvatarUrl = user.AvatarUrl,
            OriginalAvatarUrl = user.OriginalAvatarUrl,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,

            CourseId = user.StudentRole?.CourseId,
            CourseName = user.StudentRole?.Course?.Name,
            CourseType = user.StudentRole?.Course?.CourseType,
            ClassName = classRoom?.ClassName,
            Level = user.StudentRole?.Level,
            TargetScore = user.StudentRole?.TargetScore,

            TeacherClasses =
                user.TeacherRole?.TeacherClasses
                    .Select(tc => new TeacherClassDto
                    {
                        ClassRoomId = tc.ClassRoomId,
                        ClassName = tc.ClassRoom.ClassName,
                        CourseName = tc.ClassRoom.Course.Name
                    })
                    .ToList()
                ?? new()
        };
    }

    public async Task<string> UploadAvatarAsync(
    int userId,
    Stream avatarStream,
    string avatarFileName,
    string avatarContentType,
    Stream originalAvatarStream,
    string originalAvatarFileName,
    string originalAvatarContentType)
    {
        var user =
            await _uow.Users
                .GetByIdAsync(userId);

        if (user == null)
        {
            throw new Exception(
                "User not found");
        }

        if (avatarStream == null || originalAvatarStream == null)
        {
            throw new Exception(
                "Avatar files are required");
        }

        if (string.IsNullOrWhiteSpace(
            avatarFileName) || string.IsNullOrWhiteSpace(originalAvatarFileName))
        {
            throw new Exception(
                "Avatar file names are required");
        }

        var oldAvatarUrl =
            user.AvatarUrl;
        var oldOriginalAvatarUrl = user.OriginalAvatarUrl;

        var avatarFolderName =
            BuildAvatarFolderName(
                user.FullName,
                user.UserId);

        var safeFileName =
            Path.GetFileName(avatarFileName);

        var safeOriginalAvatarFileName =
            Path.GetFileName(originalAvatarFileName);

        var extension =
            Path.GetExtension(safeFileName)
                .ToLowerInvariant();

        var originalExtension =
            Path.GetExtension(safeOriginalAvatarFileName)
                .ToLowerInvariant();

        var avatarId =
            Guid.NewGuid()
                .ToString("N");

        var objectName =
            $"avatars/" +
            $"{avatarFolderName}/" +
            $"{avatarId}-avatar{extension}";

        var originalObjectName =
            $"avatars/" +
            $"{avatarFolderName}/" +
            $"{avatarId}-original{originalExtension}";

        string? newAvatarUrl = null;
        string? newOriginalAvatarUrl = null;

        var databaseSaved = false;

        try
        {
            newOriginalAvatarUrl =
                await _minioService
                    .UploadFileAsync(
                        originalAvatarStream,
                        originalObjectName,
                        originalAvatarContentType);

            newAvatarUrl =
                await _minioService
                    .UploadFileAsync(
                        avatarStream,
                        objectName,
                        avatarContentType);

            user.AvatarUrl =
                newAvatarUrl;
            user.OriginalAvatarUrl =
                newOriginalAvatarUrl;

            await _uow.SaveChangesAsync();

            databaseSaved = true;

            if (!string.IsNullOrWhiteSpace(
                oldAvatarUrl))
            {
                try
                {
                    await _minioService
                        .DeleteFileByUrlAsync(
                            oldAvatarUrl);
                }
                catch
                {
                    /*
                     * Avatar mới đã lưu thành công.
                     * Không làm request thất bại chỉ vì
                     * không xóa được avatar cũ.
                     */
                }
            }

            if (!string.IsNullOrWhiteSpace(oldOriginalAvatarUrl))
            {
                try
                {
                    await _minioService
                        .DeleteFileByUrlAsync(oldOriginalAvatarUrl);
                }
                catch
                {
                }
            }

            return newAvatarUrl;
        }
        catch
        {
            /*
             * Upload MinIO thành công nhưng lưu DB lỗi:
             * xóa file mới để tránh file rác.
             */
            if (!databaseSaved &&
                !string.IsNullOrWhiteSpace(
                    newAvatarUrl))
            {
                try
                {
                    await _minioService
                        .DeleteFileAsync(
                            objectName);
                }
                catch
                {
                    // Không che lỗi chính.
                }
            }

            if (!databaseSaved && !string.IsNullOrWhiteSpace(newOriginalAvatarUrl))
            {
                try
                {
                    await _minioService
                        .DeleteFileAsync(originalObjectName);
                }
                catch
                {
                }
            }

            throw;
        }
    }
    private static string BuildAvatarFolderName(
    string? fullName,
    int userId)
    {
        var sourceName =
            string.IsNullOrWhiteSpace(
                fullName)
                ? "user"
                : fullName.Trim();

        var normalized =
            sourceName.Normalize(
                NormalizationForm.FormD);

        var builder =
            new StringBuilder();

        foreach (var character in normalized)
        {
            var category =
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character);

            if (category !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var result =
            builder.ToString()
                .Normalize(
                    NormalizationForm.FormC)
                .ToLowerInvariant();

        result =
            result.Replace(
                'đ',
                'd');

        result =
            Regex.Replace(
                result,
                @"[^a-z0-9]+",
                "-");

        result =
            result.Trim('-');

        if (string.IsNullOrWhiteSpace(
            result))
        {
            result = "user";
        }

        return $"{result}-{userId}";
    }
    public async Task DeleteTeacherAsync(int userId)
    {
        var user =
            await _uow.Users
            .GetByIdAsync(userId);

        if (user == null)
        {
            throw new Exception("Teacher not found");
        }

        var teacherSections =
            await _uow.Lessons
            .GetSectionsByTeacherUserIdAsync(userId);

        foreach (var section in teacherSections)
        {
            _uow.Lessons.RemoveSection(section);
        }

        var teacherRole =
            _uow.TeacherRoleRepository
            .Query()
            .FirstOrDefault(x => x.UserId == userId);

        if (teacherRole != null)
        {
            teacherRole.TeacherClasses.Clear();

            _uow.TeacherRoleRepository.Remove(teacherRole);
        }

        user.IsActive = false;
        user.IsDeleted = true;

        await _uow.SaveChangesAsync();
    }
    public async Task UpdateTeacherAsync(
    int userId,
    UpdateTeacherDto dto)
    {
        var user =
            await _uow.Users
                .GetByIdAsync(userId);

        if (user == null)
        {
            throw new Exception("Teacher not found");
        }

        if (user.Role != UserRole.Teacher)
        {
            throw new Exception(
                "The selected account is not a teacher.");
        }

        var normalizedUserName = dto.UserName.Trim();
        var normalizedFullName = dto.FullName.Trim();
        var normalizedEmail = dto.Email.Trim();

        if (string.IsNullOrWhiteSpace(normalizedUserName))
        {
            throw new Exception("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedFullName))
        {
            throw new Exception("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new Exception("Email is required.");
        }

        var duplicateEmail = _uow.Users
            .Query()
            .IgnoreQueryFilters()
            .Any(x =>
                x.UserId != userId &&
                x.Email == normalizedEmail);

        if (duplicateEmail)
        {
            throw new Exception("Email already exists.");
        }

        var duplicateUserName = _uow.Users
            .Query()
            .IgnoreQueryFilters()
            .Any(x =>
                x.UserId != userId &&
                x.UserName == normalizedUserName);

        if (duplicateUserName)
        {
            throw new Exception("Username already exists.");
        }

        user.UserName = normalizedUserName;
        user.FullName = normalizedFullName;
        user.Email = normalizedEmail;
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber)
            ? null
            : dto.PhoneNumber.Trim();

        var hasNewPassword = !string.IsNullOrWhiteSpace(dto.Password);

        if (hasNewPassword)
        {
            if (dto.Password != dto.ConfirmPassword)
            {
                throw new Exception("Passwords do not match.");
            }

            if (dto.Password!.Length < 6)
            {
                throw new Exception(
                    "Password must be at least 6 characters.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt
                .HashPassword(dto.Password);
        }

        await _uow.SaveChangesAsync();
    }
    public async Task<List<UserDto>>
GetTeachersAsync()
    {
        var teachers =
            _uow.Users
            .Query()
            .Where(x =>
                x.Role == UserRole.Teacher)
            .ToList();

        return teachers.Select(x =>
        {
            return new UserDto
            {
                UserId = x.UserId,

                UserName = x.UserName,
                FullName = x.FullName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,

                Role = x.Role.ToString(),

                IsActive = x.IsActive,

                TeacherClasses =
                    x.TeacherRole?
                    .TeacherClasses
                    .Select(tc =>
                        new TeacherClassDto
                        {
                            ClassRoomId =
                                tc.ClassRoomId,

                            ClassName =
                                tc.ClassRoom
                                .ClassName,

                            CourseName =
                                tc.ClassRoom
                                .Course
                                .Name
                        }).ToList()

                        ?? new()
            };
        }).ToList();
    }
    public async Task<StudentProfileDto>
    GetStudentProfileAsync(int userId)
    {
        var user =
            _uow.Users
            .Query()
            .FirstOrDefault(x =>
                x.UserId == userId);

        if (user == null)
        {
            throw new Exception(
                "User not found");
        }

        var classRoom =
            user.StudentRole?.ClassRoom;
        var gamification = user.StudentRole == null
            ? new StudentGamificationSummaryDto()
            : await _studentGamificationService.GetSummaryAsync(
                user.StudentRole.StudentRoleId);
        var classLeaderboard = user.StudentRole == null
            ? new StudentClassLeaderboardDto()
            : await _studentGamificationService.GetClassLeaderboardAsync(
                user.StudentRole.StudentRoleId);
        var currentStudent = classLeaderboard.Students
            .FirstOrDefault(x => x.IsCurrentStudent);

        return new StudentProfileDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,

            HasClass =
                classRoom != null,

            CourseName =
                user.StudentRole?.Course?.Name,

            CourseType =
                user.StudentRole?.Course?.CourseType,

            ClassName =
                classRoom?.ClassName,

            TotalExp = gamification.TotalExp,
            CurrentStreakDays = gamification.CurrentStreakDays,
            ClassRank = currentStudent?.Rank,
            ClassStudentCount = classLeaderboard.Students.Count,
            UnlockedAchievementTypes = gamification.UnlockedAchievementTypes
        };
    }

    public async Task<StudentClassLeaderboardDto> GetStudentClassLeaderboardAsync(int userId)
    {
        var studentRole = await _uow.StudentRoleRepository
            .Query()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (studentRole == null)
        {
            throw new Exception("Student role not found");
        }

        return await _studentGamificationService.GetClassLeaderboardAsync(
            studentRole.StudentRoleId);
    }
    public async Task CreateStudentAsync(
    CreateStudentDto dto)
    {
        var existed =
            await _uow.Users
            .ExistsByEmailAsync(dto.Email);

        if (existed)
        {
            throw new Exception(
                "Email đã tồn tại");
        }

        var userNameExists = await _uow.Users
            .Query()
            .AnyAsync(x =>
                x.UserName.ToLower() ==
                dto.UserName.Trim().ToLower());

        if (userNameExists)
        {
            throw new Exception("Username already exists");
        }

        var user = new User
        {
            UserName = dto.UserName,
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,

            PasswordHash =
                BCrypt.Net.BCrypt
                .HashPassword(dto.Password),

            Role = UserRole.Student,

            IsActive = true
        };

        await _uow.Users.AddAsync(user);

        await _uow.SaveChangesAsync();

        var studentRole =
            new StudentRole
            {
                UserId = user.UserId
            };

        await _uow
.StudentRoleRepository
.AddAsync(studentRole);

        await _uow.SaveChangesAsync();
    }
    public async Task CreateTeacherAsync(
CreateTeacherDto dto)
    {
        var existed =
            await _uow.Users
            .ExistsByEmailAsync(dto.Email);

        if (existed)
        {
            throw new Exception(
                "Email đã tồn tại");
        }

        var user = new User
        {
            UserName = dto.UserName,
            FullName = dto.FullName,
            Email = dto.Email,

            PhoneNumber = dto.PhoneNumber,

            PasswordHash =
                BCrypt.Net.BCrypt
                .HashPassword(dto.Password),

            Role = UserRole.Teacher,

            IsActive = true
        };

        await _uow.Users.AddAsync(user);

        await _uow.SaveChangesAsync();

        var teacherRole =
    new TeacherRole
    {
        UserId = user.UserId
    };

        await _uow
        .TeacherRoleRepository
        .AddAsync(teacherRole);

        await _uow.SaveChangesAsync();
    }
    public async Task<List<UserDto>>
        GetUsersAsync()
    {
        var users = _uow.Users
    .Query()
    .ToList();

        return users.Select(x => new UserDto
        {
            UserId = x.UserId,
            UserName = x.UserName,
            Email = x.Email,
            Role = x.Role.ToString(),
            IsActive = x.IsActive
        }).ToList();
    }
}
