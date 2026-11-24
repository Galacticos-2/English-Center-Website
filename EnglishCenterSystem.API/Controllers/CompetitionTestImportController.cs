using System.Security.Claims;
using System.Text.Json;
using EnglishCenterSystem.API.Requests;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Competitions.DTOs;
using EnglishCenterSystem.Domain.Entities.BackgroundJobs;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Infrastructure.BackgroundJobs;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/competition-test-import")]
[Authorize(Roles = "Teacher")]
public class CompetitionTestImportController
    : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IBackgroundJobClient
        _backgroundJobClient;
    private readonly IWebHostEnvironment
        _environment;

    public CompetitionTestImportController(
        AppDbContext context,
        IBackgroundJobClient backgroundJobClient,
        IWebHostEnvironment environment)
    {
        _context = context;
        _backgroundJobClient =
            backgroundJobClient;
        _environment = environment;
    }
  
   
    [HttpPost("import")]
    public async Task<IActionResult> Import(
        [FromForm]
        ImportCompetitionTestRequest request)
    {
        var teacherUserId =
            GetCurrentUserId();

        if (request.TestFile == null ||
            request.TestFile.Length == 0)
        {
            return BadRequest(
                ApiResponse<object>.FailResponse(
                    "Competition test file is required"));
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
                    "Only PDF, DOCX or TXT competition files are allowed"));
        }
        var tempRoot =
            Path.Combine(
                _environment.ContentRootPath,
                "TempUploads",
                "CompetitionTestImports",
                Guid.NewGuid().ToString());

        Directory.CreateDirectory(
            tempRoot);

        var safeTestFileName =
    Path.GetFileName(
        request.TestFile.FileName);

        var testFilePath =
            Path.Combine(
                tempRoot,
                $"competition_{Guid.NewGuid()}_{safeTestFileName}");

        await using (var fileStream =
            new FileStream(
                testFilePath,
                FileMode.Create))
        {
            await request.TestFile
                .CopyToAsync(fileStream);
        }

        var audioPayloads =
            new List<
                CompetitionTestImportAudioFileDto>();

        foreach (var audioFile in
            request.AudioFiles)
        {
            var audioFilePath =
                Path.Combine(
                    tempRoot,
                    $"{Guid.NewGuid()}_" +
                    $"{audioFile.FileName}");

            await using var audioStream =
                new FileStream(
                    audioFilePath,
                    FileMode.Create);

            await audioFile.CopyToAsync(
                audioStream);

            audioPayloads.Add(
                new CompetitionTestImportAudioFileDto
                {
                    FilePath = audioFilePath,
                    FileName = audioFile.FileName,
                    ContentType =
                        audioFile.ContentType
                });
        }

        var payload =
            new CompetitionTestImportJobPayloadDto
            {
                TeacherUserId =
                    teacherUserId,
                ReplacingCompetitionTestId =
    request.ReplacingCompetitionTestId,
                TestFilePath =
                    testFilePath,

                TestFileName =
                    request.TestFile.FileName,

                ContentType =
                    request.TestFile.ContentType,

                QuestionCountsJson =
                    request.QuestionCountsJson,

                Transcript =
                    request.Transcript,

                AudioFiles =
                    audioPayloads,

                ImportDto =
                    new ImportCompetitionTestDto
                    {
                        Title =
                            request.Title,

                        Description =
                            request.Description,

                        TestType =
                            request.TestType,

                        CourseType =
                            request.CourseType,

                        GenerateExplanations =
                            request.GenerateExplanations
                    }
            };

        var systemJob =
            new SystemBackgroundJob
            {
                CreatedByUserId =
                    teacherUserId,

                JobType =
                    BackgroundJobType
                        .CompetitionTestImport,

                Status =
                    BackgroundJobStatus.Pending,

                PayloadJson =
                    JsonSerializer.Serialize(
                        payload)
            };

        await _context.SystemBackgroundJobs
            .AddAsync(systemJob);

        await _context.SaveChangesAsync();

        var hangfireJobId =
            _backgroundJobClient.Enqueue<
                CompetitionTestImportBackgroundJobService>(
                service =>
                    service.ProcessAsync(
                        systemJob
                            .SystemBackgroundJobId));

        systemJob.HangfireJobId =
            hangfireJobId;

        await _context.SaveChangesAsync();

        return Ok(
            ApiResponse<object>.SuccessResponse(
                new
                {
                    backgroundJobId =
                        systemJob
                            .SystemBackgroundJobId,

                    hangfireJobId
                },
                "Competition test import queued"));
    }

    private int GetCurrentUserId()
    {
        return int.Parse(
            User.FindFirst(
                ClaimTypes.NameIdentifier)!
                .Value);
    }
}
