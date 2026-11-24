using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ILessonRepository
{
    IQueryable<LessonCategory> QueryCategories();

    IQueryable<LessonSection> QuerySections();
    IQueryable<Lesson> QueryLessonsBasic();
    IQueryable<Lesson> QueryLessons();

    Task<LessonSection?> GetSectionByIdAsync(int id);

    Task<Lesson?> GetLessonByIdAsync(int id);

    Task AddSectionAsync(LessonSection section);

    Task AddLessonAsync(Lesson lesson);
    Task<List<LessonSection>> GetSectionsByTeacherUserIdAsync(int teacherUserId);

    Task AddVideoAsync(LessonVideo video);

    Task AddDocumentAsync(LessonDocument document);

    void RemoveLesson(Lesson lesson);

    void RemoveSection(LessonSection section);
    Task<StudentLessonProgress?> GetProgressAsync(
    int studentRoleId,
    int lessonId);

    Task AddProgressAsync(StudentLessonProgress progress);
}