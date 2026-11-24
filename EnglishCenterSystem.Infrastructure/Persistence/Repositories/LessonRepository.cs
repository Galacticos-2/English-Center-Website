using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class LessonRepository : ILessonRepository
{
    private readonly AppDbContext _context;

    public LessonRepository(AppDbContext context)
    {
        _context = context;
    }
    public IQueryable<Lesson> QueryLessonsBasic()
    {
        return _context.Lessons
            .AsNoTracking();
    }
    public IQueryable<LessonCategory> QueryCategories()
    {
        return _context.LessonCategories
            .Include(x => x.Sections)
                .ThenInclude(x => x.Lessons)
                    .ThenInclude(x => x.Video)
            .Include(x => x.Sections)
                .ThenInclude(x => x.Lessons)
                    .ThenInclude(x => x.Document)
            .AsQueryable();
    }


    public IQueryable<LessonSection> QuerySections()
    {
        return _context.LessonSections
            .Include(x => x.Category)
            .Include(x => x.Course)
            .Include(x => x.ClassRoom)

            .Include(x => x.TeacherUser)
                .ThenInclude(x => x.TeacherRole)
                    .ThenInclude(x => x.TeacherClasses)

            .Include(x => x.Lessons)
                .ThenInclude(x => x.Video)

            .Include(x => x.Lessons)
                .ThenInclude(x => x.Document)

            .AsQueryable();
    }
    public IQueryable<Lesson> QueryLessons()
    {
        return _context.Lessons
            .Include(x => x.Section)
                .ThenInclude(x => x.Category)
            .Include(x => x.Video)
            .Include(x => x.Document)
            .Include(x => x.StudentProgresses)
            .AsQueryable();
    }
    public async Task<StudentLessonProgress?> GetProgressAsync(
    int studentRoleId,
    int lessonId)
    {
        return await _context.StudentLessonProgresses
            .FirstOrDefaultAsync(x =>
                x.StudentRoleId == studentRoleId &&
                x.LessonId == lessonId);
    }

    public async Task AddProgressAsync(
        StudentLessonProgress progress)
    {
        await _context.StudentLessonProgresses
            .AddAsync(progress);
    }
    public async Task<LessonSection?> GetSectionByIdAsync(int id)
    {
        return await _context.LessonSections
            .FirstOrDefaultAsync(x => x.LessonSectionId == id);
    }

    public async Task<Lesson?> GetLessonByIdAsync(int id)
    {
        return await _context.Lessons
            .Include(x => x.Video)
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.LessonId == id);
    }

    public async Task AddSectionAsync(LessonSection section)
    {
        await _context.LessonSections.AddAsync(section);
    }

    public async Task AddLessonAsync(Lesson lesson)
    {
        await _context.Lessons.AddAsync(lesson);
    }
    public async Task<List<LessonSection>> GetSectionsByTeacherUserIdAsync(
    int teacherUserId)
    {
        return await _context.LessonSections
            .Include(x => x.Lessons)
                .ThenInclude(x => x.Video)
            .Include(x => x.Lessons)
                .ThenInclude(x => x.Document)
            .Where(x => x.TeacherUserId == teacherUserId)
            .ToListAsync();
    }
    public async Task AddVideoAsync(LessonVideo video)
    {
        await _context.LessonVideos.AddAsync(video);
    }

    public async Task AddDocumentAsync(LessonDocument document)
    {
        await _context.LessonDocuments.AddAsync(document);
    }

    public void RemoveLesson(Lesson lesson)
    {
        _context.Lessons.Remove(lesson);
    }

    public void RemoveSection(LessonSection section)
    {
        _context.LessonSections.Remove(section);
    }
}