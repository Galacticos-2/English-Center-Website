using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IMinioService
{
    Task<string> UploadFileAsync(
        Stream stream,
        string objectName,
        string contentType);
    Task<MemoryStream> DownloadFileAsync(string objectName);
    Task DeleteFileByUrlAsync(string? fileUrl);
    Task<string> GetPresignedUrlAsync(
    string objectName,
    int expirySeconds = 600);
    Task DeleteFolderAsync(string prefix);
    Task DeleteFileAsync(string objectName);
}
