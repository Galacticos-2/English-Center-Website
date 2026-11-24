using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application
.Features.Courses.DTOs;

using EnglishCenterSystem.Application.Interfaces;

using EnglishCenterSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;



namespace EnglishCenterSystem.Application
.Features.Courses.Services;

public class CourseService
{
    private readonly IUnitOfWork _uow;

    public CourseService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<List<CourseDto>>
    GetCoursesAsync()
    {
        var courses =
            _uow.Courses
            .Query()
            .ToList();

        return Task.FromResult(
            courses.Select(x =>
                new CourseDto
                {
                    CourseId = x.CourseId,

                    Name = x.Name,

                    Description = x.Description,

                    CourseType = x.CourseType,

                    IsActive = x.IsActive,

                    Classes =
                        x.Classes
                        .Select(c =>
                            new ClassSummaryDto
                            {
                                ClassRoomId =
                                    c.ClassRoomId,

                                ClassName =
                                    c.ClassName,

                                Capacity =
                                    c.Capacity,

                                StudentCount =
                                    c.Students.Count,

                                Schedule =
                                    c.Schedule,
                                TeacherFullName =
                                    c.TeacherClasses
    .                               FirstOrDefault()
                                    ?.TeacherRole
                                    ?.User
                                    ?.FullName,
                                Room =
                                    c.Room
                            }).ToList()
                }).ToList());
    }
    public async Task AssignTeacherAsync(
    AssignTeacherDto dto)
    {
        var teacher =
            _uow.TeacherRoleRepository
            .Query()
            .Include(x => x.TeacherClasses)
            .FirstOrDefault(x =>
                x.UserId == dto.UserId);

        if (teacher == null)
        {
            throw new Exception(
                "Teacher not found");
        }

        var classRoomIds =
            dto.ClassRoomIds
            .Distinct()
            .ToList();

        var validClassRoomIds =
            await _uow.ClassRooms
                .Query()
                .Where(x =>
                    classRoomIds.Contains(x.ClassRoomId) &&
                    !x.IsDeleted &&
                    x.IsOpen)
                .Select(x => x.ClassRoomId)
                .ToListAsync();

        var invalidClassRoomIds =
            classRoomIds
                .Except(validClassRoomIds)
                .ToList();

        if (invalidClassRoomIds.Any())
        {
            throw new Exception(
                "Class room not found or not available: " +
                string.Join(", ", invalidClassRoomIds));
        }

        teacher.TeacherClasses.Clear();

        foreach (var classRoomId in classRoomIds)
        {
            teacher.TeacherClasses
                .Add(new TeacherClass
                {
                    TeacherRoleId =
                        teacher.TeacherRoleId,

                    ClassRoomId =
                        classRoomId
                });
        }

        await _uow.SaveChangesAsync();
    }
    public async Task UpdateCourseAsync(
int id,
UpdateCourseDto dto)
    {
        var course =
            await _uow.Courses
            .GetByIdAsync(id);

        if (course == null)
        {
            throw new Exception(
                "Course not found");
        }

        course.Name = dto.Name;

        course.Description =
            dto.Description;

        course.CourseType =
            dto.CourseType;

        foreach (var item in dto.Classes)
        {
            var classRoom =
                await _uow.ClassRooms
                .GetByIdAsync(item.ClassRoomId);

            if (classRoom == null)
            {
                throw new Exception(
                    "Class room not found");
            }

            if (classRoom.CourseId != id)
            {
                throw new Exception(
                    "Class does not belong to this course");
            }

            classRoom.ClassName =
                item.ClassName;

            classRoom.Capacity =
                item.Capacity;

            classRoom.Schedule =
                item.Schedule;

            classRoom.Room =
                item.Room;
        }

        await _uow.SaveChangesAsync();
    }

