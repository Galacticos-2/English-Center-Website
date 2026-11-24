using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using EnglishCenterSystem.Domain.Entities.Listening;
using EnglishCenterSystem.Domain.Entities.Exercises;
namespace EnglishCenterSystem.Infrastructure.Persistence.Context;
using EnglishCenterSystem.Domain.Entities.Speaking;
using EnglishCenterSystem.Domain.Entities.BackgroundJobs;
using EnglishCenterSystem.Domain.Entities.Reading;
using EnglishCenterSystem.Domain.Entities.Writing;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Domain.Entities.Notifications;
using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Domain.Entities.Gamification;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<TeacherClass>
TeacherClasses
    => Set<TeacherClass>();
    public DbSet<ExerciseCategory> ExerciseCategories
    => Set<ExerciseCategory>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestAssignment> TestAssignments => Set<TestAssignment>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<TestAnswer> TestAnswers => Set<TestAnswer>();
    public DbSet<Notification> Notifications
    => Set<Notification>();

    public DbSet<UserNotification> UserNotifications
        => Set<UserNotification>();
    public DbSet<TestListeningTask> TestListeningTasks => Set<TestListeningTask>();
    public DbSet<TestReadingTask> TestReadingTasks => Set<TestReadingTask>();
    public DbSet<TestWritingTask> TestWritingTasks => Set<TestWritingTask>();
    public DbSet<TestSpeakingTask> TestSpeakingTasks => Set<TestSpeakingTask>();
    public DbSet<ExerciseListeningTask> ExerciseListeningTasks
    => Set<ExerciseListeningTask>();
    public DbSet<CompetitionTest> CompetitionTests
    => Set<CompetitionTest>();

    public DbSet<CompetitionTestListeningTask>
        CompetitionTestListeningTasks
        => Set<CompetitionTestListeningTask>();

    public DbSet<CompetitionTestReadingTask>
        CompetitionTestReadingTasks
        => Set<CompetitionTestReadingTask>();

    public DbSet<CompetitionTestWritingTask>
        CompetitionTestWritingTasks
        => Set<CompetitionTestWritingTask>();

    public DbSet<CompetitionTestSpeakingTask>
        CompetitionTestSpeakingTasks
        => Set<CompetitionTestSpeakingTask>();

    public DbSet<CompetitionTestAssignment>
        CompetitionTestAssignments
        => Set<CompetitionTestAssignment>();

    public DbSet<CompetitionTestAttempt>
        CompetitionTestAttempts
        => Set<CompetitionTestAttempt>();

    public DbSet<CompetitionTestAnswer>
        CompetitionTestAnswers
        => Set<CompetitionTestAnswer>();

    public DbSet<CompetitionResult> CompetitionResults
        => Set<CompetitionResult>();
    public DbSet<StudentExperience> StudentExperiences => Set<StudentExperience>();
    public DbSet<StudentExpTransaction> StudentExpTransactions => Set<StudentExpTransaction>();
    public DbSet<StudentAchievement> StudentAchievements => Set<StudentAchievement>();
    public DbSet<OnlineClassroom> OnlineClassrooms => Set<OnlineClassroom>();
    public DbSet<VocabularyFlashcard> VocabularyFlashcards => Set<VocabularyFlashcard>();
    public DbSet<StudentFlashcardProgress> StudentFlashcardProgresses => Set<StudentFlashcardProgress>();
    public DbSet<ExerciseSection> ExerciseSections
        => Set<ExerciseSection>();
    public DbSet<ReadingPassageImage>
     ReadingPassageImages
         => Set<ReadingPassageImage>();
    public DbSet<ExerciseAttempt> ExerciseAttempts
    => Set<ExerciseAttempt>();
    public DbSet<SpeakingTask> SpeakingTasks
    => Set<SpeakingTask>();
    public DbSet<WritingTask> WritingTasks
    => Set<WritingTask>();

    public DbSet<WritingSubmission> WritingSubmissions
        => Set<WritingSubmission>();

    public DbSet<WritingScore> WritingScores
        => Set<WritingScore>();

    public DbSet<ExerciseWritingTask> ExerciseWritingTasks
        => Set<ExerciseWritingTask>();
    public DbSet<SystemBackgroundJob> SystemBackgroundJobs
    => Set<SystemBackgroundJob>();
    public DbSet<ExerciseSpeakingTask> ExerciseSpeakingTasks
        => Set<ExerciseSpeakingTask>();
    public DbSet<ExerciseAssignment> ExerciseAssignments
    => Set<ExerciseAssignment>();
    public DbSet<ExerciseAnswer> ExerciseAnswers
    => Set<ExerciseAnswer>();
    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ListeningTask> ListeningTasks
    => Set<ListeningTask>();

    public DbSet<ListeningQuestion> ListeningQuestions
        => Set<ListeningQuestion>();

    public DbSet<ListeningOption> ListeningOptions
        => Set<ListeningOption>();
    public DbSet<StudentRole> StudentRoles => Set<StudentRole>();

    public DbSet<TeacherRole> TeacherRoles => Set<TeacherRole>();
    public DbSet<LessonCategory>
