using Microsoft.AspNetCore.Http;

namespace EnglishCenterSystem.API.Controllers;

public class UploadLessonFileRequest
{
    public IFormFile File { get; set; } = null!;
}