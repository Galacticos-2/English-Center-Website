using System.Security.Claims;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Flashcards.DTOs;
using EnglishCenterSystem.Application.Features.Flashcards.Services;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.API.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/flashcards")]
[Authorize]
public class FlashcardController(FlashcardService service) : ControllerBase
{
    [HttpGet("teacher")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetTeacherFlashcards() =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.GetTeacherFlashcardsAsync(GetUserId()), "Flashcards fetched."));

    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentFlashcards() =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.GetStudentFlashcardsAsync(GetUserId()), "Flashcards fetched."));

    [HttpGet("student/progress")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentStudyProgress() =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.GetStudentStudyProgressAsync(GetUserId()), "Flashcard progress fetched."));

    [HttpGet("student/study-session/{mode}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentStudySession(FlashcardStudyMode mode) =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.GetStudentStudySessionAsync(GetUserId(), mode), "Flashcard study session fetched."));

    [HttpPost("student/progress/{mode}/flashcards/{flashcardId:int}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CompleteStudentFlashcard(
        FlashcardStudyMode mode,
        int flashcardId) =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.CompleteStudentFlashcardAsync(GetUserId(), mode, flashcardId),
            "Flashcard progress saved."));

    [HttpDelete("student/progress/{mode}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> ResetStudentStudyProgress(FlashcardStudyMode mode) =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.ResetStudentStudyProgressAsync(GetUserId(), mode),
            "Flashcard progress reset."));

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Create(SaveFlashcardDto dto) =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.CreateAsync(GetUserId(), dto), "Flashcard created."));

    [HttpPost("import/preview")]
    [Authorize(Roles = "Teacher")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PreviewImport([FromForm] ImportFlashcardRequest request)
    {
        if (request.File == null)
        {
            throw new Exception("Select an Excel file to import.");
        }

        await using var stream = request.File.OpenReadStream();
        return Ok(ApiResponse<object>.SuccessResponse(
            await service.PreviewImportAsync(GetUserId(), request.ClassRoomId, stream, request.File.FileName),
            "Flashcard import preview generated."));
    }

    [HttpPost("import")]
    [Authorize(Roles = "Teacher")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import([FromForm] ImportFlashcardRequest request)
    {
        if (request.File == null)
        {
            throw new Exception("Select an Excel file to import.");
        }

        await using var stream = request.File.OpenReadStream();
        return Ok(ApiResponse<object>.SuccessResponse(
            await service.ImportAsync(GetUserId(), request.ClassRoomId, stream, request.File.FileName),
            "Flashcards imported."));
    }

    [HttpPut("{flashcardId:int}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Update(int flashcardId, SaveFlashcardDto dto) =>
        Ok(ApiResponse<object>.SuccessResponse(
            await service.UpdateAsync(GetUserId(), flashcardId, dto), "Flashcard updated."));

    [HttpDelete("bulk")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteFlashcardsDto dto) =>
        Ok(ApiResponse<object>.SuccessResponse(
            new { deletedCount = await service.BulkDeleteAsync(GetUserId(), dto) },
            "Flashcards deleted."));

    [HttpDelete("{flashcardId:int}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Delete(int flashcardId)
    {
        await service.DeleteAsync(GetUserId(), flashcardId);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Flashcard deleted."));
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