LessonCategories => Set<LessonCategory>();
    public DbSet<ReadingTask> ReadingTasks
    => Set<ReadingTask>();

    public DbSet<ReadingQuestion> ReadingQuestions
        => Set<ReadingQuestion>();

    public DbSet<ReadingOption> ReadingOptions
        => Set<ReadingOption>();

    public DbSet<ExerciseReadingTask> ExerciseReadingTasks
        => Set<ExerciseReadingTask>();

    public DbSet<LessonSection>
    LessonSections => Set<LessonSection>();
    public DbSet<QuestionCategory> QuestionCategories
    => Set<QuestionCategory>();
    
    public DbSet<Question> Questions
        => Set<Question>();

    public DbSet<QuestionOption> QuestionOptions
        => Set<QuestionOption>();
    public DbSet<QuestionAssignment> QuestionAssignments
        => Set<QuestionAssignment>();
    public DbSet<QuestionMap> QuestionMaps => Set<QuestionMap>();
    public DbSet<QuestionGroup> QuestionGroups => Set<QuestionGroup>();
    public DbSet<QuestionGroupQuestion> QuestionGroupQuestions => Set<QuestionGroupQuestion>();
    public DbSet<QuestionGroupAssignment> QuestionGroupAssignments => Set<QuestionGroupAssignment>();
    public DbSet<StudentQuestionGroupProgress> StudentQuestionGroupProgresses => Set<StudentQuestionGroupProgress>();
    public DbSet<Lesson>
    Lessons => Set<Lesson>();
    public DbSet<Exercise> Exercises
    => Set<Exercise>();
   
    public DbSet<ExerciseQuestionGroup> ExerciseQuestionGroups
    => Set<ExerciseQuestionGroup>();
    public DbSet<ExerciseQuestion> ExerciseQuestions
        => Set<ExerciseQuestion>();
    public DbSet<LessonVideo>
    LessonVideos => Set<LessonVideo>();
    public DbSet<StudentLessonProgress>
