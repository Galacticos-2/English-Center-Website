using EnglishCenterSystem.Application.Features.Competitions.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application.Features.Competitions.Services;

public class TeacherCompetitionTestService
{
    private readonly IUnitOfWork
        _unitOfWork;

    private readonly IMinioService
        _minioService;

    public TeacherCompetitionTestService(
     IUnitOfWork unitOfWork,
     IMinioService minioService)
    {
        _unitOfWork =
            unitOfWork;

        _minioService =
            minioService;
    }

    public async Task<List<TeacherCompetitionTestDto>>
        GetMyCompetitionTestsAsync(
            int teacherUserId)
    {
        var tests =
            await _unitOfWork.CompetitionTests
                .QueryBasic()
                .AsNoTracking()
                .Where(x =>
                    x.TeacherUserId ==
                        teacherUserId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderByDescending(x =>
                    x.CreatedAt)
                .ToListAsync();

        return tests
            .Select(x =>
                new TeacherCompetitionTestDto
                {
                    CompetitionTestId =
                        x.CompetitionTestId,

                    Title = x.Title,

                    Description =
                        x.Description,

                    TestType =
                        x.TestType,

                    CourseType =
                        x.CourseType,

                    TotalPoints =
                        x.TotalPoints,

                    IsActive =
                        x.IsActive,

                    AssignmentCount =
                        x.Assignments.Count(a =>
                            a.IsActive &&
                            !a.IsDeleted),

                    CreatedAt =
                        x.CreatedAt
                })
            .ToList();
    }
    public async Task<CompetitionSourceFileDto>
    GetSourceFileAsync(
        int teacherUserId,
        int competitionTestId)
    {
        var test =
            await _unitOfWork
                .CompetitionTests
                .QueryBasic()
                .AsNoTracking()
                .Where(x =>
                    x.CompetitionTestId ==
                        competitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .Select(x => new
                {
                    x.SourceFileObjectName
                })
                .FirstOrDefaultAsync();

        if (test == null)
        {
            throw new Exception(
                "Competition test not found");
        }

        if (string.IsNullOrWhiteSpace(
            test.SourceFileObjectName))
        {
            throw new Exception(
                "Competition source file not found");
        }

        var fileUrl =
            await _minioService
                .GetPresignedUrlAsync(
                    test.SourceFileObjectName,
                    600);

        return new CompetitionSourceFileDto
        {
            FileUrl =
                fileUrl,

            FileName =
                Path.GetFileName(
                    test.SourceFileObjectName)
        };
    }
    public async Task<TeacherCompetitionTestDto>
        GetByIdAsync(
            int teacherUserId,
            int competitionTestId)
    {
        var test =
            await _unitOfWork.CompetitionTests
                .QueryBasic()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.CompetitionTestId ==
                        competitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (test == null)
        {
            throw new Exception(
                "Competition test not found");
        }

        return new TeacherCompetitionTestDto
        {
            CompetitionTestId =
                test.CompetitionTestId,

            Title =
                test.Title,

            Description =
                test.Description,

            TestType =
                test.TestType,

            CourseType =
                test.CourseType,

            TotalPoints =
                test.TotalPoints,

            IsActive =
                test.IsActive,

            AssignmentCount =
                test.Assignments.Count(a =>
                    a.IsActive &&
                    !a.IsDeleted),

            CreatedAt =
                test.CreatedAt
        };
    }

    public async Task UpdateAsync(
        int teacherUserId,
        int competitionTestId,
        UpdateCompetitionTestDto dto)
    {
        if (string.IsNullOrWhiteSpace(
            dto.Title))
        {
            throw new Exception(
                "Competition test title is required");
        }

        var test =
            await _unitOfWork.CompetitionTests
                .QueryBasic()
                .FirstOrDefaultAsync(x =>
                    x.CompetitionTestId ==
                        competitionTestId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (test == null)
        {
            throw new Exception(
                "Competition test not found");
        }

        test.Title =
            dto.Title.Trim();

        test.Description =
            string.IsNullOrWhiteSpace(
                dto.Description)
                ? null
                : dto.Description.Trim();

        test.UpdatedAt =
            DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(
        int teacherUserId,
        int competitionTestId)
    {
        await _unitOfWork.CompetitionTests
            .HardDeleteAsync(
                competitionTestId,
                teacherUserId);

        await _unitOfWork.SaveChangesAsync();
    }
}