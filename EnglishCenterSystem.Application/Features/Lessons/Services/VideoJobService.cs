using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;

namespace EnglishCenterSystem.Application.Features.Lessons.Services;

public class VideoJobService
{
    private readonly IUnitOfWork _uow;
    private readonly IMinioService _minioService;
    private readonly IVideoProcessingService
        _videoProcessingService;

    public VideoJobService(
        IUnitOfWork uow,
        IMinioService minioService,
        IVideoProcessingService videoProcessingService)
    {
        _uow = uow;
        _minioService = minioService;
        _videoProcessingService =
            videoProcessingService;
    }

    public async Task ProcessVideoAsync(
    int lessonId,
    string lessonFolderName,
    string videoId,
    string inputPath,
    string expectedOriginalUrl,
    string? oldOriginalUrl,
    string? oldHlsUrl)
    {
        var lesson =
            await _uow.Lessons
                .GetLessonByIdAsync(lessonId);

        if (lesson?.Video == null)
        {
            throw new Exception(
                "Lesson video not found");
        }

        var tempRoot =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "temp",
                "videos");

        var videoTempFolder =
            Path.Combine(
                tempRoot,
                videoId);

        var outputFolder =
            Path.Combine(
                videoTempFolder,
                "hls");

        var newHlsPrefix =
            $"lessons/videos/hls/" +
            $"{lessonFolderName}/" +
            $"{videoId}/";

        try
        {
            await _videoProcessingService
                .ConvertToHlsAsync(
                    inputPath,
                    outputFolder);

            var hlsFiles =
                Directory.GetFiles(
                    outputFolder,
                    "*",
                    SearchOption.AllDirectories);

            string? hlsIndexUrl = null;

            foreach (var hlsFile in hlsFiles)
            {
                await using var hlsStream =
                    new FileStream(
                        hlsFile,
                        FileMode.Open,
                        FileAccess.Read);

                var relativePath =
                    Path.GetRelativePath(
                        outputFolder,
                        hlsFile)
                    .Replace("\\", "/");

                var objectName =
                    $"{newHlsPrefix}{relativePath}";

                var extension =
                    Path.GetExtension(hlsFile);

                var hlsContentType =
                    extension.Equals(
                        ".m3u8",
                        StringComparison.OrdinalIgnoreCase)
                        ? "application/vnd.apple.mpegurl"
                        : "video/mp2t";

                var uploadedUrl =
                    await _minioService
                        .UploadFileAsync(
                            hlsStream,
                            objectName,
                            hlsContentType);

                if (string.Equals(
                    relativePath,
                    "index.m3u8",
                    StringComparison.OrdinalIgnoreCase))
                {
                    hlsIndexUrl =
                        uploadedUrl;
                }
            }

            if (string.IsNullOrWhiteSpace(
                hlsIndexUrl))
            {
                throw new Exception(
                    "HLS index.m3u8 was not generated");
            }

            /*
             * Không cho job upload cũ ghi đè
             * lần upload mới hơn.
             */
            if (!string.Equals(
                lesson.Video.OriginalUrl,
                expectedOriginalUrl,
                StringComparison.Ordinal))
            {
                await _minioService
                    .DeleteFolderAsync(
                        newHlsPrefix);

                return;
            }

            lesson.Video.HlsUrl =
                hlsIndexUrl;

            lesson.Video.ProcessingStatus =
                "Ready";

            lesson.Video.ErrorMessage =
                null;

            await _uow.SaveChangesAsync();

            await DeleteOldVideoSafelyAsync(
                oldOriginalUrl,
                oldHlsUrl);
        }
        catch (Exception ex)
        {
            try
            {
                await _minioService
                    .DeleteFolderAsync(
                        newHlsPrefix);
            }
            catch
            {
                // Không che lỗi xử lý video.
            }

            if (string.Equals(
                lesson.Video.OriginalUrl,
                expectedOriginalUrl,
                StringComparison.Ordinal))
            {
                lesson.Video.OriginalUrl =
                    oldOriginalUrl ?? "";

                lesson.Video.HlsUrl =
                    oldHlsUrl ?? "";

                lesson.Video.ProcessingStatus =
                    !string.IsNullOrWhiteSpace(
                        oldHlsUrl)
                        ? "Ready"
                        : "Failed";

                lesson.Video.ErrorMessage =
                    ex.Message;

                await _uow.SaveChangesAsync();
            }

            try
            {
                await _minioService
                    .DeleteFileByUrlAsync(
                        expectedOriginalUrl);
            }
            catch
            {
                // Không che lỗi xử lý video.
            }

            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(inputPath))
                {
                    File.Delete(inputPath);
                }
            }
            catch
            {
                // Có thể bổ sung ILogger.
            }

            try
            {
                if (Directory.Exists(
                    videoTempFolder))
                {
                    Directory.Delete(
                        videoTempFolder,
                        true);
                }
            }
            catch
            {
                // Có thể bổ sung ILogger.
            }
        }
    }


    private async Task DeleteOldVideoSafelyAsync(
        string? oldOriginalUrl,
        string? oldHlsUrl)
    {
        if (!string.IsNullOrWhiteSpace(
                oldOriginalUrl))
        {
            try
            {
                await _minioService
                    .DeleteFileByUrlAsync(
                        oldOriginalUrl);
            }
            catch
            {
                // Có thể bổ sung ILogger sau.
            }
        }

        var oldHlsPrefix =
            GetFolderPrefixFromUrl(
                oldHlsUrl);

        if (!string.IsNullOrWhiteSpace(
                oldHlsPrefix))
        {
            try
            {
                await _minioService
                    .DeleteFolderAsync(
                        oldHlsPrefix);
            }
            catch
            {
                // Có thể bổ sung ILogger sau.
            }
        }
    }

    private static string?
        GetFolderPrefixFromUrl(
            string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(
                fileUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(
                fileUrl,
                UriKind.Absolute,
                out var uri))
        {
            return null;
        }

        var path =
            Uri.UnescapeDataString(
                uri.AbsolutePath)
            .TrimStart('/');

        const string bucketPrefix =
            "eduenglish/";

        if (!path.StartsWith(
                bucketPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var objectName =
            path[bucketPrefix.Length..];

        var lastSlash =
            objectName.LastIndexOf('/');

        if (lastSlash < 0)
        {
            return null;
        }

        return objectName[
            ..(lastSlash + 1)];
    }
}