using System.Text.Json;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Application.Features.Tests.Services;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace EnglishCenterSystem.Infrastructure.BackgroundJobs;

public class TestImportBackgroundJobService
{
    private readonly AppDbContext _context;
    private readonly TestImportService _testImportService;

    public TestImportBackgroundJobService(
        AppDbContext context,
        TestImportService testImportService)
    {
        _context = context;
        _testImportService = testImportService;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessAsync(int systemBackgroundJobId)
    {
        var job = await _context.SystemBackgroundJobs
            .FirstOrDefaultAsync(x =>
                x.SystemBackgroundJobId == systemBackgroundJobId);

        if (job == null)
        {
            return;
        }

        job.Status = BackgroundJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TestImportJobPayloadDto? payload = null;

        try
        {
            payload =
                JsonSerializer.Deserialize<TestImportJobPayloadDto>(
                    job.PayloadJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (payload == null)
            {
                throw new Exception("Test import job payload is invalid");
            }

            await using var testFileStream =
                File.OpenRead(payload.TestFilePath);

            var audioFiles = new List<ListeningAudioUploadItem>();

            try
            {
                foreach (var audio in payload.AudioFiles)
                {
                    audioFiles.Add(new ListeningAudioUploadItem
                    {
                        Stream = File.OpenRead(audio.FilePath),
                        FileName = audio.FileName,
                        ContentType = audio.ContentType
                    });
                }

                var testId = await _testImportService.ImportTestAsync(
                    payload.TeacherUserId,
                    testFileStream,
                    payload.TestFileName,
                    payload.ContentType,
                    payload.QuestionCountsJson,
                    payload.Transcript,
                    audioFiles,
                    payload.ImportDto);

                job.Status = BackgroundJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.ResultJson = JsonSerializer.Serialize(new
                {
                    testId
                });
            }
            finally
            {
                foreach (var audioFile in audioFiles)
                {
                    audioFile.Stream.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            job.Status = BackgroundJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;

            await _context.SaveChangesAsync();

            throw;
        }
        finally
        {
            CleanupTempFiles(payload);
        }

        await _context.SaveChangesAsync();
    }

    private void CleanupTempFiles(TestImportJobPayloadDto? payload)
    {
        if (payload == null)
        {
            return;
        }

        var tempFolder = Path.GetDirectoryName(payload.TestFilePath);

        if (string.IsNullOrWhiteSpace(tempFolder))
        {
            return;
        }

        if (!Directory.Exists(tempFolder))
        {
            return;
        }

        try
        {
            Directory.Delete(tempFolder, true);
        }
        catch
        {
            // Không throw ở đây để tránh làm job bị fail chỉ vì cleanup lỗi.
        }
    }
}