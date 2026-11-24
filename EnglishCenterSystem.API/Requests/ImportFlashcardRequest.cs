namespace EnglishCenterSystem.API.Requests;

public class ImportFlashcardRequest
{
    public int ClassRoomId { get; set; }
    public IFormFile File { get; set; } = null!;
}
