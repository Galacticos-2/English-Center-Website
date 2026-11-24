namespace EnglishCenterSystem.Application.Features.Flashcards.DTOs;

public class FlashcardDto
{
    public int VocabularyFlashcardId { get; set; }
    public int ClassRoomId { get; set; }
    public string ClassRoomName { get; set; } = null!;
    public string Word { get; set; } = null!;
    public string Meaning { get; set; } = null!;
    public string ExampleSentence { get; set; } = null!;
}

public class SaveFlashcardDto
{
    public int ClassRoomId { get; set; }
    public string Word { get; set; } = null!;
    public string Meaning { get; set; } = null!;
    public string ExampleSentence { get; set; } = null!;
}

public class BulkDeleteFlashcardsDto
{
    public List<int> FlashcardIds { get; set; } = [];
}

public class FlashcardImportPreviewDto
{
    public int TotalRows { get; set; }
    public List<FlashcardImportRowDto> PreviewRows { get; set; } = [];
    public List<FlashcardImportErrorDto> Errors { get; set; } = [];
    public List<FlashcardImportSkippedRowDto> SkippedRows { get; set; } = [];
    public int ImportableCount => PreviewRows.Count;
    public bool IsValid => Errors.Count == 0;
    public bool CanImport => IsValid && ImportableCount > 0;
}

public class FlashcardImportRowDto
{
    public int RowNumber { get; set; }
    public string Word { get; set; } = null!;
    public string Meaning { get; set; } = null!;
    public string ExampleSentence { get; set; } = null!;
}

public class FlashcardImportErrorDto
{
    public int? RowNumber { get; set; }
    public string Message { get; set; } = null!;
}

public class FlashcardImportSkippedRowDto
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = null!;
}

public class FlashcardImportResultDto
{
    public int CreatedCount { get; set; }
}

public class FlashcardStudyProgressDto
{
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
}

public class FlashcardStudyOverviewDto
{
    public FlashcardStudyProgressDto Practice { get; set; } = new();
    public FlashcardStudyProgressDto Quiz { get; set; } = new();
}

public class FlashcardStudySessionDto
{
    public FlashcardStudyProgressDto Progress { get; set; } = new();
    public List<FlashcardDto> RemainingFlashcards { get; set; } = [];
}
