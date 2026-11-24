using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities;

public class StudentFlashcardProgress : BaseEntity
{
    public int StudentFlashcardProgressId { get; set; }
    public int StudentUserId { get; set; }
    public int VocabularyFlashcardId { get; set; }
    public FlashcardStudyMode Mode { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public User StudentUser { get; set; } = null!;
    public VocabularyFlashcard VocabularyFlashcard { get; set; } = null!;
}
