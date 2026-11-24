using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Exercises.Services;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionController : ControllerBase
{
    private readonly QuestionService _questionService;

    public QuestionController(QuestionService questionService)
    {
        _questionService = questionService;
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] int? categoryId,
        [FromQuery] QuestionType? questionType,
        [FromQuery] CourseType? courseType,
        [FromQuery] SkillType? skill)
        => Ok(ApiResponse<object>.SuccessResponse(
            await _questionService.GetQuestionsAsync(GetUserId(), categoryId,
                questionType, courseType, skill), "Questions"));

    [Authorize(Roles = "Teacher")]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
        => Ok(ApiResponse<object>.SuccessResponse(
            await _questionService.GetCategoriesAsync(), "Question categories"));

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDto dto)
        => Ok(ApiResponse<object>.SuccessResponse(new
        {
            questionId = await _questionService.CreateQuestionAsync(GetUserId(), dto)
        }, "Question created"));

    [Authorize(Roles = "Teacher")]
    [HttpPut("{questionId}")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] UpdateQuestionDto dto)
    {
        await _questionService.UpdateQuestionAsync(GetUserId(), questionId, dto);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Question updated"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{questionId}")]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        await _questionService.DeleteQuestionAsync(GetUserId(), questionId);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Question deleted"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments()
        => Ok(ApiResponse<object>.SuccessResponse(
            await _questionService.GetAssignmentsAsync(GetUserId()), "Question assignments"));

    [Authorize(Roles = "Teacher")]
    [HttpPost("assignments")]
    public async Task<IActionResult> AssignQuestions([FromBody] CreateQuestionAssignmentDto dto)
    {
        await _questionService.AssignQuestionsAsync(GetUserId(), dto);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Questions assigned"));
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("assignments/{questionAssignmentId}")]
    public async Task<IActionResult> RemoveAssignment(int questionAssignmentId)
    {
        await _questionService.RemoveAssignmentAsync(GetUserId(), questionAssignmentId);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Question assignment removed"));
    }

    private int GetUserId() => int.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
