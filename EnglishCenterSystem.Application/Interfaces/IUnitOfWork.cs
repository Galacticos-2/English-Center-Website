using System.Collections.Generic;
using EnglishCenterSystem.Domain.Entities;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Entities.Tests;
namespace EnglishCenterSystem.Application.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users
    {
        get;
    }

    ICourseRepository Courses
    {
        get;
    }

    IStudentRoleRepository
    StudentRoleRepository
    {
        get;
    }
    Task HardDeleteStudentAsync(int userId);
    IWritingSubmissionRepository WritingSubmissions { get; }
    ISpeakingTaskRepository SpeakingTasks { get; }
    IListeningTaskRepository ListeningTasks { get; }
    IReadingTaskRepository ReadingTasks { get; }
    ILessonRepository Lessons { get; }
    ITestRepository Tests { get; }

    ITestAssignmentRepository TestAssignments { get; }

    ITestAttemptRepository TestAttempts { get; }
    IClassRoomRepository ClassRooms
    {
        get;
    }
    IWritingTaskRepository WritingTasks { get; }
    IQueryable<ExerciseCategory> ExerciseCategories { get; }

    IQueryable<ExerciseSection> ExerciseSections { get; }
    void RemoveExerciseSection(ExerciseSection section);
    Task AddExerciseSectionAsync(ExerciseSection section);
    IExerciseAttemptRepository ExerciseAttempts
    {
        get;
    }
    IExerciseAssignmentRepository ExerciseAssignments
    {
        get;
    }
    IQueryable<TeacherRole>
    TeacherRoles
    {
        get;
    }
    ITeacherRoleRepository
TeacherRoleRepository
    {
        get;
    }
    IExerciseRepository Exercises
    {
        get;
    }
    ICompetitionTestRepository CompetitionTests { get; }

    ICompetitionTestAssignmentRepository
        CompetitionTestAssignments
    { get; }

    ICompetitionTestAttemptRepository
        CompetitionTestAttempts
    { get; }

    ICompetitionResultRepository
        CompetitionResults
    { get; }
    IWritingScoreRepository WritingScores { get; }
    IQueryable<StudentRole>

    StudentRoles
    {
        get;
    }
    INotificationRepository Notifications
    {
        get;
    }
    IQueryable<EnglishCenterSystem.Domain.Entities.Gamification.StudentExperience>
        StudentExperiences { get; }
    IQueryable<OnlineClassroom> OnlineClassrooms { get; }
    Task AddOnlineClassroomAsync(OnlineClassroom onlineClassroom);
    IQueryable<VocabularyFlashcard> VocabularyFlashcards { get; }
    Task AddVocabularyFlashcardAsync(VocabularyFlashcard flashcard);
    IQueryable<StudentFlashcardProgress> StudentFlashcardProgresses { get; }
    Task AddStudentFlashcardProgressAsync(StudentFlashcardProgress progress);
    void RemoveStudentFlashcardProgresses(IEnumerable<StudentFlashcardProgress> progresses);

    IQuestionRepository Questions
    {
        get;
    }
    IQuestionAssignmentRepository QuestionAssignments
    {
        get;
    }
    IQuestionMapRepository QuestionMaps { get; }
    IQueryable<StudentQuestionGroupProgress> StudentQuestionGroupProgresses { get; }
    Task<int> SaveChangesAsync();
}
