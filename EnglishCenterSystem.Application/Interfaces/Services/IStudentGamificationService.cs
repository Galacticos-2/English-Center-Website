namespace EnglishCenterSystem.Application.Interfaces.Services;

using EnglishCenterSystem.Application.Features.Users.DTOs;

public interface IStudentGamificationService
{
    Task<StudentGamificationSummaryDto> GetSummaryAsync(int studentRoleId);
    Task<StudentClassLeaderboardDto> GetClassLeaderboardAsync(int studentRoleId);
    Task RecordDailyActivityAsync(int studentRoleId, DateTime activityAt);
    Task AwardLessonCompletionAsync(int studentRoleId, int lessonId);
    Task AwardExerciseSubmissionAsync(int exerciseAttemptId);
    Task AwardTestSubmissionAsync(int testAttemptId);
    Task AwardCompetitionRankAsync(int competitionResultId);
}
