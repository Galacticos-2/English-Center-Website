using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class VocabularyFlashcard : BaseEntity
{
    public int VocabularyFlashcardId { get; set; }
    public int TeacherUserId { get; set; }
    public int ClassRoomId { get; set; }
    public string Word { get; set; } = null!;
    public string Meaning { get; set; } = null!;
    public string ExampleSentence { get; set; } = null!;

    public User TeacherUser { get; set; } = null!;
    public ClassRoom ClassRoom { get; set; } = null!;
}
