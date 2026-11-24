using Microsoft.AspNetCore.Mvc;

using EnglishCenterSystem.Application
.Features.Courses.Services;

using EnglishCenterSystem.Application
.Features.Courses.DTOs;

using EnglishCenterSystem.Application
.Common.Models;
using Microsoft.AspNetCore.Authorization;
namespace Project_Web_EnglishCenterSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CourseController : ControllerBase
{
    private readonly CourseService
        _courseService;

    public CourseController(
        CourseService courseService)
    {
        _courseService = courseService;
    }
    [HttpPost("assign-teacher")]
    public async Task<IActionResult>
AssignTeacher(
    [FromBody]
    AssignTeacherDto dto)
    {
        await _courseService
            .AssignTeacherAsync(dto);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Teacher assigned"));
    }
    [HttpGet]
    public async Task<IActionResult>
        GetCourses()
    {
        var result =
            await _courseService
            .GetCoursesAsync();

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                result,
                "Courses"));
    }

    [HttpPost]
    public async Task<IActionResult>
        CreateCourse(
            [FromBody]
            CreateCourseDto dto)
    {
        await _courseService
            .CreateCourseAsync(dto);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Course created"));
    }
    [HttpPut("{id}")]
    public async Task<IActionResult>
    UpdateCourse(
        int id,
        [FromBody]
        UpdateCourseDto dto)
    {
        await _courseService
            .UpdateCourseAsync(id, dto);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Course updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult>
        DeleteCourse(int id)
    {
        await _courseService
            .DeleteCourseAsync(id);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Course deleted"));
    }
    [HttpGet("{courseId}/classes")]
    public async Task<IActionResult>
        GetClasses(int courseId)
    {
        var result =
            await _courseService
            .GetClassRoomsAsync(courseId);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                result,
                "Classes"));
    }

    [HttpPost("{courseId}/classes")]
    public async Task<IActionResult> CreateClassRoom(
        int courseId,
        [FromBody] ClassRoomFormDto dto)
    {
        await _courseService.CreateClassRoomAsync(courseId, dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Class room created"));
    }

    [HttpPut("{courseId}/classes/{classRoomId}")]
    public async Task<IActionResult> UpdateClassRoom(
        int courseId,
        int classRoomId,
        [FromBody] ClassRoomFormDto dto)
    {
        await _courseService.UpdateClassRoomAsync(
            courseId,
            classRoomId,
            dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Class room updated"));
    }

    [HttpDelete("{courseId}/classes/{classRoomId}")]
    public async Task<IActionResult> DeleteClassRoom(
        int courseId,
        int classRoomId)
    {
        await _courseService.DeleteClassRoomAsync(
            courseId,
            classRoomId);

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Class room deleted"));
    }

    [HttpPost("teacher-class")]
    public async Task<IActionResult>
AddTeacherClass(
    [FromBody]
    AddTeacherClassDto dto)
    {
        await _courseService
            .AddTeacherClassAsync(dto);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Teacher class added"));
    }

    [HttpPut("teacher-class")]
    public async Task<IActionResult>
    UpdateTeacherClass(
        [FromBody]
    UpdateTeacherClassDto dto)
    {
        await _courseService
            .UpdateTeacherClassAsync(dto);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Teacher class updated"));
    }

    [HttpDelete("teacher-class/{userId:int}/{classRoomId:int}")]
    public async Task<IActionResult> DeleteTeacherClass(
        int userId,
        int classRoomId)
    {
        await _courseService.DeleteTeacherClassAsync(
            new DeleteTeacherClassDto
            {
                UserId = userId,
                ClassRoomId = classRoomId
            });

        return Ok(
            ApiResponse<object>.SuccessResponse(
                null,
                "Teacher class removed"));
    }

    [HttpPost("assign-student")]
    public async Task<IActionResult>
        AssignStudent(
            [FromBody]
            AssignStudentDto dto)
    {
        await _courseService
            .AssignStudentAsync(dto);

        return Ok(
            ApiResponse<object>
            .SuccessResponse(
                null,
                "Assigned"));
    }

    [HttpPost("assign-student-course")]
    public async Task<IActionResult> AssignStudentCourse(
        [FromBody] AssignStudentCourseDto dto)
    {
        await _courseService.AssignStudentCourseAsync(dto);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                null,
                "Course assigned"));
    }
}