    public async Task DeleteCourseAsync(
        int id)
    {
        var course =
            await _uow.Courses
            .GetByIdAsync(id);

        if (course == null)
        {
            throw new Exception(
                "Course not found");
        }

        var assignedStudents = await _uow.StudentRoleRepository
            .Query()
            .Where(x => x.CourseId == id)
            .ToListAsync();

        foreach (var student in assignedStudents)
        {
            student.CourseId = null;
        }

        _uow.Courses.Remove(course);

        await _uow.SaveChangesAsync();
    }
    public async Task CreateCourseAsync(
        CreateCourseDto dto)
    {
        if (dto.Classes.Count != dto.NumberOfClasses)
        {
            throw new Exception(
                "Number of classes does not match class list");
        }

        var course = new Course
        {
            Name = dto.Name,
            Description = dto.Description,
            CourseType = dto.CourseType,
            IsActive = true
        };

        await _uow.Courses.AddAsync(course);

        await _uow.SaveChangesAsync();

        foreach (var item in dto.Classes)
        {
            var classRoom =
                new ClassRoom
                {
                    CourseId = course.CourseId,
                    ClassName = item.ClassName,
                    Capacity = item.Capacity,
                    Schedule = item.Schedule,
                    Room = item.Room,
                    IsOpen = true
                };

            await _uow.ClassRooms
    .AddAsync(classRoom);
        

    }

        await _uow.SaveChangesAsync();
    }

    
    public Task<List<ClassRoomDto>>
GetClassRoomsAsync(int courseId)
    {
        var classes =
            _uow.ClassRooms
            .Query()
            .Where(x =>
                x.CourseId == courseId)
            .ToList();

        return Task.FromResult(
            classes.Select(x =>
                new ClassRoomDto
                {
                    ClassRoomId = x.ClassRoomId,
                    ClassName = x.ClassName,
                    Capacity = x.Capacity,
                    Schedule = x.Schedule,
                    Room = x.Room
                }).ToList());
    }

    public async Task CreateClassRoomAsync(
        int courseId,
        ClassRoomFormDto dto)
    {
        var course = await _uow.Courses
            .GetByIdAsync(courseId);

        if (course == null)
        {
            throw new Exception("Course not found");
        }

        ValidateClassRoom(dto);

        await _uow.ClassRooms.AddAsync(new ClassRoom
        {
            CourseId = courseId,
            ClassName = dto.ClassName.Trim(),
            Capacity = dto.Capacity,
            Schedule = dto.Schedule.Trim(),
            Room = dto.Room.Trim(),
            IsOpen = true
        });

        await _uow.SaveChangesAsync();
    }

    public async Task UpdateClassRoomAsync(
        int courseId,
        int classRoomId,
        ClassRoomFormDto dto)
    {
        var classRoom = await _uow.ClassRooms
            .GetByIdAsync(classRoomId);

        if (classRoom == null || classRoom.CourseId != courseId)
        {
            throw new Exception("Class room not found");
        }

        ValidateClassRoom(dto);

        classRoom.ClassName = dto.ClassName.Trim();
        classRoom.Capacity = dto.Capacity;
        classRoom.Schedule = dto.Schedule.Trim();
        classRoom.Room = dto.Room.Trim();

        await _uow.SaveChangesAsync();
    }

    public async Task DeleteClassRoomAsync(
        int courseId,
        int classRoomId)
    {
        var classRoom = await _uow.ClassRooms
            .GetByIdAsync(classRoomId);

        if (classRoom == null || classRoom.CourseId != courseId)
        {
            throw new Exception("Class room not found");
        }

        classRoom.IsDeleted = true;
        classRoom.IsOpen = false;

        await _uow.SaveChangesAsync();
    }

    public async Task AssignStudentAsync(
    AssignStudentDto dto)
    {
        var student =
            _uow.StudentRoleRepository
.Query()
            .FirstOrDefault(x =>
                x.UserId == dto.UserId);

        if (student == null)
        {
            throw new Exception(
                "Student not found");
        }

        if (student.ClassRoomId != null)
        {
            throw new Exception(
                "Student already has class");
        }

        if (student.CourseId == null)
        {
            throw new Exception(
                "Please assign a course before assigning a class");
        }

        var classRoom =
            await _uow.ClassRooms
                .Query()
                .Include(x => x.Students)
                .FirstOrDefaultAsync(x =>
                    x.ClassRoomId == dto.ClassRoomId &&
                    x.CourseId == student.CourseId &&
                    !x.IsDeleted &&
                    x.IsOpen);

        if (classRoom == null)
        {
            throw new Exception(
                "Class room not found or not available");
        }

        if (classRoom.Students.Count(x => !x.IsDeleted) >= classRoom.Capacity)
        {
            throw new Exception(
                "Class room has reached its capacity");
        }

        student.ClassRoomId =
            dto.ClassRoomId;

        await _uow.SaveChangesAsync();
    }
    public async Task AddTeacherClassAsync(
    AddTeacherClassDto dto)
    {
        var teacher =
            _uow.TeacherRoleRepository
            .Query()
            .Include(x => x.TeacherClasses)
            .FirstOrDefault(x =>
                x.UserId == dto.UserId);

        if (teacher == null)
        {
            throw new Exception(
                "Teacher not found");
        }

        var existed =
            teacher.TeacherClasses.Any(x =>
                x.ClassRoomId == dto.ClassRoomId);

        if (existed)
        {
            throw new Exception(
                "Giáo viên đã được phân lớp này");
        }

        await EnsureClassRoomAvailableAsync(
            dto.ClassRoomId);

        teacher.TeacherClasses.Add(
            new TeacherClass
            {
                TeacherRoleId =
                    teacher.TeacherRoleId,

                ClassRoomId =
                    dto.ClassRoomId
            });

        await _uow.SaveChangesAsync();
    }

