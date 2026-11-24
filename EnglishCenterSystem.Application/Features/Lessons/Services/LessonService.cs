using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnglishCenterSystem.Application.Features.Lessons.DTOs;
using System.Globalization;
using System.Text.RegularExpressions;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities;
using Hangfire;
using EnglishCenterSystem.Application
    .Features.Notifications.DTOs;

using EnglishCenterSystem.Application
    .Features.Notifications.Services;

using EnglishCenterSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishCenterSystem.Application.Features.Lessons.Services;

public class LessonService
{
    private readonly IUnitOfWork _uow;
    private readonly IMinioService _minioService;
    private readonly IPdfParserService _pdfParserService;
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly NotificationService
    _notificationService;
    private readonly IStudentGamificationService _studentGamificationService;

    private readonly ILogger<LessonService>
        _logger;
    public LessonService(
    IUnitOfWork uow,
    IMinioService minioService,
    IPdfParserService pdfParserService,
    IBackgroundJobClient backgroundJobClient,
    IVideoProcessingService videoProcessingService,
    NotificationService notificationService,
    IStudentGamificationService studentGamificationService,
    ILogger<LessonService> logger)
    {
        _uow = uow;
        _minioService = minioService;
        _pdfParserService = pdfParserService;
        _videoProcessingService =
            videoProcessingService;
        _backgroundJobClient =
            backgroundJobClient;
        _notificationService =
            notificationService;
        _studentGamificationService = studentGamificationService;
        _logger = logger;
    }

    public Task<List<LessonCategoryDto>> GetCategoriesAsync()
    {
        var categories = _uow.Lessons
            .QueryCategories()
            .Select(x => new LessonCategoryDto
            {
                LessonCategoryId = x.LessonCategoryId,
                Name = x.Name
            })
            .ToList();

        return Task.FromResult(categories);
    }

