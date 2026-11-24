namespace EnglishCenterSystem.Domain.Enums;


public enum NotificationType
{
    LessonCreated = 1,

    ExerciseAssigned = 2,

    TestAssigned = 3,

    CompetitionAssigned = 4,

    AdminTeacherUnassigned = 5,

    AdminClassWithoutTeacher = 6,

    AdminStudentUnassigned = 7,

    AdminClassAtCapacity = 8,

    AssignmentDeadlineReminder = 9,

    OnlineClassroomReminder = 10,

    LearningStreakReminder = 11
}
