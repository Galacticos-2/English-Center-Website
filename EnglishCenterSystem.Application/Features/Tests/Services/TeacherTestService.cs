using System.Drawing;
using ClosedXML.Excel;
using EnglishCenterSystem.Application.Features.Tests.DTOs.Teacher;
using EnglishCenterSystem.Application.Features.Competitions.DTOs;
using EnglishCenterSystem.Application.Features.Tests.Mappers;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using EnglishCenterSystem.Application.Interfaces.Services;
namespace EnglishCenterSystem.Application.Features.Tests.Services;

public class TeacherTestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TestResultMapper _testResultMapper;
    private readonly IQuestionExplanationService _questionExplanationService;
    private readonly IMinioService _minioService;

    public TeacherTestService(
        IUnitOfWork unitOfWork,
        TestResultMapper testResultMapper,
        IQuestionExplanationService questionExplanationService,
        IMinioService minioService)
    {
        _unitOfWork = unitOfWork;
        _testResultMapper = testResultMapper;
        _questionExplanationService = questionExplanationService;
        _minioService = minioService;
    }
    public async Task<CompetitionSourceFileDto> GetSourceFileAsync(
        int teacherUserId,
        int testId)
    {
        var objectName = await _unitOfWork.Tests.QueryBasic()
            .AsNoTracking()
            .Where(x => x.TestId == testId &&
                x.TeacherUserId == teacherUserId && x.IsActive)
            .Select(x => x.SourceFileObjectName)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new Exception("Test source file not found");
        }

        return new CompetitionSourceFileDto
        {
            FileUrl = await _minioService.GetPresignedUrlAsync(objectName, 600),
            FileName = Path.GetFileName(objectName)
        };
    }
    public async Task<TeacherTestExportResultDto> ExportResultsAsync(
    int teacherUserId,
    int testId)
    {
        var test = await _unitOfWork.Tests
            .Query()
            .FirstOrDefaultAsync(x =>
                x.TestId == testId &&
                x.TeacherUserId == teacherUserId);

        if (test == null)
        {
            throw new Exception("Test not found");
        }

        var attempts = await _unitOfWork.TestAttempts
            .GetTeacherAttemptsAsync(
                teacherUserId,
                testId);

        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Test Results");

        worksheet.Cell(1, 1).Value = "Test";
        worksheet.Cell(1, 2).Value = test.Title;

        worksheet.Cell(2, 1).Value = "Exported At";
        worksheet.Cell(2, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        var headerRow = 4;

        worksheet.Cell(headerRow, 1).Value = "STT";
        worksheet.Cell(headerRow, 2).Value = "Student Name";
        worksheet.Cell(headerRow, 3).Value = "Class";
        worksheet.Cell(headerRow, 4).Value = "Score";
        worksheet.Cell(headerRow, 5).Value = "Total Points";
        worksheet.Cell(headerRow, 6).Value = "Correct Answers";
        worksheet.Cell(headerRow, 7).Value = "Total Questions";
        worksheet.Cell(headerRow, 8).Value = "Status";
        worksheet.Cell(headerRow, 9).Value = "Started At";
        worksheet.Cell(headerRow, 10).Value = "Submitted At";

        var headerRange = worksheet.Range(headerRow, 1, headerRow, 10);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var row = headerRow + 1;

        for (var i = 0; i < attempts.Count; i++)
        {
            var attempt = attempts[i];

            worksheet.Cell(row, 1).Value = i + 1;

            worksheet.Cell(row, 2).Value =
                attempt.StudentRole.User?.FullName ??
                attempt.StudentRole.User?.UserName ??
                "";

            worksheet.Cell(row, 3).Value =
                attempt.StudentRole.ClassRoom?.ClassName ??
                attempt.TestAssignment.ClassRoom?.ClassName ??
                "";

            worksheet.Cell(row, 4).Value = attempt.Score;
            worksheet.Cell(row, 5).Value = attempt.TotalPoints;
            worksheet.Cell(row, 6).Value = attempt.CorrectAnswers;
            worksheet.Cell(row, 7).Value = attempt.TotalQuestions;
            worksheet.Cell(row, 8).Value = attempt.Status.ToString();

            worksheet.Cell(row, 9).Value =
                attempt.StartedAt.ToString("dd/MM/yyyy HH:mm:ss");

            worksheet.Cell(row, 10).Value =
                attempt.SubmittedAt == null
                    ? "Chưa nộp"
                    : attempt.SubmittedAt.Value.ToString("dd/MM/yyyy HH:mm:ss");

            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        var safeTitle = MakeSafeFileName(test.Title);

        return new TeacherTestExportResultDto
        {
            FileBytes = stream.ToArray(),
            FileName = $"{safeTitle}_results.xlsx"
        };
    }
    public async Task<List<TeacherTestAttemptDto>> GetAttemptsAsync(
        int teacherUserId,
        int testId)
    {
        var testExists = await _unitOfWork.Tests
            .Query()
            .AnyAsync(x =>
                x.TestId == testId &&
                x.TeacherUserId == teacherUserId);

        if (!testExists)
        {
            throw new Exception("Test not found");
        }

        var attempts = await _unitOfWork.TestAttempts
            .GetTeacherAttemptsAsync(
                teacherUserId,
                testId);

        return attempts
            .Select(MapAttemptDto)
            .ToList();
    }
    private string MakeSafeFileName(string value)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(value)
            ? "test"
            : value.Trim();
    }
    public async Task<TeacherTestStudentResultDto> GetAttemptDetailAsync(
        int teacherUserId,
        int testAttemptId)
    {
        var attempt = await _unitOfWork.TestAttempts
            .GetTeacherAttemptDetailAsync(
                teacherUserId,
                testAttemptId);

        if (attempt == null)
        {
            throw new Exception("Test attempt not found");
        }

        return new TeacherTestStudentResultDto
        {
            Attempt = MapAttemptDto(attempt),
            Detail = _testResultMapper.BuildResultDto(attempt)
        };
    }
    public async Task UpdateTestAsync(
    int teacherUserId,
    int testId,
    UpdateTeacherTestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new Exception("Test title is required");
        }

        if (dto.MaxAttempts <= 0)
        {
            throw new Exception("Max attempts must be greater than 0");
        }

        if (dto.DurationMinutes <= 0)
        {
            throw new Exception("Duration must be greater than 0");
        }

        if (dto.DurationMinutes > 24 * 60)
        {
            throw new Exception("Duration cannot exceed 24 hours");
        }

        var test = await _unitOfWork.Tests
    .GetForUpdateAsync(
        testId,
        teacherUserId);

        if (test == null)
        {
            throw new Exception("Test not found");
        }

        test.Title = dto.Title.Trim();
        test.Description = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();
        test.UpdatedAt = DateTime.UtcNow;

        foreach (var assignment in test.Assignments)
        {
            assignment.MaxAttempts = dto.MaxAttempts;
            assignment.DurationMinutes = dto.DurationMinutes;
            assignment.DeadlineAt = dto.DeadlineAt;
            assignment.UpdatedAt = DateTime.UtcNow;
        }

        if (dto.GenerateExplanations)
        {
            var detailedTest = await _unitOfWork.Tests
                .Query()
                .FirstAsync(x =>
                    x.TestId == testId &&
                    x.TeacherUserId == teacherUserId);

            foreach (var listeningTask in detailedTest.ListeningTasks
                .Select(x => x.ListeningTask))
            {
                await _questionExplanationService
                    .AddListeningExplanationsAsync(listeningTask);
            }

            foreach (var readingTask in detailedTest.ReadingTasks
                .Select(x => x.ReadingTask))
            {
                await _questionExplanationService
                    .AddReadingExplanationsAsync(readingTask);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteTestAsync(
        int teacherUserId,
        int testId)
    {
        await _unitOfWork.Tests.HardDeleteAsync(
            testId,
            teacherUserId);

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task<List<TeacherTestDto>> GetMyTestsAsync(
    int teacherUserId)
    {
        var tests = await _unitOfWork.Tests.QueryBasic()
            .Where(x =>
                x.TeacherUserId == teacherUserId &&
                x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return tests.Select(x => new TeacherTestDto
        {
            TestId = x.TestId,
            Title = x.Title,
            Description = x.Description,
            TestType = x.TestType,
            CourseType = x.CourseType,
            TotalPoints = x.TotalPoints,
            IsActive = x.IsActive,
            HasSourceFile = !string.IsNullOrWhiteSpace(x.SourceFileObjectName),
            AssignmentCount = x.Assignments.Count(a => a.IsActive),
            MaxAttempts = x.Assignments
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.MaxAttempts)
                .FirstOrDefault(),
            DurationMinutes = x.Assignments
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.DurationMinutes)
                .FirstOrDefault(),
            DeadlineAt = x.Assignments
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.DeadlineAt)
                .FirstOrDefault(),
            CreatedAt = x.CreatedAt
        }).ToList();
    }
    public async Task<TeacherTestAnalyticsDto> GetAnalyticsAsync(
        int teacherUserId,
        int testId)
    {
        var test = await _unitOfWork.Tests
            .Query()
            .FirstOrDefaultAsync(x =>
                x.TestId == testId &&
                x.TeacherUserId == teacherUserId);

        if (test == null)
        {
            throw new Exception("Test not found");
        }

        var attempts = await _unitOfWork.TestAttempts
            .GetTeacherAttemptsAsync(
                teacherUserId,
                testId);

        var assignments = test.Assignments
            .Where(x => x.IsActive)
            .ToList();

        var classRoomIds = assignments
            .Select(x => x.ClassRoomId)
            .Distinct()
            .ToList();

        var totalStudents = await _unitOfWork.StudentRoles
            .CountAsync(x =>
                x.ClassRoomId != null &&
                classRoomIds.Contains(x.ClassRoomId.Value));

        var submittedAttempts = attempts
            .Where(x => x.Status != TestAttemptStatus.InProgress)
            .ToList();

        var scores = submittedAttempts
            .Select(x => x.Score)
            .ToList();

        var submittedStudents = submittedAttempts
            .Select(x => x.StudentRoleId)
            .Distinct()
            .Count();

        var pendingStudents = Math.Max(
            0,
            totalStudents - submittedStudents);

        var passCount = submittedAttempts
            .Where(x => x.Score >= 5)
            .Select(x => x.StudentRoleId)
            .Distinct()
            .Count();

        return new TeacherTestAnalyticsDto
        {
            TotalStudents = totalStudents,
            SubmittedStudents = submittedStudents,
            PendingStudents = pendingStudents,
            AverageScore = scores.Any()
                ? Math.Round(scores.Average(), 2)
                : 0,
            HighestScore = scores.Any()
                ? Math.Round(scores.Max(), 2)
                : 0,
            LowestScore = scores.Any()
                ? Math.Round(scores.Min(), 2)
                : 0,
            PassRate = submittedStudents > 0
                ? Math.Round((double)passCount / submittedStudents * 100, 2)
                : 0,
            CompletionRate = totalStudents > 0
                ? Math.Round((double)submittedStudents / totalStudents * 100, 2)
                : 0
        };
    }

    private TeacherTestAttemptDto MapAttemptDto(TestAttempt attempt)
    {
        return new TeacherTestAttemptDto
        {
            TestAttemptId = attempt.TestAttemptId,
            StudentRoleId = attempt.StudentRoleId,
            StudentName =
                attempt.StudentRole.User?.FullName ??
                attempt.StudentRole.User?.UserName ??
                "",
            ClassRoomName =
                attempt.StudentRole.ClassRoom?.ClassName ??
                attempt.TestAssignment.ClassRoom?.ClassName ??
                "",
            Score = attempt.Score,
            CorrectAnswers = attempt.CorrectAnswers,
            TotalQuestions = attempt.TotalQuestions,
            Status = attempt.Status.ToString(),
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt
        };
    }
}