    public Task<List<LessonSectionDto>> GetSectionsAsync(
    int courseId,
    int classRoomId,
    int? categoryId)
    {
        var query = _uow.Lessons
            .QuerySections()
            .Where(x =>
                x.CourseId == courseId &&
                x.ClassRoomId == classRoomId);

        if (categoryId != null)
        {
            query = query.Where(x =>
                x.LessonCategoryId == categoryId);
        }

        var sections = query
            .ToList()
            .Select(x => new LessonSectionDto
            {
                LessonSectionId = x.LessonSectionId,
                Title = x.Title,
                CourseId = x.CourseId,
                ClassRoomId = x.ClassRoomId,
                LessonCategoryId = x.LessonCategoryId,
                CategoryName = x.Category.Name,
                ClassName = x.ClassRoom != null
        ? x.ClassRoom.ClassName
        : "",

                Lessons = x.Lessons
        .OrderBy(l => l.OrderIndex)
        .Select(l => new LessonDto
        {
            LessonId = l.LessonId,
            Title = l.Title,
            OrderIndex = l.OrderIndex,
            VideoUrl = l.Video?.HlsUrl,
            VideoProcessingStatus = l.Video?.ProcessingStatus,
            VideoErrorMessage = l.Video?.ErrorMessage,
            PdfUrl = l.Document?.PdfUrl,
            HtmlContent = l.Document?.HtmlContent
        })
        .ToList()
            })
            
            .ToList();

        return Task.FromResult(sections);
    }
    public Task<List<LessonSectionDto>> GetTeacherSectionsAsync(
    int userId,
    int courseId,
    int classRoomId,
    int? categoryId)
    {
        var canAccessClass = _uow.TeacherRoleRepository
            .Query()
            .Where(x => x.UserId == userId)
            .SelectMany(x => x.TeacherClasses)
            .Any(x =>
                x.ClassRoomId == classRoomId &&
                x.ClassRoom.CourseId == courseId);

        if (!canAccessClass)
        {
            return Task.FromResult(new List<LessonSectionDto>());
        }

        var query = _uow.Lessons
    .QuerySections()
    .Where(x =>
        x.CourseId == courseId &&
        x.ClassRoomId == classRoomId &&
        x.TeacherUserId == userId);

        if (categoryId != null)
        {
            query = query.Where(x =>
                x.LessonCategoryId == categoryId);
        }

        var sections = query
            .ToList()
            .Select(x => new LessonSectionDto
            {
                LessonSectionId = x.LessonSectionId,
                Title = x.Title,
                CourseId = x.CourseId,
                ClassRoomId = x.ClassRoomId,
                LessonCategoryId = x.LessonCategoryId,
                CategoryName = x.Category.Name,
                ClassName = x.ClassRoom != null ? x.ClassRoom.ClassName : "",

                Lessons = x.Lessons
                    .OrderBy(l => l.OrderIndex)
                    .Select(l => new LessonDto
                    {
                        LessonId = l.LessonId,
                        Title = l.Title,
                        OrderIndex = l.OrderIndex,
                        VideoUrl = l.Video?.HlsUrl,
                        VideoProcessingStatus = l.Video?.ProcessingStatus,
                        VideoErrorMessage = l.Video?.ErrorMessage,
                        PdfUrl = l.Document?.PdfUrl,
                        HtmlContent = l.Document?.HtmlContent
                    })
                    .ToList()
            })
            .ToList();

        return Task.FromResult(sections);
    }
    public Task<List<TeacherClassDto>> GetTeacherClassesAsync(int userId)
    {
        var classes = _uow.TeacherRoleRepository
            .Query()
            .Where(x => x.UserId == userId)
            .SelectMany(x => x.TeacherClasses)
            .Select(x => new TeacherClassDto
            {
                CourseId = x.ClassRoom.CourseId,
                CourseName = x.ClassRoom.Course.Name,
                ClassRoomId = x.ClassRoomId,
                ClassName = x.ClassRoom.ClassName
            })
            .ToList();

        return Task.FromResult(classes);
    }
    public Task<List<LessonSectionDto>> GetMySectionsAsync(
    int userId,
    int? categoryId)
    {
        var studentRole = _uow.StudentRoleRepository
    .Query()
    .Where(x => x.UserId == userId)
    .Select(x => new
    {
        x.StudentRoleId,
        x.ClassRoomId,
        CourseId = x.ClassRoom != null
            ? x.ClassRoom.CourseId
            : 0
    })
    .FirstOrDefault();

        if (studentRole == null || studentRole.ClassRoomId == null)
        {
            throw new Exception("Student has not been assigned to a class");
        }
        var completedLessonIds = _uow.Lessons
    .QueryLessons()
    .SelectMany(x => x.StudentProgresses)
    .Where(x =>
        x.StudentRoleId == studentRole.StudentRoleId &&
        x.IsCompleted)
    .Select(x => x.LessonId)
    .ToList();
        var query = _uow.Lessons
    .QuerySections()
    .Where(x =>
        x.CourseId == studentRole.CourseId &&
        x.ClassRoomId == studentRole.ClassRoomId &&
        x.TeacherUserId != null &&
        x.TeacherUser != null &&
        x.TeacherUser.IsActive &&
        !x.TeacherUser.IsDeleted &&
        x.TeacherUser.TeacherRole != null &&
        x.TeacherUser.TeacherRole.TeacherClasses.Any(tc =>
            tc.ClassRoomId == studentRole.ClassRoomId));

        if (categoryId != null)
        {
            query = query.Where(x =>
                x.LessonCategoryId == categoryId);
        }

        var sections = query
            .ToList()
            .Select(x => new LessonSectionDto
            {
                LessonSectionId = x.LessonSectionId,
                Title = x.Title,
                CourseId = x.CourseId,
                ClassRoomId = x.ClassRoomId,
                LessonCategoryId = x.LessonCategoryId,
                CategoryName = x.Category.Name,
                ClassName = x.ClassRoom != null
                    ? x.ClassRoom.ClassName
                    : "",

                Lessons = x.Lessons
    .OrderBy(l => l.OrderIndex)
    .Select(l => new LessonDto
    {
        LessonId = l.LessonId,
        Title = l.Title,
        OrderIndex = l.OrderIndex,
        VideoUrl = l.Video?.HlsUrl,
        VideoProcessingStatus = l.Video?.ProcessingStatus,
        VideoErrorMessage = l.Video?.ErrorMessage,
        PdfUrl = l.Document?.PdfUrl,
        HtmlContent = l.Document?.HtmlContent,
        IsCompleted = completedLessonIds.Contains(l.LessonId)
    })
    .ToList()
            })
            .ToList();

        return Task.FromResult(sections);
    }
    public async Task CreateSectionAsync(
    int teacherUserId,
    CreateLessonSectionDto dto)
    {
        var classRoom = await _uow.ClassRooms.GetByIdAsync(dto.ClassRoomId);

        if (classRoom == null)
        {
            throw new Exception("Class room not found");
        }

        if (classRoom.CourseId != dto.CourseId)
        {
            throw new Exception("Class does not belong to this course");
        }
        var canAccessClass = _uow.TeacherRoleRepository
    .Query()
    .Where(x => x.UserId == teacherUserId)
    .SelectMany(x => x.TeacherClasses)
    .Any(x => x.ClassRoomId == dto.ClassRoomId);

        if (!canAccessClass)
        {
            throw new Exception("Teacher is not assigned to this class");
        }
        var section = new LessonSection
        {
            Title = dto.Title,
            CourseId = dto.CourseId,
            ClassRoomId = dto.ClassRoomId,
            LessonCategoryId = dto.LessonCategoryId,
            TeacherUserId = teacherUserId
        };

        await _uow.Lessons.AddSectionAsync(section);
        await _uow.SaveChangesAsync();
    }

