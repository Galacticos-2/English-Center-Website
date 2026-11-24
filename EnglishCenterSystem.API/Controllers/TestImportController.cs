using System.Security.Claims;
using System.Text.Json;
using EnglishCenterSystem.API.Requests;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Domain.Entities.BackgroundJobs;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Infrastructure.BackgroundJobs;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/test-import")]
public class TestImportController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IWebHostEnvironment _environment;
    public TestImportController(
        AppDbContext context,
        IBackgroundJobClient backgroundJobClient,
        IWebHostEnvironment environment)
    {
        _context = context;
        _backgroundJobClient = backgroundJobClient;
        _environment = environment;
    }
    [Authorize(Roles = "Teacher")]
    [HttpGet("jobs/{jobId}")]
    public async Task<IActionResult> GetImportJobStatus(int jobId)
    {
        var job = await _context.SystemBackgroundJobs
            .Where(x =>
                x.SystemBackgroundJobId == jobId &&
                x.CreatedByUserId == int.Parse(
                    User.FindFirst(
                        ClaimTypes.NameIdentifier)!
                        .Value))
            .Select(x => new
            {
                x.SystemBackgroundJobId,
                x.Status,
                x.ErrorMessage,
                x.StartedAt,
                x.CompletedAt
            })
            .FirstOrDefaultAsync();

        if (job == null)
        {
            return NotFound(ApiResponse<object>.FailResponse(
                "Import job not found"));
        }

        return Ok(ApiResponse<object>.SuccessResponse(
            job,
            "Import job status"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPost("import-assign")]
    public async Task<IActionResult> ImportAndAssign(
        [FromForm] ImportTestRequest request)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        if (request.TestFile == null ||
            request.TestFile.Length == 0)
        {
            return BadRequest(ApiResponse<object>.FailResponse(
                "Test file is required"));
        }
        var testFileExtension = Path
    .GetExtension(request.TestFile.FileName)
    .ToLowerInvariant();

        var allowedDocumentExtensions =
            new HashSet<string>
            {
        ".pdf",
        ".docx",
        ".txt"
            };

        if (!allowedDocumentExtensions.Contains(
                testFileExtension))
        {
            return BadRequest(
                ApiResponse<object>.FailResponse(
                    "Only PDF, DOCX or TXT test files are allowed"));
        }
        var tempRoot = Path.Combine(
            _environment.ContentRootPath,
            "TempUploads",
            "TestImports",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);

        var safeTestFileName =
    Path.GetFileName(
        request.TestFile.FileName);

        var testFilePath = Path.Combine(
            tempRoot,
            $"test_{Guid.NewGuid()}_{safeTestFileName}");

        await using (var fileStream =
            new FileStream(testFilePath, FileMode.Create))
        {
            await request.TestFile.CopyToAsync(fileStream);
        }
        var audioPayloads = new List<TestImportAudioFileDto>();

        foreach (var audioFile in request.AudioFiles)
        {
            var audioFilePath = Path.Combine(
                tempRoot,
                $"{Guid.NewGuid()}_{audioFile.FileName}");

            await using var audioStream =
                new FileStream(audioFilePath, FileMode.Create);

            await audioFile.CopyToAsync(audioStream);

            audioPayloads.Add(new TestImportAudioFileDto
            {
                FilePath = audioFilePath,
                FileName = audioFile.FileName,
                ContentType = audioFile.ContentType
            });
        }
        var payload = new TestImportJobPayloadDto
        {
            TeacherUserId = teacherUserId,
            TestFilePath = testFilePath,
            TestFileName = request.TestFile.FileName,
            ContentType = request.TestFile.ContentType,
            QuestionCountsJson = request.QuestionCountsJson,
            Transcript = request.Transcript,
            AudioFiles = audioPayloads,
            ImportDto = new ImportTestDto
            {
                Title = request.Title,
                Description = request.Description,
                TestType = request.TestType,
                CourseType = request.CourseType,
                ClassRoomId = request.ClassRoomId,
                MaxAttempts = request.MaxAttempts,
                DurationMinutes = request.DurationMinutes,
                DeadlineAt = request.DeadlineAt,
                GenerateExplanations = request.GenerateExplanations
            }
        };

        var systemJob = new SystemBackgroundJob
        {
            CreatedByUserId = teacherUserId,
            JobType = BackgroundJobType.TestImport,
            Status = BackgroundJobStatus.Pending,
            PayloadJson = JsonSerializer.Serialize(payload)
        };

        await _context.SystemBackgroundJobs.AddAsync(systemJob);
        await _context.SaveChangesAsync();

        var hangfireJobId =
            _backgroundJobClient.Enqueue<TestImportBackgroundJobService>(
                service => service.ProcessAsync(
                    systemJob.SystemBackgroundJobId));

        systemJob.HangfireJobId = hangfireJobId;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                backgroundJobId = systemJob.SystemBackgroundJobId,
                hangfireJobId
            },
            "Test import job has been queued"));
    }
}
