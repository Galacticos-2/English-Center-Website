using EnglishCenterSystem.Application.Features.OnlineClassrooms.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application.Features.OnlineClassrooms.Services;

public class OnlineClassroomService(IUnitOfWork uow)
{
    public async Task<List<OnlineClassroomDto>> GetTeacherClassroomsAsync(int teacherUserId)
    {
        var classrooms = await uow.OnlineClassrooms
            .Where(x => x.TeacherUserId == teacherUserId)
            .OrderByDescending(x => x.StartsAt)
            .ToListAsync();

        return classrooms.Select(ToDto).ToList();
    }

    public async Task<List<OnlineClassroomDto>> GetStudentClassroomsAsync(int studentUserId)
    {
        var classRoomId = await uow.StudentRoles
            .Where(x => x.UserId == studentUserId && !x.IsDeleted)
            .Select(x => x.ClassRoomId)
            .SingleOrDefaultAsync();

        if (classRoomId == null)
        {
            return [];
        }

        var classrooms = await uow.OnlineClassrooms
            .Where(x => x.ClassRoomId == classRoomId.Value)
            .OrderByDescending(x => x.StartsAt)
            .ToListAsync();

        return classrooms.Select(ToDto).ToList();
    }

    public async Task<OnlineClassroomDto> CreateAsync(int teacherUserId, CreateOnlineClassroomDto dto)
    {
        await EnsureTeacherHasClassAsync(teacherUserId, dto.ClassRoomId);
        Validate(dto);

        var classroom = new OnlineClassroom
        {
            ClassRoomId = dto.ClassRoomId,
            TeacherUserId = teacherUserId,
            Title = dto.Title.Trim(),
            GoogleMeetUrl = dto.GoogleMeetUrl.Trim(),
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt
        };

        await uow.AddOnlineClassroomAsync(classroom);
        await uow.SaveChangesAsync();

        return await GetTeacherClassroomAsync(teacherUserId, classroom.OnlineClassroomId);
    }

    public async Task<OnlineClassroomDto> UpdateAsync(
        int teacherUserId,
        int onlineClassroomId,
        UpdateOnlineClassroomDto dto)
    {
        var classroom = await GetTeacherEntityAsync(teacherUserId, onlineClassroomId);
        await EnsureTeacherHasClassAsync(teacherUserId, dto.ClassRoomId);
        Validate(dto);

        classroom.ClassRoomId = dto.ClassRoomId;
        classroom.Title = dto.Title.Trim();
        classroom.GoogleMeetUrl = dto.GoogleMeetUrl.Trim();
        classroom.StartsAt = dto.StartsAt;
        classroom.EndsAt = dto.EndsAt;
        classroom.UpdatedAt = DateTime.UtcNow;

        await uow.SaveChangesAsync();
        return await GetTeacherClassroomAsync(teacherUserId, onlineClassroomId);
    }

    public async Task DeleteAsync(int teacherUserId, int onlineClassroomId)
    {
        var classroom = await GetTeacherEntityAsync(teacherUserId, onlineClassroomId);
        classroom.IsDeleted = true;
        classroom.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync();
    }

    private async Task<OnlineClassroom> GetTeacherEntityAsync(int teacherUserId, int onlineClassroomId)
    {
        var classroom = await uow.OnlineClassrooms
            .SingleOrDefaultAsync(x =>
                x.OnlineClassroomId == onlineClassroomId &&
                x.TeacherUserId == teacherUserId);

        return classroom ?? throw new Exception("Online classroom not found.");
    }

    private async Task<OnlineClassroomDto> GetTeacherClassroomAsync(int teacherUserId, int onlineClassroomId)
    {
        var classroom = await uow.OnlineClassrooms
            .Where(x => x.OnlineClassroomId == onlineClassroomId && x.TeacherUserId == teacherUserId)
            .SingleOrDefaultAsync();

        return classroom == null
            ? throw new Exception("Online classroom not found.")
            : ToDto(classroom);
    }

    private async Task EnsureTeacherHasClassAsync(int teacherUserId, int classRoomId)
    {
        var hasClass = await uow.TeacherRoles
            .Where(x => x.UserId == teacherUserId && !x.IsDeleted)
            .SelectMany(x => x.TeacherClasses)
            .AnyAsync(x => x.ClassRoomId == classRoomId);

        if (!hasClass)
        {
            throw new Exception("You are not assigned to this class.");
        }
    }

    private static void Validate(CreateOnlineClassroomDto dto)
    {
        if (dto.EndsAt <= dto.StartsAt)
        {
            throw new Exception("The end time must be after the start time.");
        }

        if (!Uri.TryCreate(dto.GoogleMeetUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "meet.google.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("A valid Google Meet URL is required.");
        }
    }

    private static OnlineClassroomDto ToDto(OnlineClassroom classroom) => new()
    {
        OnlineClassroomId = classroom.OnlineClassroomId,
        ClassRoomId = classroom.ClassRoomId,
        ClassName = classroom.ClassRoom.ClassName,
        CourseName = classroom.ClassRoom.Course.Name,
        TeacherName = string.IsNullOrWhiteSpace(classroom.TeacherUser.FullName)
            ? classroom.TeacherUser.UserName
            : classroom.TeacherUser.FullName,
        Title = classroom.Title,
        GoogleMeetUrl = classroom.GoogleMeetUrl,
        StartsAt = classroom.StartsAt,
        EndsAt = classroom.EndsAt
    };
}
