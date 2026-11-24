using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class ExerciseStructureService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ExerciseService _exerciseService;
    public ExerciseStructureService(
    IUnitOfWork unitOfWork,
    ExerciseService exerciseService)
    {
        _unitOfWork = unitOfWork;
        _exerciseService = exerciseService;
    }

    public async Task<List<ExerciseCategoryDto>> GetCategoriesAsync()
    {
        return await _unitOfWork.ExerciseCategories
            .OrderBy(x => x.OrderIndex)
            .Select(x => new ExerciseCategoryDto
            {
                ExerciseCategoryId = x.ExerciseCategoryId,
                Name = x.Name,
                OrderIndex = x.OrderIndex
            })
            .ToListAsync();
    }
    public async Task DeleteSectionAsync(
    int teacherUserId,
    int exerciseSectionId)
    {
        /*
         * Chỉ kiểm tra section tồn tại.
         * Không Include Exercises để tránh EF tracking
         * toàn bộ Exercise trong section.
         */
        var sectionExists =
            await _unitOfWork.ExerciseSections
                .AsNoTracking()
                .AnyAsync(x =>
                    x.ExerciseSectionId ==
                        exerciseSectionId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (!sectionExists)
        {
            throw new Exception(
                "Exercise section not found");
        }

        /*
         * Chỉ lấy ExerciseId, không tracking entity.
         */
        var exerciseIds =
            await _unitOfWork.Exercises
                .QueryBasic()
                .Where(x =>
                    x.ExerciseSectionId ==
                        exerciseSectionId &&
                    x.TeacherUserId ==
                        teacherUserId)
                .Select(x => x.ExerciseId)
                .ToListAsync();

        /*
         * Hard delete từng Exercise cùng toàn bộ
         * dữ liệu liên quan.
         */
        foreach (var exerciseId in exerciseIds)
        {
            await _exerciseService
                .DeleteExerciseAsync(
                    teacherUserId,
                    exerciseId);
        }

        /*
         * Xóa section trực tiếp trong database.
         * Không attach entity và không SaveChanges,
         * nhờ đó không bị ảnh hưởng bởi ChangeTracker.
         */
        var sectionToDelete =
    await _unitOfWork.ExerciseSections
        .FirstOrDefaultAsync(x =>
            x.ExerciseSectionId ==
                exerciseSectionId &&
            x.TeacherUserId ==
                teacherUserId);

        if (sectionToDelete == null)
        {
            throw new Exception(
                "Exercise section not found");
        }

        _unitOfWork.RemoveExerciseSection(
            sectionToDelete);

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task UpdateSectionAsync(
    int teacherUserId,
    int exerciseSectionId,
    UpdateExerciseSectionDto dto)
    {
        var normalizedTitle =
            dto.Title?.Trim();

        if (string.IsNullOrWhiteSpace(
            normalizedTitle))
        {
            throw new Exception(
                "Section title is required");
        }

        var section =
            await _unitOfWork.ExerciseSections
                .FirstOrDefaultAsync(x =>
                    x.ExerciseSectionId ==
                        exerciseSectionId &&
                    x.TeacherUserId ==
                        teacherUserId);

        if (section == null)
        {
            throw new Exception(
                "Exercise section not found");
        }

        /*
         * Kiểm tra section hiện đang chứa
         * Exercise Set hay chưa.
         */
        var hasExercises =
            await _unitOfWork.Exercises
                .QueryBasic()
                .AnyAsync(x =>
                    x.ExerciseSectionId ==
                        exerciseSectionId &&
                    x.TeacherUserId ==
                        teacherUserId);

        var isChangingClassRoom =
            section.ClassRoomId !=
            dto.ClassRoomId;

        var isChangingCourse =
            section.CourseId !=
            dto.CourseId;

        var isChangingCategory =
            section.ExerciseCategoryId !=
            dto.ExerciseCategoryId;

        /*
         * Nếu section đã chứa Exercise Set thì
         * chỉ được phép đổi tên.
         *
         * Việc so sánh giá trị cũ và mới giúp
         * request vẫn được chấp nhận khi frontend
         * gửi lại ClassRoomId, CourseId và
         * ExerciseCategoryId hiện tại.
         */
        if (
            hasExercises &&
            (
                isChangingClassRoom ||
                isChangingCourse ||
                isChangingCategory
            )
        )
        {
            throw new Exception(
                "Chỉ có thể đổi tên. " +
                "Nếu muốn đổi lớp và kỹ năng thì " +
                "hãy xóa các bài tập bên trong chương."
            );
        }

        /*
         * Chỉ cần kiểm tra lớp và kỹ năng khi
         * section chưa có Exercise Set hoặc khi
         * request thực sự thay đổi các giá trị đó.
         */
        if (!hasExercises)
        {
            var categoryExists =
                await _unitOfWork
                    .ExerciseCategories
                    .AnyAsync(x =>
                        x.ExerciseCategoryId ==
                            dto.ExerciseCategoryId &&
                        x.IsActive);

            if (!categoryExists)
            {
                throw new Exception(
                    "Exercise category not found");
            }

            var classRoom =
                await _unitOfWork.ClassRooms
                    .Query()
                    .FirstOrDefaultAsync(x =>
                        x.ClassRoomId ==
                            dto.ClassRoomId &&
                        x.CourseId ==
                            dto.CourseId);

            if (classRoom == null)
            {
                throw new Exception(
                    "Class room not found");
            }

            /*
             * Kiểm tra giáo viên thực sự được
             * phân công vào lớp mới.
             */
            var teacherHasClass =
                await _unitOfWork
                    .TeacherRoleRepository
                    .Query()
                    .Where(x =>
                        x.UserId ==
                            teacherUserId)
                    .SelectMany(x =>
                        x.TeacherClasses)
                    .AnyAsync(x =>
                        x.ClassRoomId ==
                            dto.ClassRoomId);

            if (!teacherHasClass)
            {
                throw new Exception(
                    "Teacher is not assigned to this class");
            }

            section.CourseId =
                dto.CourseId;

            section.ClassRoomId =
                dto.ClassRoomId;

            section.ExerciseCategoryId =
                dto.ExerciseCategoryId;
        }

        section.Title =
            normalizedTitle;

        section.UpdatedAt =
            DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task<List<ExerciseSectionDto>> GetSectionsAsync(
    int teacherUserId,
    int? courseId,
    int? classRoomId,
    int? exerciseCategoryId)
    {
        var query = _unitOfWork.ExerciseSections
            .Where(x =>
                x.TeacherUserId == teacherUserId &&
                x.IsActive);

        if (courseId.HasValue)
        {
            query = query.Where(x =>
                x.CourseId == courseId.Value);
        }

        if (classRoomId.HasValue)
        {
            query = query.Where(x =>
                x.ClassRoomId == classRoomId.Value);
        }

        if (exerciseCategoryId.HasValue)
        {
            query = query.Where(x =>
                x.ExerciseCategoryId == exerciseCategoryId.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ExerciseSectionDto
            {
                ExerciseSectionId = x.ExerciseSectionId,
                Title = x.Title,
                CourseId = x.CourseId,
                CourseName = x.Course.Name,
                ClassRoomId = x.ClassRoomId,
                ClassRoomName = x.ClassRoom.ClassName,
                ExerciseCategoryId = x.ExerciseCategoryId,
                ExerciseCategoryName = x.Category.Name
            })
            .ToListAsync();
    }
    public async Task<int> CreateSectionAsync(
        int teacherUserId,
        CreateExerciseSectionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new Exception("Section title is required");
        }

        var categoryExists = await _unitOfWork.ExerciseCategories
            .AnyAsync(x =>
                x.ExerciseCategoryId == dto.ExerciseCategoryId &&
                x.IsActive);

        if (!categoryExists)
        {
            throw new Exception("Exercise category not found");
        }

        var classExists = await _unitOfWork.ClassRooms
            .Query()
            .AnyAsync(x =>
                x.ClassRoomId == dto.ClassRoomId &&
                x.CourseId == dto.CourseId);

        if (!classExists)
        {
            throw new Exception("Class room not found");
        }
        var teacherHasClass =
    await _unitOfWork
        .TeacherRoleRepository
        .Query()
        .Where(x =>
            x.UserId ==
                teacherUserId)
        .SelectMany(x =>
            x.TeacherClasses)
        .AnyAsync(x =>
            x.ClassRoomId ==
                dto.ClassRoomId);

        if (!teacherHasClass)
        {
            throw new Exception(
                "Teacher is not assigned to this class");
        }
        var section = new ExerciseSection
        {
            Title = dto.Title,
            CourseId = dto.CourseId,
            ClassRoomId = dto.ClassRoomId,
            ExerciseCategoryId = dto.ExerciseCategoryId,
            TeacherUserId = teacherUserId,
            IsActive = true
        };

        await _unitOfWork.AddExerciseSectionAsync(section);
        await _unitOfWork.SaveChangesAsync();

        return section.ExerciseSectionId;
    }
}