using EnglishCenterSystem.Application.Interfaces.Services;
using FFMpegCore;
using FFMpegCore.Enums;

namespace EnglishCenterSystem.Infrastructure.Services;

public class VideoProcessingService : IVideoProcessingService
{
    public async Task ConvertToHlsAsync(
        string inputPath,
        string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);

        var outputPath = Path.Combine(
            outputFolder,
            "index.m3u8");

        try
        {
            await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(
                    outputPath,
                    true,
                    options => options
                        .WithVideoCodec(VideoCodec.LibX264)
                        .WithAudioCodec(AudioCodec.Aac)
                        .WithCustomArgument("-preset veryfast")
                        .WithCustomArgument("-profile:v baseline")
                        .WithCustomArgument("-level 3.0")
                        .WithCustomArgument("-start_number 0")
                        .WithCustomArgument("-hls_time 10")
                        .WithCustomArgument("-hls_list_size 0")
                        .WithCustomArgument("-f hls"))
                .ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            Console.WriteLine("FFMPEG ERROR:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.InnerException?.Message);
            throw;
        }

        if (!File.Exists(outputPath))
        {
            throw new Exception("FFmpeg did not create index.m3u8");
        }
    }
}