using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Exercises.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/student/questions")]
public class StudentQuestionController : ControllerBase
{
    private readonly QuestionService _questionService;

    public StudentQuestionController(QuestionService questionService)
    {
        _questionService = questionService;
    }

    [Authorize(Roles = "Student")]
    [HttpGet]
    public async Task<IActionResult> GetQuestions()
        => Ok(ApiResponse<object>.SuccessResponse(
            await _questionService.GetStudentQuestionsAsync(GetUserId()),
            "Assigned questions"));

    [Authorize(Roles = "Student")]
    [HttpPost("{questionId}/check")]
    public async Task<IActionResult> CheckAnswer(
        int questionId,
        [FromBody] SubmitQuestionAnswerDto dto)
        => Ok(ApiResponse<object>.SuccessResponse(
            await _questionService.CheckStudentAnswerAsync(GetUserId(), questionId, dto),
            "Answer checked"));

    private int GetUserId() => int.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
