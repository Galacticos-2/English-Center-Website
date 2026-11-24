using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using EnglishCenterSystem.Application.Features.BackgroundJobs.DTOs;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Exercises.Services;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.BackgroundJobs;

public class ListeningImportBackgroundJobService
{
    private readonly AppDbContext _context;
    private readonly ListeningExerciseImportService
        _listeningExerciseImportService;

    public ListeningImportBackgroundJobService(
        AppDbContext context,
        ListeningExerciseImportService listeningExerciseImportService)
    {
        _context = context;
        _listeningExerciseImportService =
            listeningExerciseImportService;
    }

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

        try
        {
            var payload =
                JsonSerializer.Deserialize<ListeningImportJobPayloadDto>(
                    job.PayloadJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (payload == null)
            {
                throw new Exception("Listening import job payload is invalid");
            }

            await using var questionStream =
                File.OpenRead(payload.QuestionFilePath);

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

                var exerciseId =
                    await _listeningExerciseImportService
                        .ImportListeningExerciseAsync(
                            payload.TeacherUserId,
                            audioFiles,
                            questionStream,
                            payload.QuestionFileName,
                            payload.QuestionCountsJson,
                            payload.ImportDto);

                job.Status = BackgroundJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.ResultJson = JsonSerializer.Serialize(new
                {
                    exerciseId
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
        }

        await _context.SaveChangesAsync();
    }
}