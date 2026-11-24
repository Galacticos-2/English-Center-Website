using System.Security.Claims;
using EnglishCenterSystem.API.Requests;
using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Exercises.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Hangfire;
using EnglishCenterSystem.Application.Features.BackgroundJobs.DTOs;
using EnglishCenterSystem.Infrastructure.BackgroundJobs;
using EnglishCenterSystem.Domain.Entities.BackgroundJobs;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Application.Features.Writing.DTOs;
using EnglishCenterSystem.Application.Features.Writing.Services;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/exercise-import")]
public class ExerciseImportController : ControllerBase
{
    private readonly ExerciseImportService _exerciseImportService;
    private readonly ListeningExerciseImportService
    _listeningExerciseImportService;
    private readonly AppDbContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IWebHostEnvironment _environment;
    private readonly WritingExerciseImportService
    _writingExerciseImportService;
    private readonly ReadingExerciseImportService
_readingExerciseImportService;
    private readonly SpeakingExerciseImportService
    _speakingExerciseImportService;
    public ExerciseImportController(
        ExerciseImportService exerciseImportService,
    ListeningExerciseImportService listeningExerciseImportService,
    SpeakingExerciseImportService speakingExerciseImportService,
    WritingExerciseImportService writingExerciseImportService,
    ReadingExerciseImportService readingExerciseImportService,
    AppDbContext context,
    IBackgroundJobClient backgroundJobClient,
    IWebHostEnvironment environment)
    {
        _exerciseImportService = exerciseImportService;
        _listeningExerciseImportService = listeningExerciseImportService;
        _speakingExerciseImportService = speakingExerciseImportService;
        _readingExerciseImportService = readingExerciseImportService;
        _context = context;
        _writingExerciseImportService = writingExerciseImportService;
        _backgroundJobClient = backgroundJobClient;
        _environment = environment;
    }
    [Authorize(Roles = "Teacher")]
    [HttpPost("listening-import-assign")]
    public async Task<IActionResult> ImportListeningAndAssign(
    [FromForm] ImportListeningExerciseRequest request)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var tempRoot = Path.Combine(
            _environment.ContentRootPath,
            "TempUploads",
            "ListeningImports",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);

        var questionFilePath = Path.Combine(
            tempRoot,
            $"question_{request.QuestionFile.FileName}");

        await using (var questionFileStream =
            new FileStream(questionFilePath, FileMode.Create))
        {
            await request.QuestionFile.CopyToAsync(questionFileStream);
        }

        var audioPayloads = new List<ListeningImportAudioFileDto>();

        foreach (var audioFile in request.AudioFiles)
        {
            var audioFilePath = Path.Combine(
                tempRoot,
                $"{Guid.NewGuid()}_{audioFile.FileName}");

            await using var audioStream =
                new FileStream(audioFilePath, FileMode.Create);

            await audioFile.CopyToAsync(audioStream);

            audioPayloads.Add(new ListeningImportAudioFileDto
            {
                FilePath = audioFilePath,
                FileName = audioFile.FileName,
                ContentType = audioFile.ContentType
            });
        }

        var payload = new ListeningImportJobPayloadDto
        {
            TeacherUserId = teacherUserId,
            QuestionFilePath = questionFilePath,
            QuestionFileName = request.QuestionFile.FileName,
            QuestionCountsJson = request.QuestionCountsJson,
            AudioFiles = audioPayloads,
            ImportDto = new ImportListeningExerciseDto
            {
                Title = request.Title,
                Description = request.Description,
                Transcript = request.Transcript,
                CourseType = request.CourseType,
             
                ExerciseSectionId = request.ExerciseSectionId,
                ClassRoomId = request.ClassRoomId,
                
                MaxAttempts = request.MaxAttempts,
                DeadlineAt = request.DeadlineAt,
                GenerateExplanations = request.GenerateExplanations
            }
        };

        var systemJob = new SystemBackgroundJob
        {
            CreatedByUserId = teacherUserId,
            JobType = BackgroundJobType.ListeningImport,
            Status = BackgroundJobStatus.Pending,
            PayloadJson = JsonSerializer.Serialize(payload)
        };

