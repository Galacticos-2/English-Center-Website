using EnglishCenterSystem.Application
    .Features.Competitions.DTOs.Teacher;
using EnglishCenterSystem.Application
    .Features.Competitions.Mappers;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application
    .Features.Competitions.Services;

public class TeacherCompetitionResultService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly CompetitionResultMapper
        _competitionResultMapper;

    public TeacherCompetitionResultService(
        IUnitOfWork unitOfWork,
        CompetitionResultMapper competitionResultMapper)
    {
        _unitOfWork = unitOfWork;
        _competitionResultMapper =
            competitionResultMapper;
    }

    public async Task<TeacherCompetitionLeaderboardDto>
        GetLeaderboardAsync(
            int teacherUserId,
            int competitionTestAssignmentId)
    {
        var assignment =
            await _unitOfWork
                .CompetitionTestAssignments
                .GetDetailAsync(
                    competitionTestAssignmentId);

        if (assignment == null ||
            assignment.TeacherUserId != teacherUserId)
        {
            throw new Exception(
                "Competition not found");
        }

        var totalStudents =
            assignment.ClassRoom.Students
                .Count(x => !x.IsDeleted);

        var participantCount =
            assignment.Attempts.Count;

        var submittedCount =
            assignment.Attempts.Count(x =>
                x.SubmittedAt != null);

        var results =
            await _unitOfWork
                .CompetitionResults
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.CompetitionTestAssignmentId ==
                    competitionTestAssignmentId)
                .OrderBy(x => x.Rank)
                .ToListAsync();

        return new TeacherCompetitionLeaderboardDto
        {
            CompetitionTestAssignmentId =
                assignment
                    .CompetitionTestAssignmentId,

            CompetitionTitle =
                assignment.Title,

            CompetitionTestTitle =
                assignment.CompetitionTest.Title,

            ClassRoomName =
                assignment.ClassRoom.ClassName,

            StartsAt =
                assignment.StartsAt,

            DeadlineAt =
                assignment.DeadlineAt,

            Status =
                ResolveStatus(
                    assignment.Status,
                    assignment.StartsAt,
                    assignment.DeadlineAt),

            RankedAt =
                assignment.RankedAt,

            IsLeaderboardAvailable =
                assignment.Status ==
                    CompetitionStatus.Ranked,

            TotalStudentsInClass =
                totalStudents,

            ParticipantCount =
                participantCount,

            SubmittedCount =
                submittedCount,

            Rankings =
                results.Select(result =>
                    new TeacherCompetitionLeaderboardItemDto
                    {
                        CompetitionResultId =
                            result.CompetitionResultId,

                        CompetitionTestAttemptId =
                            result
                                .CompetitionTestAttemptId,

                        StudentRoleId =
                            result.StudentRoleId,

                        StudentName =
                            result.StudentRole.User?.FullName ??
                            result.StudentRole.User?.UserName ??
                            "",

                        Rank =
                            result.Rank,

                        FinalScore =
                            result.FinalScore,

                        CorrectAnswers =
                            result.CorrectAnswers,

                        TotalQuestions =
                            result.TotalQuestions,

                        CompletionSeconds =
                            result.CompletionSeconds,

                        StartedAt =
                            result.Attempt.StartedAt,

                        SubmittedAt =
                            result.SubmittedAt,

                        AttemptStatus =
                            result.Attempt.Status.ToString()
                    })
                    .ToList()
        };
    }

    public async Task<TeacherCompetitionAttemptDetailDto>
        GetAttemptDetailAsync(
            int teacherUserId,
            int competitionTestAttemptId)
    {
        var attempt =
            await _unitOfWork
                .CompetitionTestAttempts
                .GetTeacherAttemptDetailAsync(
                    teacherUserId,
                    competitionTestAttemptId);

        if (attempt == null)
        {
            throw new Exception(
                "Competition attempt not found");
        }

        return new TeacherCompetitionAttemptDetailDto
        {
            CompetitionTestAssignmentId =
                attempt.CompetitionTestAssignmentId,

            CompetitionTestAttemptId =
                attempt.CompetitionTestAttemptId,

            StudentRoleId =
                attempt.StudentRoleId,

            StudentName =
                attempt.StudentRole.User?.FullName ??
                attempt.StudentRole.User?.UserName ??
                "",

            ClassRoomName =
                attempt.StudentRole.ClassRoom?.ClassName ??
                attempt.Assignment.ClassRoom.ClassName,

            Rank =
                attempt.Result?.Rank,

            Result =
                _competitionResultMapper.Build(
                    attempt)
        };
    }

    private static CompetitionStatus ResolveStatus(
        CompetitionStatus storedStatus,
        DateTime startsAt,
        DateTime deadlineAt)
    {
        if (storedStatus is
            CompetitionStatus.Cancelled or
            CompetitionStatus.WaitingForGrading or
            CompetitionStatus.Ranked or
            CompetitionStatus.Failed)
        {
            return storedStatus;
        }

        var now = DateTime.UtcNow;

        if (now >= deadlineAt)
        {
            return CompetitionStatus.Closed;
        }

        if (now >= startsAt)
        {
            return CompetitionStatus.Open;
        }

        return CompetitionStatus.Scheduled;
    }
}