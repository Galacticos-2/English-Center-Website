namespace EnglishCenterSystem.API.Requests;

public class UploadAvatarRequest
{
    public IFormFile File { get; set; } = null!;
    public IFormFile OriginalFile { get; set; } = null!;
}