        await _context.SystemBackgroundJobs.AddAsync(systemJob);
        await _context.SaveChangesAsync();

        var hangfireJobId = _backgroundJobClient.Enqueue<
            ListeningImportBackgroundJobService>(
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
            "Listening import job has been queued"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPost("writing-import-assign")]
    public async Task<IActionResult> ImportWritingAndAssign(
    [FromForm] ImportWritingExerciseRequest request)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        using var questionStream =
            request.QuestionFile.OpenReadStream();

        var dto = new ImportWritingExerciseDto
        {
            Title = request.Title,
            Description = request.Description,
            CourseType = request.CourseType,
            ExerciseSectionId = request.ExerciseSectionId,
            ClassRoomId = request.ClassRoomId,

            MaxAttempts = request.MaxAttempts,
            DeadlineAt = request.DeadlineAt
        };

        var exerciseId =
            await _writingExerciseImportService
                .ImportWritingExerciseAsync(
                    teacherUserId,
                    questionStream,
                    request.QuestionFile.FileName,
                    dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                exerciseId
            },
            "Writing exercise imported and assigned"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPost("reading-import-assign")]
    public async Task<IActionResult> ImportReadingAndAssign(
    [FromForm] ImportReadingExerciseRequest request)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        using var questionStream =
            request.QuestionFile.OpenReadStream();

        var dto = new ImportReadingExerciseDto
        {
            Title = request.Title,
            Description = request.Description,
            CourseType = request.CourseType,
            ExerciseSectionId = request.ExerciseSectionId,
            ClassRoomId = request.ClassRoomId,
          
            MaxAttempts = request.MaxAttempts,
            DeadlineAt = request.DeadlineAt,
            GenerateExplanations = request.GenerateExplanations
        };

        var exerciseId =
            await _readingExerciseImportService
                .ImportReadingExerciseAsync(
                    teacherUserId,
                    questionStream,
                    request.QuestionFile.FileName,
                    dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                exerciseId
            },
            "Reading exercise imported and assigned"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPost("speaking-import-assign")]
    public async Task<IActionResult> ImportSpeakingAndAssign(
    [FromForm] ImportSpeakingExerciseRequest request)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        using var questionStream =
            request.QuestionFile.OpenReadStream();

        var dto = new ImportSpeakingExerciseDto
        {
            Title = request.Title,
            Description = request.Description,
            CourseType = request.CourseType,
          
            ExerciseSectionId = request.ExerciseSectionId,
            ClassRoomId = request.ClassRoomId,
           
            MaxAttempts = request.MaxAttempts,
            DeadlineAt = request.DeadlineAt,
            PreparationTimeSeconds =
                request.PreparationTimeSeconds,
            SpeakingTimeSeconds =
                request.SpeakingTimeSeconds
        };

        var exerciseId =
            await _speakingExerciseImportService
                .ImportSpeakingExerciseAsync(
                    teacherUserId,
                    questionStream,
                    request.QuestionFile.FileName,
                    request.QuestionFile.ContentType,
                    dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                exerciseId
            },
            "Speaking exercise imported and assigned"));
    }
    [Authorize(Roles = "Teacher")]
    [HttpPost("import-assign")]
    public async Task<IActionResult> ImportAndAssign(
        [FromForm] ImportAssignExerciseRequest request)
    {
        var teacherUserId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        using var stream = request.File.OpenReadStream();

        var dto = new ImportAssignExerciseDto
        {
            Title = request.Title,
            Description = request.Description,
            CourseType = request.CourseType,
            Skill = request.Skill,
           
            ExerciseSectionId = request.ExerciseSectionId,
            ClassRoomId = request.ClassRoomId,
           
            MaxAttempts = request.MaxAttempts,
            DeadlineAt = request.DeadlineAt,
            GenerateExplanations = request.GenerateExplanations
        };

        var exerciseId = await _exerciseImportService
            .ImportAndAssignAsync(
                teacherUserId,
                stream,
                request.File.FileName,
                
                dto);

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                exerciseId
            },
            "Exercise set imported and assigned"));
    }
}
