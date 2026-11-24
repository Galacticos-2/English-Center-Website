using EnglishCenterSystem.Application.Interfaces.Services;
using Minio;
using Minio.DataModel.Args;

namespace EnglishCenterSystem.Infrastructure.Services;

public class MinioService : IMinioService
{
    private readonly IMinioClient _minioClient;

    private const string Bucket = "eduenglish";

    public MinioService()
    {
        _minioClient = new MinioClient()
            .WithEndpoint("localhost:9000")
            .WithCredentials("minioadmin", "minioadmin")
            .WithSSL(false)
            .Build();
    }
    public async Task<string>
    GetPresignedUrlAsync(
        string objectName,
        int expirySeconds = 600)
    {
        if (string.IsNullOrWhiteSpace(
            objectName))
        {
            throw new Exception(
                "Object name is required");
        }

        if (expirySeconds <= 0)
        {
            throw new Exception(
                "Expiry seconds must be greater than 0");
        }

        var found =
            await _minioClient
                .BucketExistsAsync(
                    new BucketExistsArgs()
                        .WithBucket(Bucket));

        if (!found)
        {
            throw new Exception(
                "MinIO bucket not found");
        }

        var presignedUrl =
            await _minioClient
                .PresignedGetObjectAsync(
                    new PresignedGetObjectArgs()
                        .WithBucket(Bucket)
                        .WithObject(objectName)
                        .WithExpiry(
                            expirySeconds));

        return presignedUrl;
    }
    public async Task<MemoryStream> DownloadFileAsync(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new Exception("Object name is required");
        }

        var memoryStream = new MemoryStream();

        await _minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(Bucket)
                .WithObject(objectName)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                }));

        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task<string> UploadFileAsync(
        Stream stream,
        string objectName,
        string contentType)
    {
        var found = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs()
                .WithBucket(Bucket));

        if (!found)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs()
                    .WithBucket(Bucket));
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await _minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(Bucket)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType)
        );

        return $"http://localhost:9000/{Bucket}/{objectName}";
    }
    public async Task DeleteFileByUrlAsync(
    string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return;
        }

        var objectName =
            GetObjectNameFromUrl(fileUrl);

        if (string.IsNullOrWhiteSpace(objectName))
        {
            return;
        }

        await DeleteFileAsync(objectName);
    }

    public async Task DeleteFolderAsync(
        string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return;
        }

        var found =
            await _minioClient.BucketExistsAsync(
                new BucketExistsArgs()
                    .WithBucket(Bucket));

        if (!found)
        {
            return;
        }

        prefix = prefix
            .Replace("\\", "/")
            .TrimStart('/');

        if (!prefix.EndsWith('/'))
        {
            prefix += "/";
        }

        var objects =
            _minioClient.ListObjectsEnumAsync(
                new ListObjectsArgs()
                    .WithBucket(Bucket)
                    .WithPrefix(prefix)
                    .WithRecursive(true));

        await foreach (var item in objects)
        {
            await _minioClient.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(Bucket)
                    .WithObject(item.Key));
        }
    }

    private static string? GetObjectNameFromUrl(
        string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(
                fileUrl,
                UriKind.Absolute,
                out var uri))
        {
            return fileUrl
                .Replace("\\", "/")
                .TrimStart('/');
        }

        var path =
            Uri.UnescapeDataString(
                uri.AbsolutePath)
            .TrimStart('/');

        var bucketPrefix =
            $"{Bucket}/";

        if (!path.StartsWith(
                bucketPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return path[bucketPrefix.Length..];
    }
    public async Task DeleteFileAsync(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return;
        }

        var found = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs()
                .WithBucket(Bucket));

        if (!found)
        {
            return;
        }

        await _minioClient.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(Bucket)
                .WithObject(objectName));
    }
}
