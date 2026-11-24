using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Application.Features.Users.DTOs;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Domain.Entities.Gamification;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Services;

public class StudentGamificationService(AppDbContext context) : IStudentGamificationService
{
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    public async Task<StudentGamificationSummaryDto> GetSummaryAsync(int studentRoleId)
    {
        var profile = await context.StudentExperiences
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.StudentRoleId == studentRoleId);

        var unlockedAchievementTypes = await context.StudentAchievements
            .AsNoTracking()
            .Where(x => x.StudentRoleId == studentRoleId)
            .Select(x => (int)x.Type)
            .ToListAsync();

        return new StudentGamificationSummaryDto
        {
            TotalExp = profile?.TotalExp ?? 0,
            CurrentStreakDays = profile?.CurrentStreakDays ?? 0,
            UnlockedAchievementTypes = unlockedAchievementTypes
        };
    }

    public async Task<StudentClassLeaderboardDto> GetClassLeaderboardAsync(int studentRoleId)
    {
        var student = await context.StudentRoles
            .AsNoTracking()
            .Include(x => x.ClassRoom)
            .SingleOrDefaultAsync(x => x.StudentRoleId == studentRoleId);

        if (student?.ClassRoomId == null)
        {
            return new StudentClassLeaderboardDto();
        }

        var students = await (
            from classStudent in context.StudentRoles.AsNoTracking()
            where classStudent.ClassRoomId == student.ClassRoomId &&
                  !classStudent.IsDeleted &&
                  classStudent.User.IsActive
            join experience in context.StudentExperiences.AsNoTracking()
                on classStudent.StudentRoleId equals experience.StudentRoleId into experiences
            from experience in experiences.DefaultIfEmpty()
            select new
            {
                classStudent.StudentRoleId,
                classStudent.User.FullName,
                classStudent.User.UserName,
                classStudent.User.AvatarUrl,
                TotalExp = experience == null ? 0 : experience.TotalExp
            })
            .OrderByDescending(x => x.TotalExp)
            .ThenBy(x => x.FullName)
            .ThenBy(x => x.StudentRoleId)
            .ToListAsync();

        return new StudentClassLeaderboardDto
        {
            ClassName = student.ClassRoom?.ClassName,
            Students = students.Select((item, index) => new StudentClassLeaderboardItemDto
            {
                StudentRoleId = item.StudentRoleId,
                FullName = string.IsNullOrWhiteSpace(item.FullName)
                    ? item.UserName
                    : item.FullName,
                AvatarUrl = item.AvatarUrl,
                TotalExp = item.TotalExp,
                Rank = students.Take(index).Count(x => x.TotalExp > item.TotalExp) + 1,
                Level = GamificationLevelHelper.GetLevel(item.TotalExp),
                Title = GamificationLevelHelper.GetTitle(item.TotalExp),
                IsCurrentStudent = item.StudentRoleId == studentRoleId
            }).ToList()
        };
    }

    public Task AwardLessonCompletionAsync(int studentRoleId, int lessonId) =>
        AwardActivityAsync(
            studentRoleId,
            StudentExpTransactionType.LessonCompletion,
            35,
            $"lesson:{lessonId}",
            DateTime.UtcNow);

    public async Task RecordDailyActivityAsync(int studentRoleId, DateTime activityAt)
    {
        var profile = await GetProfileAsync(studentRoleId);
        await AwardDailyActivityAsync(studentRoleId, profile, activityAt);
        await context.SaveChangesAsync();
    }

    public async Task AwardExerciseSubmissionAsync(int exerciseAttemptId)
    {
        var attempt = await context.ExerciseAttempts.Include(x => x.ExerciseAssignment)
            .ThenInclude(x => x.Exercise).SingleAsync(x => x.ExerciseAttemptId == exerciseAttemptId);
        var previous = await context.ExerciseAttempts.Where(x => x.StudentRoleId == attempt.StudentRoleId &&
                x.ExerciseAssignment.ExerciseId == attempt.ExerciseAssignment.ExerciseId && x.SubmittedAt != null)
            .OrderBy(x => x.SubmittedAt).ThenBy(x => x.ExerciseAttemptId).ToListAsync();
        var position = previous.FindIndex(x => x.ExerciseAttemptId == attempt.ExerciseAttemptId) + 1;
        var percentage = GetPercentage(attempt.Score, attempt.TotalPoints);
        var exp = position == 1 ? percentage >= 85 ? 20 : percentage > 50 ? 15 : 0
            : position == 2 && attempt.Score > previous.Take(position - 1).Max(x => x.Score) ? 5 : 0;

        await AwardActivityAsync(
            attempt.StudentRoleId,
            position == 1
                ? StudentExpTransactionType.ExerciseFirstAttempt
                : StudentExpTransactionType.ExerciseImprovement,
            exp,
            $"exercise-attempt:{attempt.ExerciseAttemptId}",
            attempt.SubmittedAt ?? DateTime.UtcNow);
    }

    public async Task AwardTestSubmissionAsync(int testAttemptId)
    {
        var attempt = await context.TestAttempts
            .Include(x => x.TestAssignment)
            .Include(x => x.Answers)
            .SingleAsync(x => x.TestAttemptId == testAttemptId);
        var previous = await context.TestAttempts.Where(x => x.StudentRoleId == attempt.StudentRoleId &&
                x.TestAssignment.TestId == attempt.TestAssignment.TestId && x.SubmittedAt != null)
            .OrderBy(x => x.SubmittedAt).ThenBy(x => x.TestAttemptId).ToListAsync();
        var position = previous.FindIndex(x => x.TestAttemptId == attempt.TestAttemptId) + 1;
        var percentage = GetPercentage(
            attempt.Answers.Sum(x => x.PointEarned),
            attempt.TotalPoints);
        var exp = position == 1 ? percentage >= 85 ? 50 : percentage > 50 ? 30 : 0
            : position == 2 && attempt.Score > previous.Take(position - 1).Max(x => x.Score) ? 15 : 0;

        await AwardActivityAsync(
            attempt.StudentRoleId,
            position == 1
                ? StudentExpTransactionType.TestFirstAttempt
                : StudentExpTransactionType.TestImprovement,
            exp,
            $"test-attempt:{attempt.TestAttemptId}",
            attempt.SubmittedAt ?? DateTime.UtcNow);
    }

    public async Task AwardCompetitionRankAsync(int competitionResultId)
    {
        var result = await context.CompetitionResults.SingleAsync(x => x.CompetitionResultId == competitionResultId);
        var exp = result.Rank switch { 1 => 100, 2 => 80, 3 => 60, _ => 30 };
        await AwardActivityAsync(
            result.StudentRoleId,
            StudentExpTransactionType.CompetitionRank,
            exp,
            $"competition:{result.CompetitionTestAssignmentId}:student:{result.StudentRoleId}",
            result.RankedAt);
    }

    private async Task AwardActivityAsync(
        int studentRoleId,
        StudentExpTransactionType type,
        int amount,
        string referenceKey,
        DateTime activityAt)
    {
        var profile = await GetProfileAsync(studentRoleId);
        if (amount > 0 && !await context.StudentExpTransactions.AnyAsync(x =>
                x.StudentRoleId == studentRoleId &&
                x.Type == type &&
                x.ReferenceKey == referenceKey))
        {
            context.StudentExpTransactions.Add(new StudentExpTransaction
            {
                StudentRoleId = studentRoleId,
                Type = type,
                ExpAmount = amount,
                ReferenceKey = referenceKey,
                AwardedAt = activityAt
            });
            profile.TotalExp += amount;
        }

        await AwardDailyActivityAsync(studentRoleId, profile, activityAt);
        await EvaluateAchievementsAsync(studentRoleId, profile, activityAt);
        await context.SaveChangesAsync();
    }

    private async Task AwardDailyActivityAsync(int studentRoleId, StudentExperience profile, DateTime now)
    {
        var activityAt = now.Kind == DateTimeKind.Utc ? now : now.ToUniversalTime();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(activityAt, VietnamTimeZone));
        var dailyReferenceKey = $"daily:{today:yyyy-MM-dd}";
        if (await context.StudentExpTransactions.AnyAsync(x =>
                x.StudentRoleId == studentRoleId &&
                x.Type == StudentExpTransactionType.DailyActivity &&
                x.ReferenceKey == dailyReferenceKey)) return;

        var advancedProfile = profile.LastActivityDate == null || today >= profile.LastActivityDate.Value;
        if (advancedProfile)
        {
            profile.CurrentStreakDays = profile.LastActivityDate == today.AddDays(-1)
                ? profile.CurrentStreakDays + 1
                : 1;
            profile.LastActivityDate = today;
        }

        profile.TotalExp += 5;
        context.StudentExpTransactions.Add(new StudentExpTransaction { StudentRoleId = studentRoleId, Type = StudentExpTransactionType.DailyActivity, ExpAmount = 5, ReferenceKey = dailyReferenceKey, AwardedAt = activityAt });
        if (advancedProfile && profile.CurrentStreakDays is 7 or 30)
        {
            var amount = profile.CurrentStreakDays == 7 ? 20 : 100;
            profile.TotalExp += amount;
            context.StudentExpTransactions.Add(new StudentExpTransaction { StudentRoleId = studentRoleId, Type = StudentExpTransactionType.StreakMilestone, ExpAmount = amount, ReferenceKey = $"streak:{profile.CurrentStreakDays}:{today:yyyy-MM-dd}", AwardedAt = activityAt });
        }
    }

    private async Task<StudentExperience> GetProfileAsync(int studentRoleId)
    {
        var profile = await context.StudentExperiences.SingleOrDefaultAsync(x => x.StudentRoleId == studentRoleId);
        if (profile != null) return profile;
        profile = new StudentExperience { StudentRoleId = studentRoleId };
        context.StudentExperiences.Add(profile);
        return profile;
    }

    private async Task EvaluateAchievementsAsync(int studentRoleId, StudentExperience profile, DateTime now)
    {
        var types = new List<StudentAchievementType>();
        if (profile.CurrentStreakDays >= 20) types.Add(StudentAchievementType.TwentyDayStreak);
        if (await context.ExerciseAttempts.Where(x => x.StudentRoleId == studentRoleId && x.SubmittedAt != null &&
                (x.Status == ExerciseAttemptStatus.Submitted || x.Status == ExerciseAttemptStatus.Reviewed || x.Status == ExerciseAttemptStatus.AiGraded))
                .Select(x => x.ExerciseAssignment.ExerciseId).Distinct().CountAsync() >= 30)
            types.Add(StudentAchievementType.ThirtyCompletedExercises);
        var completedTests = await context.TestAttempts
            .Include(x => x.Answers)
            .Where(x => x.StudentRoleId == studentRoleId && x.SubmittedAt != null &&
                (x.Status == TestAttemptStatus.Submitted || x.Status == TestAttemptStatus.Reviewed || x.Status == TestAttemptStatus.AiGraded))
            .ToListAsync();
        if (completedTests.Any(x => GetPercentage(x.Answers.Sum(answer => answer.PointEarned), x.TotalPoints) >= 100))
            types.Add(StudentAchievementType.PerfectTestOverallScore);
        if (await context.CompetitionResults.AnyAsync(x => x.StudentRoleId == studentRoleId && x.Rank <= 3))
            types.Add(StudentAchievementType.CompetitionTopThree);
        if (completedTests.Count(x => GetPercentage(x.Answers.Sum(answer => answer.PointEarned), x.TotalPoints) >= 80) >= 5)
            types.Add(StudentAchievementType.FiveTestsAtLeastEight);

        var unlocked = await context.StudentAchievements.Where(x => x.StudentRoleId == studentRoleId)
            .Select(x => x.Type).ToListAsync();
        foreach (var type in types.Where(type => !unlocked.Contains(type)))
        {
            context.StudentAchievements.Add(new StudentAchievement { StudentRoleId = studentRoleId, Type = type, UnlockedAt = now });
            context.StudentExpTransactions.Add(new StudentExpTransaction { StudentRoleId = studentRoleId, Type = StudentExpTransactionType.Achievement, ExpAmount = 50, ReferenceKey = $"achievement:{type}", AwardedAt = now });
            profile.TotalExp += 50;
        }
    }

    private static double GetPercentage(double score, double totalPoints) =>
        totalPoints <= 0 ? 0 : score / totalPoints * 100;

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }
}