StudentLessonProgresses => Set<StudentLessonProgress>();

    public DbSet<LessonDocument>
    LessonDocuments => Set<LessonDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<StudentExperience>().HasIndex(x => x.StudentRoleId).IsUnique();
        modelBuilder.Entity<StudentExpTransaction>().HasIndex(x => new { x.StudentRoleId, x.Type, x.ReferenceKey }).IsUnique();
        modelBuilder.Entity<StudentAchievement>().HasIndex(x => new { x.StudentRoleId, x.Type }).IsUnique();
        modelBuilder.Entity<OnlineClassroom>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<OnlineClassroom>()
            .HasOne(x => x.ClassRoom)
            .WithMany()
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OnlineClassroom>()
            .HasOne(x => x.TeacherUser)
            .WithMany()
            .HasForeignKey(x => x.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VocabularyFlashcard>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<VocabularyFlashcard>()
            .HasOne(x => x.TeacherUser)
            .WithMany()
            .HasForeignKey(x => x.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VocabularyFlashcard>()
            .HasOne(x => x.ClassRoom)
            .WithMany()
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VocabularyFlashcard>().Property(x => x.Word).HasMaxLength(150);
        modelBuilder.Entity<VocabularyFlashcard>().Property(x => x.Meaning).HasMaxLength(500);
        modelBuilder.Entity<VocabularyFlashcard>().Property(x => x.ExampleSentence).HasMaxLength(1000);
        modelBuilder.Entity<StudentFlashcardProgress>()
            .HasIndex(x => new { x.StudentUserId, x.VocabularyFlashcardId, x.Mode })
            .IsUnique();
        modelBuilder.Entity<StudentFlashcardProgress>()
            .HasOne(x => x.StudentUser)
            .WithMany()
            .HasForeignKey(x => x.StudentUserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudentFlashcardProgress>()
            .HasOne(x => x.VocabularyFlashcard)
            .WithMany()
            .HasForeignKey(x => x.VocabularyFlashcardId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
    .HasOne(x => x.StudentRole)
    .WithOne(x => x.User)
    .HasForeignKey<StudentRole>(x => x.UserId);
        modelBuilder.Entity<Course>()
    .HasMany(x => x.Classes)
    .WithOne(x => x.Course)
    .HasForeignKey(x => x.CourseId);
        modelBuilder.Entity<ClassRoom>()
    .HasOne(x => x.Course)
    .WithMany(x => x.Classes)
    .HasForeignKey(x => x.CourseId);
        modelBuilder.Entity<ClassRoom>()
            .HasMany(x => x.Students)
            .WithOne(x => x.ClassRoom)
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Course>()
            .HasMany(x => x.Students)
            .WithOne(x => x.Course)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClassRoom>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<TeacherClass>()
    .HasOne(x => x.TeacherRole)
    .WithMany(x => x.TeacherClasses)
    .HasForeignKey(x => x.TeacherRoleId);

        modelBuilder.Entity<TeacherClass>()
            .HasOne(x => x.ClassRoom)
            .WithMany(x => x.TeacherClasses)
            .HasForeignKey(x => x.ClassRoomId);
        modelBuilder.Entity<TeacherClass>()
    .HasIndex(x => new
    {
        x.TeacherRoleId,
        x.ClassRoomId
    })
    .IsUnique();
        modelBuilder.Entity<Exercise>()
    .HasOne(x => x.TeacherUser)
    .WithMany()
    .HasForeignKey(x => x.TeacherUserId)
    .OnDelete(DeleteBehavior.Restrict);

        

        modelBuilder.Entity<Exercise>()
            .HasMany(x => x.ExerciseQuestions)
            .WithOne(x => x.Exercise)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseQuestion>()
            .HasOne(x => x.Question)
            .WithMany(x => x.ExerciseQuestions)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Exercise>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ExerciseQuestion>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<User>()
            .HasOne(x => x.TeacherRole)
            .WithOne(x => x.User)
            .HasForeignKey<TeacherRole>(x => x.UserId);
        modelBuilder.Entity<User>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ListeningTask>()
    .HasOne(x => x.TeacherUser)
    .WithMany()
    .HasForeignKey(x => x.TeacherUserId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ListeningTask>()
            .HasMany(x => x.Questions)
            .WithOne(x => x.ListeningTask)
            .HasForeignKey(x => x.ListeningTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ListeningQuestion>()
            .HasMany(x => x.Options)
            .WithOne(x => x.ListeningQuestion)
            .HasForeignKey(x => x.ListeningQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ListeningTask>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ListeningQuestion>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ListeningQuestion>()
            .Property(x => x.Explanation)
            .HasMaxLength(4000);

        modelBuilder.Entity<ListeningOption>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<LessonSection>()
    .HasOne(x => x.Course)
    .WithMany()
    .HasForeignKey(x => x.CourseId);
        modelBuilder.Entity<LessonSection>()
    .HasOne(x => x.TeacherUser)
    .WithMany()
    .HasForeignKey(x => x.TeacherUserId)
    .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LessonSection>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Sections)
            .HasForeignKey(x => x.LessonCategoryId);
       
        modelBuilder.Entity<ExerciseQuestionGroup>()
    .HasOne(x => x.Exercise)
    .WithMany(x => x.QuestionGroups)
    .HasForeignKey(x => x.ExerciseId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseQuestionGroup>()
    .HasMany(x => x.ExerciseQuestions)
    .WithOne(x => x.ExerciseQuestionGroup)
    .HasForeignKey(x => x.ExerciseQuestionGroupId)
    .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ExerciseQuestionGroup>()
            .HasQueryFilter(x => !x.IsDeleted);
        
        modelBuilder.Entity<Lesson>()
    .HasOne(x => x.Section)
    .WithMany(x => x.Lessons)
    .HasForeignKey(x => x.LessonSectionId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Lesson>()
    .HasOne(x => x.Video)
    .WithOne(x => x.Lesson)
    .HasForeignKey<LessonVideo>(x => x.LessonId)
    .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LessonSection>()
    .HasOne(x => x.ClassRoom)
    .WithMany()
    .HasForeignKey(x => x.ClassRoomId)
    .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QuestionCategory>()
    .HasMany(x => x.Questions)
    .WithOne(x => x.QuestionCategory)
    .HasForeignKey(x => x.QuestionCategoryId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Question>()
            .HasOne(x => x.TeacherUser)
            .WithMany()
            .HasForeignKey(x => x.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Question>()
            .Property(x => x.Explanation)
            .HasMaxLength(4000);
        modelBuilder.Entity<ExerciseListeningTask>()
    .HasOne(x => x.Exercise)
    .WithMany(x => x.ListeningTasks)
    .HasForeignKey(x => x.ExerciseId)
    .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SpeakingTask>()
    .HasOne(x => x.TeacherUser)
    .WithMany()
    .HasForeignKey(x => x.TeacherUserId)
    .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SystemBackgroundJob>()
    .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<SpeakingTask>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ExerciseSpeakingTask>()
            .HasOne(x => x.Exercise)
            .WithMany(x => x.SpeakingTasks)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseSpeakingTask>()
            .HasOne(x => x.SpeakingTask)
            .WithMany()
            .HasForeignKey(x => x.SpeakingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseSpeakingTask>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ExerciseListeningTask>()
            .HasOne(x => x.ListeningTask)
            .WithMany()
            .HasForeignKey(x => x.ListeningTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseListeningTask>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Question>()
            .HasMany(x => x.Options)
            .WithOne(x => x.Question)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Question>()
            .HasMany(x => x.Assignments)
            .WithOne(x => x.Question)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<QuestionAssignment>()
            .HasOne(x => x.ClassRoom)
            .WithMany()
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QuestionAssignment>()
            .HasIndex(x => new { x.QuestionId, x.ClassRoomId })
            .IsUnique();
        modelBuilder.Entity<QuestionAssignment>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<QuestionMap>().HasMany(x => x.Groups).WithOne(x => x.QuestionMap)
            .HasForeignKey(x => x.QuestionMapId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<QuestionMap>().HasOne<User>().WithMany().HasForeignKey(x => x.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QuestionMap>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<QuestionGroup>().HasMany(x => x.Questions).WithOne(x => x.QuestionGroup)
            .HasForeignKey(x => x.QuestionGroupId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<QuestionGroup>().HasMany(x => x.Assignments).WithOne(x => x.QuestionGroup)
            .HasForeignKey(x => x.QuestionGroupId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<QuestionGroup>().HasIndex(x => new { x.QuestionMapId, x.OrderIndex }).IsUnique();
        modelBuilder.Entity<QuestionGroup>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<QuestionGroupQuestion>().HasOne(x => x.Question).WithMany()
            .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QuestionGroupQuestion>().HasIndex(x => new { x.QuestionGroupId, x.QuestionId }).IsUnique();
        modelBuilder.Entity<QuestionGroupQuestion>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<QuestionGroupAssignment>().HasOne(x => x.ClassRoom).WithMany()
            .HasForeignKey(x => x.ClassRoomId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QuestionGroupAssignment>().HasIndex(x => new { x.QuestionGroupId, x.ClassRoomId }).IsUnique();
        modelBuilder.Entity<QuestionGroupAssignment>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<StudentQuestionGroupProgress>().HasOne(x => x.StudentRole).WithMany()
            .HasForeignKey(x => x.StudentRoleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudentQuestionGroupProgress>().HasOne(x => x.QuestionGroup).WithMany()
            .HasForeignKey(x => x.QuestionGroupId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudentQuestionGroupProgress>().HasIndex(x => new { x.StudentRoleId, x.QuestionGroupId }).IsUnique();
        modelBuilder.Entity<StudentQuestionGroupProgress>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ReadingTask>()
    .HasOne(x => x.TeacherUser)
    .WithMany()
    .HasForeignKey(x => x.TeacherUserId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadingTask>()
            .HasMany(x => x.Questions)
            .WithOne(x => x.ReadingTask)
            .HasForeignKey(x => x.ReadingTaskId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Test>()
    .HasOne(x => x.TeacherUser)
    .WithMany()
    .HasForeignKey(x => x.TeacherUserId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Test>()
            .HasMany(x => x.Assignments)
            .WithOne(x => x.Test)
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Test>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<TestAssignment>()
            .HasOne(x => x.Test)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestAssignment>()
            .HasOne(x => x.ClassRoom)
            .WithMany()
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAssignment>()
            .HasIndex(x => new { x.TestId, x.ClassRoomId })
            .IsUnique();

        modelBuilder.Entity<TestAssignment>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<TestAttempt>()
            .HasOne(x => x.TestAssignment)
            .WithMany()
            .HasForeignKey(x => x.TestAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TestAttempt>()
    .HasIndex(x => new
    {
        x.TestAssignmentId,
        x.StudentRoleId,
        x.Status
    });

     

        modelBuilder.Entity<TestAttempt>()
            .HasOne(x => x.StudentRole)
            .WithMany()
            .HasForeignKey(x => x.StudentRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAttempt>()
            .HasMany(x => x.Answers)
            .WithOne(x => x.TestAttempt)
            .HasForeignKey(x => x.TestAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestAttempt>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(x => x.SelectedQuestionOption)
            .WithMany()
            .HasForeignKey(x => x.SelectedQuestionOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(x => x.ListeningQuestion)
            .WithMany()
            .HasForeignKey(x => x.ListeningQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(x => x.SelectedListeningOption)
            .WithMany()
            .HasForeignKey(x => x.SelectedListeningOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(x => x.ReadingQuestion)
            .WithMany()
            .HasForeignKey(x => x.ReadingQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(x => x.SelectedReadingOption)
            .WithMany()
            .HasForeignKey(x => x.SelectedReadingOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(x => x.WritingTask)
            .WithMany()
            .HasForeignKey(x => x.WritingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(x => x.SpeakingTask)
            .WithMany()
            .HasForeignKey(x => x.SpeakingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAnswer>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<TestListeningTask>()
            .HasOne(x => x.Test)
            .WithMany(x => x.ListeningTasks)
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestListeningTask>()
            .HasOne(x => x.ListeningTask)
            .WithMany()
            .HasForeignKey(x => x.ListeningTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestListeningTask>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<TestReadingTask>()
            .HasOne(x => x.Test)
            .WithMany(x => x.ReadingTasks)
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestReadingTask>()
            .HasOne(x => x.ReadingTask)
            .WithMany()
            .HasForeignKey(x => x.ReadingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestReadingTask>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<TestWritingTask>()
            .HasOne(x => x.Test)
            .WithMany(x => x.WritingTasks)
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestWritingTask>()
            .HasOne(x => x.WritingTask)
            .WithMany()
            .HasForeignKey(x => x.WritingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestWritingTask>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<TestSpeakingTask>()
            .HasOne(x => x.Test)
            .WithMany(x => x.SpeakingTasks)
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompetitionTest>()
    .HasOne(x => x.TeacherUser)
    .WithMany()
    .HasForeignKey(x => x.TeacherUserId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTest>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<CompetitionTest>()
            .HasIndex(x => new
            {
                x.TeacherUserId,
                x.CreatedAt
            });
        modelBuilder.Entity<CompetitionTestListeningTask>()
    .HasOne(x => x.CompetitionTest)
    .WithMany(x => x.ListeningTasks)
    .HasForeignKey(x => x.CompetitionTestId)
    .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Notification>(
    entity =>
    {
        entity.Property(x =>
                x.DeduplicationKey)
            .HasMaxLength(200);

        entity.HasIndex(x =>
                x.DeduplicationKey)
            .IsUnique()
            .HasFilter(
                "[DeduplicationKey] IS NOT NULL " +
                "AND [IsDeleted] = 0");
    });
        modelBuilder.Entity<CompetitionTestListeningTask>()
            .HasOne(x => x.ListeningTask)
            .WithMany()
            .HasForeignKey(x => x.ListeningTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestListeningTask>()
            .HasIndex(x => new
            {
                x.CompetitionTestId,
                x.ListeningTaskId
            })
            .IsUnique();
       
        modelBuilder.Entity<CompetitionTestListeningTask>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<CompetitionTestReadingTask>()
    .HasOne(x => x.CompetitionTest)
    .WithMany(x => x.ReadingTasks)
    .HasForeignKey(x => x.CompetitionTestId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompetitionTestReadingTask>()
            .HasOne(x => x.ReadingTask)
            .WithMany()
            .HasForeignKey(x => x.ReadingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestReadingTask>()
            .HasIndex(x => new
            {
                x.CompetitionTestId,
                x.ReadingTaskId
            })
            .IsUnique();

        modelBuilder.Entity<CompetitionTestReadingTask>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<CompetitionTestWritingTask>()
    .HasOne(x => x.CompetitionTest)
    .WithMany(x => x.WritingTasks)
    .HasForeignKey(x => x.CompetitionTestId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompetitionTestWritingTask>()
            .HasOne(x => x.WritingTask)
            .WithMany()
            .HasForeignKey(x => x.WritingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestWritingTask>()
            .HasIndex(x => new
            {
                x.CompetitionTestId,
                x.WritingTaskId
            })
            .IsUnique();

        modelBuilder.Entity<CompetitionTestWritingTask>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<CompetitionTestSpeakingTask>()
    .HasOne(x => x.CompetitionTest)
    .WithMany(x => x.SpeakingTasks)
    .HasForeignKey(x => x.CompetitionTestId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompetitionTestSpeakingTask>()
            .HasOne(x => x.SpeakingTask)
            .WithMany()
            .HasForeignKey(x => x.SpeakingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestSpeakingTask>()
            .HasIndex(x => new
            {
                x.CompetitionTestId,
                x.SpeakingTaskId
            })
            .IsUnique();

        modelBuilder.Entity<CompetitionTestSpeakingTask>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<CompetitionTestAssignment>()
    .HasOne(x => x.CompetitionTest)
    .WithMany(x => x.Assignments)
    .HasForeignKey(x => x.CompetitionTestId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestAssignment>()
            .HasOne(x => x.TeacherUser)
            .WithMany()
            .HasForeignKey(x => x.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestAssignment>()
            .HasOne(x => x.ClassRoom)
            .WithMany()
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestAssignment>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<CompetitionTestAssignment>()
            .HasIndex(x => new
            {
                x.TeacherUserId,
                x.ClassRoomId,
                x.StartsAt
            });

        modelBuilder.Entity<CompetitionTestAssignment>()
            .HasIndex(x => new
            {
                x.Status,
                x.DeadlineAt
            });
        modelBuilder.Entity<CompetitionTestAttempt>()
    .HasOne(x => x.Assignment)
    .WithMany(x => x.Attempts)
    .HasForeignKey(x =>
        x.CompetitionTestAssignmentId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompetitionTestAttempt>()
            .HasOne(x => x.StudentRole)
            .WithMany()
            .HasForeignKey(x => x.StudentRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestAttempt>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<CompetitionTestAttempt>()
            .HasIndex(x => new
            {
                x.CompetitionTestAssignmentId,
                x.StudentRoleId
            })
            .IsUnique();
        modelBuilder.Entity<CompetitionTestAnswer>()
    .HasOne(x => x.Attempt)
    .WithMany(x => x.Answers)
    .HasForeignKey(x =>
        x.CompetitionTestAttemptId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompetitionTestAnswer>()
            .HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestAnswer>()
            .HasOne(x => x.SelectedQuestionOption)
            .WithMany()
            .HasForeignKey(x =>
                x.SelectedQuestionOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestAnswer>()
            .HasOne(x => x.ListeningQuestion)
            .WithMany()
            .HasForeignKey(x =>
                x.ListeningQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestAnswer>()
            .HasOne(x => x.SelectedListeningOption)
            .WithMany()
            .HasForeignKey(x =>
                x.SelectedListeningOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestAnswer>()
            .HasOne(x => x.ReadingQuestion)
            .WithMany()
            .HasForeignKey(x =>
                x.ReadingQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestAnswer>()
            .HasOne(x => x.SelectedReadingOption)
            .WithMany()
            .HasForeignKey(x =>
                x.SelectedReadingOptionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ReadingTask>()
    .HasMany(x => x.PassageImages)
    .WithOne(x => x.ReadingTask)
    .HasForeignKey(x =>
        x.ReadingTaskId)
    .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ReadingPassageImage>()
    .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ReadingPassageImage>()
    .HasIndex(x => new
    {
        x.ReadingTaskId,
        x.OrderIndex
    })
    .IsUnique();
        modelBuilder.Entity<CompetitionTestAnswer>()
            .HasOne(x => x.WritingTask)
            .WithMany()
            .HasForeignKey(x => x.WritingTaskId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Notification>()
    .HasOne(x => x.CreatedByUser)
    .WithMany()
    .HasForeignKey(x =>
        x.CreatedByUserId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(x => x.ClassRoom)
            .WithMany()
            .HasForeignKey(x =>
                x.ClassRoomId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserNotification>()
            .HasOne(x => x.Notification)
            .WithMany(x => x.Recipients)
            .HasForeignKey(x =>
                x.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserNotification>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x =>
                x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<Notification>()
            .Property(x => x.Message)
            .HasMaxLength(1000)
            .IsRequired();

        modelBuilder.Entity<Notification>()
            .Property(x => x.Type)
            .HasConversion<int>();

        modelBuilder.Entity<UserNotification>()
            .Property(x => x.ActionUrl)
            .HasMaxLength(500);

        modelBuilder.Entity<UserNotification>()
            .HasIndex(x => new
            {
                x.NotificationId,
                x.UserId
            })
            .IsUnique();

        modelBuilder.Entity<UserNotification>()
            .HasIndex(x => new
            {
                x.UserId,
                x.IsRead,
                x.CreatedAt
            });

        modelBuilder.Entity<Notification>()
            .HasIndex(x => new
            {
                x.ClassRoomId,
                x.CreatedAt
            });

        modelBuilder.Entity<Notification>()
            .HasQueryFilter(x =>
                !x.IsDeleted);

        modelBuilder.Entity<UserNotification>()
            .HasQueryFilter(x =>
                !x.IsDeleted);
        modelBuilder.Entity<CompetitionTestAnswer>()
            .HasOne(x => x.SpeakingTask)
            .WithMany()
            .HasForeignKey(x => x.SpeakingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionTestAnswer>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<CompetitionResult>()
    .HasOne(x => x.Assignment)
    .WithMany(x => x.Results)
    .HasForeignKey(x =>
        x.CompetitionTestAssignmentId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompetitionResult>()
            .HasOne(x => x.Attempt)
            .WithOne(x => x.Result)
            .HasForeignKey<CompetitionResult>(x =>
                x.CompetitionTestAttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionResult>()
            .HasOne(x => x.StudentRole)
            .WithMany()
            .HasForeignKey(x => x.StudentRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompetitionResult>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<CompetitionResult>()
            .HasIndex(x => new
            {
                x.CompetitionTestAssignmentId,
                x.StudentRoleId
            })
            .IsUnique();

        modelBuilder.Entity<CompetitionResult>()
            .HasIndex(x => new
            {
                x.CompetitionTestAssignmentId,
                x.Rank
            })
            .IsUnique();

        modelBuilder.Entity<CompetitionResult>()
            .HasIndex(x =>
                x.CompetitionTestAttemptId)
            .IsUnique();
        modelBuilder.Entity<TestSpeakingTask>()
            .HasOne(x => x.SpeakingTask)
            .WithMany()
            .HasForeignKey(x => x.SpeakingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestSpeakingTask>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ReadingQuestion>()
            .HasMany(x => x.Options)
            .WithOne(x => x.ReadingQuestion)
            .HasForeignKey(x => x.ReadingQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseReadingTask>()
            .HasOne(x => x.Exercise)
            .WithMany(x => x.ReadingTasks)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseReadingTask>()
            .HasOne(x => x.ReadingTask)
            .WithMany()
            .HasForeignKey(x => x.ReadingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadingTask>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ReadingQuestion>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ReadingQuestion>()
            .Property(x => x.Explanation)
            .HasMaxLength(4000);

        modelBuilder.Entity<ReadingOption>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ExerciseReadingTask>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<QuestionCategory>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Question>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ExerciseCategory>()
    .HasMany(x => x.Sections)
    .WithOne(x => x.Category)
    .HasForeignKey(x => x.ExerciseCategoryId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseCategory>()
            .HasQueryFilter(x => !x.IsDeleted);
        

       
      



        modelBuilder.Entity<ExerciseSection>()
            .HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseSection>()
            .HasOne(x => x.ClassRoom)
            .WithMany()
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseSection>()
            .HasOne(x => x.TeacherUser)
            .WithMany()
            .HasForeignKey(x => x.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseSection>()
            .HasMany(x => x.Exercises)
            .WithOne(x => x.ExerciseSection)
            .HasForeignKey(x => x.ExerciseSectionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ExerciseSection>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<QuestionOption>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Lesson>()
    .HasOne(x => x.Document)
    .WithOne(x => x.Lesson)
    .HasForeignKey<LessonDocument>(x => x.LessonId)
    .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudentLessonProgress>()
    .HasIndex(x => new
    {
        x.StudentRoleId,
        x.LessonId
    })
    .IsUnique();
        modelBuilder.Entity<WritingTask>()
    .HasOne<User>()
    .WithMany()
    .HasForeignKey(x => x.TeacherUserId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WritingTask>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ExerciseWritingTask>()
            .HasOne(x => x.Exercise)
            .WithMany(x => x.WritingTasks)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseWritingTask>()
            .HasOne(x => x.WritingTask)
            .WithMany()
            .HasForeignKey(x => x.WritingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseWritingTask>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<WritingSubmission>()
            .HasOne<ExerciseAttempt>()
            .WithMany()
            .HasForeignKey(x => x.ExerciseAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WritingSubmission>()
            .HasOne<WritingTask>()
            .WithMany()
            .HasForeignKey(x => x.WritingTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WritingSubmission>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<WritingScore>()
            .HasOne<WritingSubmission>()
            .WithMany()
            .HasForeignKey(x => x.WritingSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WritingScore>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ExerciseAttempt>()
    .HasMany(x => x.Answers)
    .WithOne(x => x.ExerciseAttempt)
    .HasForeignKey(x => x.ExerciseAttemptId)
    .OnDelete(DeleteBehavior.Cascade);
        
       
     
     



   

      
      
        modelBuilder.Entity<ExerciseAnswer>()
            .HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ExerciseCategory>().HasData(
    new ExerciseCategory
    {
        ExerciseCategoryId = 1,
        Name = "Grammar",
        OrderIndex = 1,
        IsActive = true
    },
    new ExerciseCategory
    {
        ExerciseCategoryId = 2,
        Name = "Listening",
        OrderIndex = 2,
        IsActive = true
    },
    new ExerciseCategory
    {
        ExerciseCategoryId = 3,
        Name = "Speaking",
        OrderIndex = 3,
        IsActive = true
    },
    new ExerciseCategory
    {
        ExerciseCategoryId = 4,
        Name = "Reading",
        OrderIndex = 4,
        IsActive = true
    },
    new ExerciseCategory
    {
        ExerciseCategoryId = 5,
        Name = "Writing",
        OrderIndex = 5,
        IsActive = true
    }
);
        modelBuilder.Entity<ExerciseAnswer>()
            .HasOne(x => x.SelectedQuestionOption)
            .WithMany()
            .HasForeignKey(x => x.SelectedQuestionOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseAnswer>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ExerciseAssignment>()
    .HasOne(x => x.Exercise)
    .WithMany(x => x.Assignments)
    .HasForeignKey(x => x.ExerciseId)
    .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ExerciseAttempt>()
    .HasOne(x => x.ExerciseAssignment)
    .WithMany()
    .HasForeignKey(x => x.ExerciseAssignmentId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseAttempt>()
            .HasOne(x => x.StudentRole)
            .WithMany()
            .HasForeignKey(x => x.StudentRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseAttempt>()
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ExerciseAssignment>()
            .HasOne(x => x.ClassRoom)
            .WithMany()
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExerciseAssignment>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<ExerciseAssignment>()
            .HasIndex(x => new
            {
                x.ExerciseId,
                x.ClassRoomId
            })
            .IsUnique();
        modelBuilder.Entity<StudentLessonProgress>()
            .HasOne(x => x.StudentRole)
            .WithMany(x => x.LessonProgresses)
            .HasForeignKey(x => x.StudentRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentLessonProgress>()
            .HasOne(x => x.Lesson)
            .WithMany(x => x.StudentProgresses)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