    public async Task<int> CreateLessonAsync(
    int teacherUserId,
    CreateLessonDto dto)
    {
        if (string.IsNullOrWhiteSpace(
            dto.Title))
        {
            throw new Exception(
                "Lesson title is required");
        }

        var section =
            await _uow.Lessons
                .QuerySections()
                .FirstOrDefaultAsync(x =>
                    x.LessonSectionId ==
                        dto.LessonSectionId);

        if (section == null)
        {
            throw new Exception(
                "Lesson section not found");
        }

        if (section.TeacherUserId !=
            teacherUserId)
        {
            throw new Exception(
                "You cannot create a lesson in this section");
        }

        if (section.ClassRoomId == null)
        {
            throw new Exception(
                "Lesson section has not been assigned to a class");
        }

        var teacherHasClass =
            await _uow.TeacherRoleRepository
                .Query()
                .Where(x =>
                    x.UserId ==
                        teacherUserId)
                .SelectMany(x =>
                    x.TeacherClasses)
                .AnyAsync(x =>
                    x.ClassRoomId ==
                        section.ClassRoomId.Value);

        if (!teacherHasClass)
        {
            throw new Exception(
                "Teacher is not assigned to this class");
        }

        var lesson =
            new Lesson
            {
                Title =
                    dto.Title.Trim(),

                LessonSectionId =
                    dto.LessonSectionId,

                OrderIndex =
                    dto.OrderIndex
            };

        await _uow.Lessons
            .AddLessonAsync(lesson);

        await _uow.SaveChangesAsync();

        await CreateLessonNotificationSafelyAsync(
            teacherUserId,
            section.ClassRoomId.Value,
            section.ClassRoom?.ClassName,
            lesson);

        return lesson.LessonId;
    }
    private async Task
    CreateLessonNotificationSafelyAsync(
        int teacherUserId,
        int classRoomId,
        string? classRoomName,
        Lesson lesson)
    {
        try
        {
            var resolvedClassName =
                string.IsNullOrWhiteSpace(
                    classRoomName)
                    ? "the class"
                    : classRoomName;

            await _notificationService
                .CreateForClassAsync(
                    new CreateClassNotificationDto
                    {
                        CreatedByUserId =
                            teacherUserId,

                        ClassRoomId =
                            classRoomId,

                        Type =
                            NotificationType
                                .LessonCreated,

                        RelatedEntityId =
                            lesson.LessonId,

                        Title =
                            "New lesson added",

                        Message =
                            $"Lesson \"{lesson.Title}\" " +
                            $"has been added to class " +
                            $"{resolvedClassName}.",

                        TeacherActionUrl =
                            "/teacher/lessons",

                        StudentActionUrl =
                            "/student/learning"
                    });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not create lesson notification for LessonId {LessonId}",
                lesson.LessonId);
        }
    }

    public async Task UploadDocumentAsync(
    int teacherUserId,
    int lessonId,
    Stream pdfStream,
    string fileName,
    string contentType)
    {
        var lesson =
            await GetTeacherLessonAsync(
                teacherUserId,
                lessonId);
        var lessonFolderName =
    BuildLessonFolderName(
        lesson.Title);
        var oldPdfUrl =
            lesson.Document?.PdfUrl;

        var safeFileName =
            Path.GetFileName(fileName);

        var tempFolder =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "temp",
                "lessons",
                "documents");

        Directory.CreateDirectory(tempFolder);

        var tempPath =
            Path.Combine(
                tempFolder,
                $"{Guid.NewGuid()}_{safeFileName}");

        string? newObjectName = null;

        var databaseSaved = false;

        try
        {
            await using (var fileStream =
                new FileStream(
                    tempPath,
                    FileMode.Create,
                    FileAccess.Write))
            {
                await pdfStream.CopyToAsync(fileStream);
            }

            newObjectName =
    $"lessons/documents/" +
    $"{lessonFolderName}/" +
    $"{Guid.NewGuid()}_{safeFileName}";

            string newPdfUrl;

            await using (var uploadStream =
                new FileStream(
                    tempPath,
                    FileMode.Open,
                    FileAccess.Read))
            {
                newPdfUrl =
                    await _minioService.UploadFileAsync(
                        uploadStream,
                        newObjectName,
                        contentType);
            }

            if (lesson.Document == null)
            {
                await _uow.Lessons.AddDocumentAsync(
                    new LessonDocument
                    {
                        LessonId = lessonId,
                        PdfUrl = newPdfUrl,
                        HtmlContent = ""
                    });
            }
            else
            {
                lesson.Document.PdfUrl = newPdfUrl;
                lesson.Document.HtmlContent = "";
            }

            await _uow.SaveChangesAsync();

            databaseSaved = true;

            if (!string.IsNullOrWhiteSpace(oldPdfUrl))
            {
                try
                {
                    await _minioService
                        .DeleteFileByUrlAsync(oldPdfUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Could not delete old PDF for LessonId {LessonId}",
                        lessonId);
                }
            }
        }
        catch
        {
            if (!databaseSaved &&
                !string.IsNullOrWhiteSpace(newObjectName))
            {
                try
                {
                    await _minioService
                        .DeleteFileAsync(newObjectName);
                }
                catch
                {
                }
            }

            throw;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public async Task UploadVideoAsync(
    int teacherUserId,
    int lessonId,
    Stream videoStream,
    string fileName,
    string contentType)
    {
        var lesson =
            await GetTeacherLessonAsync(
                teacherUserId,
                lessonId);
        var lessonFolderName =
    BuildLessonFolderName(
        lesson.Title);
        if (lesson.Video?.ProcessingStatus ==
            "Processing")
        {
            throw new Exception(
                "Video is currently being processed.");
        }

        var oldOriginalUrl =
            lesson.Video?.OriginalUrl;

        var oldHlsUrl =
            lesson.Video?.HlsUrl;

        var tempRoot =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "temp",
                "videos");

        Directory.CreateDirectory(
            tempRoot);

        var videoId =
            Guid.NewGuid().ToString();

        var safeFileName =
            Path.GetFileName(fileName);

        var inputPath =
            Path.Combine(
                tempRoot,
                $"{videoId}_{safeFileName}");

        var originalObjectName =
    $"lessons/videos/original/" +
    $"{lessonFolderName}/" +
    $"{videoId}_{safeFileName}";

        string? originalUrl = null;

        var newOriginalDeleted = false;
        var jobEnqueued = false;
        var rollbackDatabaseFailed = false;

        LessonVideo? videoEntity = null;

        try
        {
            await using (var fileStream =
                new FileStream(
                    inputPath,
                    FileMode.Create,
                    FileAccess.Write))
            {
                await videoStream
                    .CopyToAsync(fileStream);
            }

            await using (var uploadStream =
                new FileStream(
                    inputPath,
                    FileMode.Open,
                    FileAccess.Read))
            {
                originalUrl =
                    await _minioService
                        .UploadFileAsync(
                            uploadStream,
                            originalObjectName,
                            contentType);
            }

            if (lesson.Video == null)
            {
                videoEntity =
                    new LessonVideo
                    {
                        LessonId = lessonId,
                        OriginalUrl = originalUrl,
                        HlsUrl = "",
                        ThumbnailUrl = "",
                        DurationSeconds = 0,
                        ProcessingStatus =
                            "Processing",
                        ErrorMessage = null
                    };

                await _uow.Lessons
                    .AddVideoAsync(
                        videoEntity);
            }
            else
            {
                videoEntity =
                    lesson.Video;

                videoEntity.OriginalUrl =
                    originalUrl;

                /*
                 * Giữ HlsUrl cũ cho đến khi
                 * video mới xử lý HLS thành công.
                 */
                videoEntity.ProcessingStatus =
                    "Processing";

                videoEntity.ErrorMessage =
                    null;
            }

            await _uow.SaveChangesAsync();

            try
            {
                _backgroundJobClient
                    .Enqueue<VideoJobService>(
                        x => x.ProcessVideoAsync(
    lessonId,
    lessonFolderName,
    videoId,
    inputPath,
    originalUrl,
    oldOriginalUrl,
    oldHlsUrl));

                jobEnqueued = true;
            }
            catch (Exception enqueueException)
            {
                /*
                 * Khôi phục trạng thái video cũ
                 * trong database.
                 */
                videoEntity.OriginalUrl =
                    oldOriginalUrl ?? "";

                videoEntity.HlsUrl =
                    oldHlsUrl ?? "";

                videoEntity.ProcessingStatus =
                    !string.IsNullOrWhiteSpace(
                        oldHlsUrl)
                        ? "Ready"
                        : "Failed";

                videoEntity.ErrorMessage =
                    "Could not enqueue video processing job.";

                try
                {
                    await _uow.SaveChangesAsync();
                }
                catch (Exception rollbackException)
                {
                    rollbackDatabaseFailed = true;

                    throw new AggregateException(
                        "Could not enqueue video processing job " +
                        "and could not restore the previous video state.",
                        enqueueException,
                        rollbackException);
                }

                /*
                 * Chỉ xóa original mới sau khi
                 * rollback database thành công.
                 */
                try
                {
                    await _minioService
                        .DeleteFileAsync(
                            originalObjectName);

                    newOriginalDeleted = true;
                }
                catch
                {
                    /*
                     * DB đã rollback thành công.
                     * File mới còn lại chỉ là file rác,
                     * không làm hỏng dữ liệu hiện tại.
                     */
                }

                throw;
            }
        }
        catch
        {
            /*
             * Nếu job chưa được enqueue và original mới
             * chưa bị xóa, thử xóa nó khỏi MinIO.
             *
             * Trường hợp rollback DB thất bại được ném bằng
             * AggregateException ở trên. Khi đó videoEntity
             * đã được đổi lại trong EF tracking, nhưng DB có
             * thể vẫn đang trỏ đến file mới.
             *
             * Vì vậy cần kiểm tra trạng thái entity trước khi xóa.
             */


            if (!jobEnqueued &&
    !newOriginalDeleted &&
    !rollbackDatabaseFailed &&
    !string.IsNullOrWhiteSpace(originalUrl))
            {
                try
                {
                    await _minioService
                        .DeleteFileAsync(
                            originalObjectName);

                    newOriginalDeleted = true;
                }
                catch
                {
                    // Không che lỗi chính.
                }
            }

            /*
             * Hangfire chưa nhận job thì file tạm
             * không còn được sử dụng.
             */
            if (!jobEnqueued)
            {
                try
                {
                    if (File.Exists(inputPath))
                    {
                        File.Delete(inputPath);
                    }
                }
                catch
                {
                    // Không che lỗi chính.
                }
            }

            throw;
        }
    }
    private static string BuildLessonFolderName(
    string lessonTitle)
    {
        if (string.IsNullOrWhiteSpace(lessonTitle))
        {
            return "untitled-lesson";
        }

        var normalized =
            lessonTitle.Trim()
                .Normalize(
                    NormalizationForm.FormD);

        var builder =
            new StringBuilder();

        foreach (var character in normalized)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character);

            if (category !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var result =
            builder.ToString()
                .Normalize(
                    NormalizationForm.FormC)
                .ToLowerInvariant();

        result = result
            .Replace('đ', 'd');

        result = Regex.Replace(
            result,
            @"[^a-z0-9]+",
            "-");

        result = result.Trim('-');

        return string.IsNullOrWhiteSpace(result)
            ? "untitled-lesson"
            : result;
    }
    private static string?
