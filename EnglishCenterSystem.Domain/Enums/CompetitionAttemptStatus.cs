namespace EnglishCenterSystem.Domain.Enums;

public enum CompetitionAttemptStatus
{
    InProgress = 1,
    Submitted = 2,
    PendingAiGrading = 3,
    AiGrading = 4,
    AiGraded = 5,
    AiGradingFailed = 6,
    Reviewed = 7,
    Expired = 8
}