    public async Task UpdateTeacherClassAsync(
        UpdateTeacherClassDto dto)
    {
        var teacher =
            _uow.TeacherRoleRepository
            .Query()
            .Include(x => x.TeacherClasses)
            .FirstOrDefault(x =>
                x.UserId == dto.UserId);

        if (teacher == null)
        {
            throw new Exception(
                "Teacher not found");
        }

        var teacherClass =
            teacher.TeacherClasses
            .FirstOrDefault(x =>
                x.ClassRoomId == dto.OldClassRoomId);

        if (teacherClass == null)
        {
            throw new Exception(
                "Teacher class not found");
        }

        var duplicated =
            teacher.TeacherClasses.Any(x =>
                x.ClassRoomId == dto.NewClassRoomId
                && x.ClassRoomId != dto.OldClassRoomId);

        if (duplicated)
        {
            throw new Exception(
                "Giáo viên đã có lớp này");
        }

        await EnsureClassRoomAvailableAsync(
            dto.NewClassRoomId);

        teacherClass.ClassRoomId =
            dto.NewClassRoomId;

        await _uow.SaveChangesAsync();
    }

    public async Task DeleteTeacherClassAsync(
        DeleteTeacherClassDto dto)
    {
        var teacher = _uow.TeacherRoleRepository
            .Query()
            .Include(x => x.TeacherClasses)
            .FirstOrDefault(x => x.UserId == dto.UserId);

        if (teacher == null)
        {
            throw new Exception("Teacher not found");
        }

        var teacherClass = teacher.TeacherClasses
            .FirstOrDefault(x =>
                x.ClassRoomId == dto.ClassRoomId);

        if (teacherClass == null)
        {
            throw new Exception("Teacher class not found");
        }

        teacher.TeacherClasses.Remove(teacherClass);

        await _uow.SaveChangesAsync();
    }

    public async Task AssignStudentCourseAsync(
        AssignStudentCourseDto dto)
    {
        var student = await _uow.StudentRoleRepository
            .Query()
            .FirstOrDefaultAsync(x => x.UserId == dto.UserId);

        if (student == null)
        {
            throw new Exception("Student not found");
        }

        if (student.CourseId != null)
        {
            throw new Exception("Student already has course");
        }

        var course = await _uow.Courses
            .Query()
            .FirstOrDefaultAsync(x =>
                x.CourseId == dto.CourseId && x.IsActive);

        if (course == null)
        {
            throw new Exception("Course not found or not available");
        }

        student.CourseId = dto.CourseId;

        await _uow.SaveChangesAsync();
    }

    private async Task EnsureClassRoomAvailableAsync(
        int classRoomId)
    {
        var exists =
            await _uow.ClassRooms
                .Query()
                .AnyAsync(x =>
                    x.ClassRoomId == classRoomId &&
                    !x.IsDeleted &&
                    x.IsOpen);

        if (!exists)
        {
            throw new Exception(
                "Class room not found or not available");
        }
    }

    private static void ValidateClassRoom(
        ClassRoomFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ClassName) ||
            string.IsNullOrWhiteSpace(dto.Schedule) ||
            string.IsNullOrWhiteSpace(dto.Room) ||
            dto.Capacity <= 0)
        {
            throw new Exception("Class room information is invalid");
        }
    }
}