GetFolderPrefixFromUrl(
    string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(
            fileUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(
            fileUrl,
            UriKind.Absolute,
            out var uri))
        {
            return null;
        }

        var path =
            Uri.UnescapeDataString(
                uri.AbsolutePath)
            .TrimStart('/');

        const string bucketPrefix =
            "eduenglish/";

        if (!path.StartsWith(
            bucketPrefix,
            StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var objectName =
            path[bucketPrefix.Length..];

        var lastSlash =
            objectName.LastIndexOf('/');

        if (lastSlash < 0)
        {
            return null;
        }

        return objectName[
            ..(lastSlash + 1)];
    }
    private async Task
DeleteLessonMediaSafelyAsync(
    int lessonId,
    string? pdfUrl,
    string? originalUrl,
    string? hlsUrl)
    {
        if (!string.IsNullOrWhiteSpace(
            pdfUrl))
        {
            try
            {
                await _minioService
                    .DeleteFileByUrlAsync(
                        pdfUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not delete PDF for LessonId {LessonId}",
                    lessonId);
            }
        }

        if (!string.IsNullOrWhiteSpace(
            originalUrl))
        {
            try
            {
                await _minioService
                    .DeleteFileByUrlAsync(
                        originalUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not delete original video for LessonId {LessonId}",
                    lessonId);
            }
        }

        var hlsPrefix =
            GetFolderPrefixFromUrl(
                hlsUrl);

        if (!string.IsNullOrWhiteSpace(
            hlsPrefix))
        {
            try
            {
                await _minioService
                    .DeleteFolderAsync(
                        hlsPrefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not delete HLS folder {Prefix} for LessonId {LessonId}",
                    hlsPrefix,
                    lessonId);
            }
        }
    }

    public async Task DeleteLessonAsync(
     int teacherUserId,
     int lessonId)
    {
        var lesson =
            await GetTeacherLessonAsync(
                teacherUserId,
                lessonId);

        /*
         * Lưu URL trước khi xóa entity khỏi DB.
         */
        var pdfUrl =
            lesson.Document?.PdfUrl;

        var originalUrl =
            lesson.Video?.OriginalUrl;

        var hlsUrl =
            lesson.Video?.HlsUrl;

        _uow.Lessons.RemoveLesson(
            lesson);

        await _uow.SaveChangesAsync();

        await DeleteLessonMediaSafelyAsync(
            lessonId,
            pdfUrl,
            originalUrl,
            hlsUrl);
    }

    public async Task UpdateLessonAsync(
    int teacherUserId,
    int lessonId,
    UpdateLessonDto dto)
    {
        var lesson =
            await GetTeacherLessonAsync(
                teacherUserId,
                lessonId);

        lesson.Title = dto.Title;
        lesson.OrderIndex = dto.OrderIndex;

        await _uow.SaveChangesAsync();
    }
    public async Task UpdateSectionAsync(
    int teacherUserId,
    int sectionId,
    UpdateLessonSectionDto dto)
    {
        var section =
            await GetTeacherSectionAsync(
                teacherUserId,
                sectionId);

        section.Title = dto.Title;

        await _uow.SaveChangesAsync();
    }
    public async Task DeleteSectionAsync(
    int teacherUserId,
    int sectionId)
    {
        var section =
            await GetTeacherSectionAsync(
                teacherUserId,
                sectionId);

        /*
         * Phải lưu toàn bộ URL trước khi xóa section,
         * vì cascade delete có thể xóa các bản ghi media.
         */
        var lessonMedia =
            await _uow.Lessons
                .QueryLessons()
                .Where(x =>
                    x.LessonSectionId ==
                    sectionId)
                .Select(x => new
                {
                    x.LessonId,

                    PdfUrl =
                        x.Document != null
                            ? x.Document.PdfUrl
                            : null,

                    OriginalUrl =
                        x.Video != null
                            ? x.Video.OriginalUrl
                            : null,

                    HlsUrl =
                        x.Video != null
                            ? x.Video.HlsUrl
                            : null
                })
                .ToListAsync();

        _uow.Lessons.RemoveSection(
            section);

        await _uow.SaveChangesAsync();

        foreach (var media in lessonMedia)
        {
            await DeleteLessonMediaSafelyAsync(
                media.LessonId,
                media.PdfUrl,
                media.OriginalUrl,
                media.HlsUrl);
        }
    }
    public async Task<List<LessonCategoryStatisticDto>>
    GetTeacherLessonStatisticsAsync(
        int userId,
        int courseId,
        int classRoomId)
    {
        var canAccessClass =
            await _uow.TeacherRoleRepository
                .Query()
                .Where(x => x.UserId == userId)
                .SelectMany(x => x.TeacherClasses)
                .AnyAsync(x =>
                    x.ClassRoomId == classRoomId &&
                    x.ClassRoom.CourseId == courseId);

        if (!canAccessClass)
        {
            return new List<LessonCategoryStatisticDto>();
        }

        var categories =
            await _uow.Lessons
                .QueryCategories()
                .OrderBy(x => x.LessonCategoryId)
                .Select(x => new
                {
                    x.LessonCategoryId,
                    x.Name
                })
                .ToListAsync();

        var lessonStatistics =
            await _uow.Lessons
                .QuerySections()
                .Where(x =>
                    x.CourseId == courseId &&
                    x.ClassRoomId == classRoomId &&
                    x.TeacherUserId == userId)
                .GroupBy(x => new
                {
                    x.LessonCategoryId,
                    CategoryName = x.Category.Name
                })
                .Select(group => new
                {
                    group.Key.LessonCategoryId,
                    group.Key.CategoryName,
                    SectionCount = group.Count(),
                    LessonCount = group.Sum(
                        section => section.Lessons.Count)
                })
                .ToListAsync();

        var result = categories
            .Select(category =>
            {
                var statistic = lessonStatistics
                    .FirstOrDefault(x =>
                        x.LessonCategoryId ==
                        category.LessonCategoryId);

                return new LessonCategoryStatisticDto
                {
                    LessonCategoryId =
                        category.LessonCategoryId,

                    CategoryName =
                        category.Name,

                    SectionCount =
                        statistic?.SectionCount ?? 0,

                    LessonCount =
                        statistic?.LessonCount ?? 0
                };
            })
            .ToList();

        return result;
    }
    public async Task MarkLessonCompletedAsync(
    int userId,
    int lessonId)
    {
        var studentRole = _uow.StudentRoleRepository
            .Query()
            .FirstOrDefault(x => x.UserId == userId);

        if (studentRole == null)
        {
            throw new Exception("Student role not found");
        }

        if (studentRole.ClassRoomId == null)
        {
            throw new Exception("Student has not been assigned to a class");
        }

        var lesson = await _uow.Lessons
            .QueryLessons()
            .FirstOrDefaultAsync(x =>
                x.LessonId == lessonId);

        if (lesson == null)
        {
            throw new Exception("Lesson not found");
        }

        if (lesson.Section.ClassRoomId !=
            studentRole.ClassRoomId)
        {
            throw new Exception(
                "You cannot access this lesson");
        }

        var progress = await _uow.Lessons
            .GetProgressAsync(
                studentRole.StudentRoleId,
                lessonId);

        var becameCompleted = progress?.IsCompleted != true;

        if (progress == null)
        {
            progress = new StudentLessonProgress
            {
                StudentRoleId = studentRole.StudentRoleId,
                LessonId = lessonId,
                IsCompleted = true
            };

            await _uow.Lessons.AddProgressAsync(progress);
        }
        else
        {
            progress.IsCompleted = true;
        }

        await _uow.SaveChangesAsync();

        if (becameCompleted)
        {
            await _studentGamificationService.AwardLessonCompletionAsync(studentRole.StudentRoleId, lessonId);
        }
    }

    private async Task<Lesson> GetTeacherLessonAsync(
        int teacherUserId,
        int lessonId)
    {
        var lesson =
            await _uow.Lessons
                .QueryLessons()
                .FirstOrDefaultAsync(x =>
                    x.LessonId == lessonId);

        if (lesson == null)
        {
            throw new Exception("Lesson not found");
        }

        await EnsureTeacherCanAccessSectionAsync(
            teacherUserId,
            lesson.Section);

        return lesson;
    }

    private async Task<LessonSection> GetTeacherSectionAsync(
        int teacherUserId,
        int sectionId)
    {
        var section =
            await _uow.Lessons
                .QuerySections()
                .FirstOrDefaultAsync(x =>
                    x.LessonSectionId == sectionId);

        if (section == null)
        {
            throw new Exception("Lesson section not found");
        }

        await EnsureTeacherCanAccessSectionAsync(
            teacherUserId,
            section);

        return section;
    }

    private async Task EnsureTeacherCanAccessSectionAsync(
        int teacherUserId,
        LessonSection section)
    {
        if (section.TeacherUserId != teacherUserId)
        {
            throw new Exception(
                "You cannot access this lesson section");
        }

        if (section.ClassRoomId == null)
        {
            throw new Exception(
                "Lesson section has not been assigned to a class");
        }

        var teacherHasClass =
            await _uow.TeacherRoleRepository
                .Query()
                .Where(x =>
                    x.UserId == teacherUserId &&
                    !x.IsDeleted)
                .SelectMany(x => x.TeacherClasses)
                .AnyAsync(x =>
                    x.ClassRoomId == section.ClassRoomId.Value);

        if (!teacherHasClass)
        {
            throw new Exception(
                "Teacher is not assigned to this class");
        }
    }
}
