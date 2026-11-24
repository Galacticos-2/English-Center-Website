using System.Text.Json;
using EnglishCenterSystem.Application
    .Features.Competitions.DTOs;
using EnglishCenterSystem.Application
    .Features.Competitions.Services;
using EnglishCenterSystem.Application
    .Features.Exercises.DTOs;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Infrastructure
    .Persistence.Context;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishCenterSystem.Infrastructure
    .BackgroundJobs;

public class
    CompetitionTestImportBackgroundJobService
{
    private readonly AppDbContext _context;

    private readonly CompetitionTestImportService
        _competitionTestImportService;
    private readonly ILogger<
    CompetitionTestImportBackgroundJobService>
    _logger;
    private readonly
        TeacherCompetitionAssignmentService
        _teacherCompetitionAssignmentService;

    public CompetitionTestImportBackgroundJobService(
    AppDbContext context,
    CompetitionTestImportService
        competitionTestImportService,
    TeacherCompetitionAssignmentService
        teacherCompetitionAssignmentService,
    ILogger<
        CompetitionTestImportBackgroundJobService>
        logger)
    {
        _context =
            context;

        _competitionTestImportService =
            competitionTestImportService;

        _teacherCompetitionAssignmentService =
            teacherCompetitionAssignmentService;

        _logger =
            logger;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessAsync(
        int systemBackgroundJobId)
    {
        var job =
            await _context.SystemBackgroundJobs
                .FirstOrDefaultAsync(x =>
                    x.SystemBackgroundJobId ==
                    systemBackgroundJobId);

        if (job == null)
        {
            return;
        }

        job.Status =
            BackgroundJobStatus.Processing;

        job.StartedAt =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        CompetitionTestImportJobPayloadDto?
            payload = null;

        try
        {
            payload =
                JsonSerializer.Deserialize<
                    CompetitionTestImportJobPayloadDto>(
                    job.PayloadJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive =
                            true
                    });

            if (payload == null)
            {
                throw new Exception(
                    "Competition import payload is invalid");
            }

            await using var testFileStream =
                File.OpenRead(
                    payload.TestFilePath);

            var audioFiles =
                new List<
                    ListeningAudioUploadItem>();

            try
            {
                foreach (var audio in
                    payload.AudioFiles)
                {
                    audioFiles.Add(
                        new ListeningAudioUploadItem
                        {
                            Stream =
                                File.OpenRead(
                                    audio.FilePath),

                            FileName =
                                audio.FileName,

                            ContentType =
                                audio.ContentType
                        });
                }

                var newCompetitionTestId =
                    await _competitionTestImportService
                        .ImportAsync(
                            payload.TeacherUserId,
                            testFileStream,
                            payload.TestFileName,
                            payload.ContentType,
                            payload.QuestionCountsJson,
                            payload.Transcript,
                            audioFiles,
                            payload.ImportDto);

                if (payload.ReplacingCompetitionTestId
    is int oldCompetitionTestId)
                {
                    try
                    {
                        await _teacherCompetitionAssignmentService
                            .ReplaceCompetitionTestAsync(
                                payload.TeacherUserId,
                                oldCompetitionTestId,
                                newCompetitionTestId);
                    }
                    catch (Exception replaceException)
                    {
                        try
                        {
                            await _teacherCompetitionAssignmentService
                                .CleanupFailedReplacementImportAsync(
                                    payload.TeacherUserId,
                                    newCompetitionTestId);
                        }
                        catch (Exception cleanupException)
                        {
                            /*
                             * Không được làm mất lỗi replace ban đầu.
                             * Cleanup lỗi chỉ ghi log để xử lý thủ công.
                             */
                            _logger.LogError(
                                cleanupException,
                                "Could not clean up imported CompetitionTestId {CompetitionTestId} after replacement failed",
                                newCompetitionTestId);
                        }

                        throw new Exception(
                            "The new competition test was imported, but replacing the old test failed",
                            replaceException);
                    }
                }

                job.Status =
                    BackgroundJobStatus.Completed;

                job.CompletedAt =
                    DateTime.UtcNow;

                job.ResultJson =
                    JsonSerializer.Serialize(
                        new
                        {
                            competitionTestId =
                                newCompetitionTestId
                        });
            }
            finally
            {
                foreach (var audioFile in
                    audioFiles)
                {
                    audioFile.Stream.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            job.Status =
                BackgroundJobStatus.Failed;

            job.CompletedAt =
                DateTime.UtcNow;

            job.ErrorMessage =
                ex.Message;

            await _context.SaveChangesAsync();

            throw;
        }
        finally
        {
            CleanupTempFiles(payload);
        }

        await _context.SaveChangesAsync();
    }

    private static void CleanupTempFiles(
        CompetitionTestImportJobPayloadDto?
            payload)
    {
        if (payload == null)
        {
            return;
        }

        var tempFolder =
            Path.GetDirectoryName(
                payload.TestFilePath);

        if (string.IsNullOrWhiteSpace(
                tempFolder) ||
            !Directory.Exists(tempFolder))
        {
            return;
        }

        try
        {
            Directory.Delete(
                tempFolder,
                true);
        }
        catch
        {
            // Không làm job thất bại vì cleanup.
        }
    }
}