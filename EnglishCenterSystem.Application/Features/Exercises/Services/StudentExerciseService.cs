using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Entities.Listening;
using EnglishCenterSystem.Domain.Entities.Reading;
using EnglishCenterSystem.Domain.Entities.Speaking;
using EnglishCenterSystem.Domain.Entities.Writing;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class StudentExerciseService
{
    private readonly IUnitOfWork _unitOfWork;
  
    private readonly IMinioService _minioService;
    private readonly ISpeechAssessmentService _speechAssessmentService;
    private readonly ISpeakingAiEvaluationService _speakingAiEvaluationService;
    private readonly IStudentGamificationService _studentGamificationService;
    public StudentExerciseService(
    IUnitOfWork unitOfWork,
    IMinioService minioService,
    ISpeechAssessmentService speechAssessmentService,
    ISpeakingAiEvaluationService speakingAiEvaluationService,
    IStudentGamificationService studentGamificationService
    )
    {
        _unitOfWork = unitOfWork;
        _minioService = minioService;
        _speechAssessmentService = speechAssessmentService;
        _speakingAiEvaluationService = speakingAiEvaluationService;
        _studentGamificationService = studentGamificationService;
       
    }

    public async Task<List<StudentExerciseDto>> GetMyExercisesAsync(
    int studentUserId,
    SkillType? skill = null)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .Include(x => x.ClassRoom)
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null ||
            studentRole.ClassRoomId == null)
        {
            return new List<StudentExerciseDto>();
        }

        var now = DateTime.UtcNow;

        var attempts = await _unitOfWork.ExerciseAttempts
    .Query()
    .Where(x =>
        x.StudentRoleId == studentRole.StudentRoleId)
    .Select(x => new
    {
        x.ExerciseAttemptId,
        x.ExerciseAssignmentId,
        x.StudentRoleId,
        x.StartedAt,
        x.SubmittedAt,
        x.Score,
        x.Status
    })
    .ToListAsync();

        var assignments = await _unitOfWork.ExerciseAssignments
    .Query()
    .AsNoTracking()
    .Where(x =>
        x.ClassRoomId == studentRole.ClassRoomId &&
        x.IsActive &&
        x.Exercise.IsActive &&
        (
            !skill.HasValue ||
            x.Exercise.Skill == skill.Value
        ))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return assignments.Select(x =>
        {
            var assignmentAttempts = attempts
                .Where(a =>
                    a.ExerciseAssignmentId ==
                    x.ExerciseAssignmentId)
                .ToList();

            var submittedAttempts = assignmentAttempts
                .Where(a =>
                    a.Status != ExerciseAttemptStatus.InProgress)
                .ToList();

            return new StudentExerciseDto
            {
                ExerciseAssignmentId = x.ExerciseAssignmentId,
                ExerciseId = x.ExerciseId,
                Title = x.Exercise.Title,
                Description = x.Exercise.Description,
               
                Skill = x.Exercise.Skill,

                ExerciseSectionId = x.Exercise.ExerciseSectionId,

                ExerciseSectionTitle =
                    x.Exercise.ExerciseSection != null
                        ? x.Exercise.ExerciseSection.Title
                        : "Chưa có section",

                MaxAttempts = x.MaxAttempts,

                AttemptCount = submittedAttempts.Count,

                CanStart =
    assignmentAttempts.Any(a =>
        a.Status == ExerciseAttemptStatus.InProgress)
    ||
    submittedAttempts.Count < x.MaxAttempts,

                TotalQuestions =
                    x.Exercise.ExerciseQuestions.Count +
                    x.Exercise.QuestionGroups
                        .SelectMany(g => g.ExerciseQuestions)
                        .Count() +
                    x.Exercise.ListeningTasks
                        .SelectMany(t => t.ListeningTask.Questions)
                        .Count() +
                    x.Exercise.ReadingTasks
                        .SelectMany(t => t.ReadingTask.Questions)
                        .Count() +
                    x.Exercise.SpeakingTasks.Count +
                    x.Exercise.WritingTasks.Count,

                TotalPoints = 10,

                AttemptHistory = submittedAttempts
                    .Where(a => a.SubmittedAt != null)
                    .OrderByDescending(a => a.SubmittedAt)
                    .Select(a => new StudentExerciseAttemptHistoryItemDto
                    {
                        ExerciseAttemptId = a.ExerciseAttemptId,
                        StartedAt = a.StartedAt,
                        SubmittedAt = a.SubmittedAt,
                        Score = a.Score,
                        Status = a.Status.ToString()
                    })
                    .ToList()
            };
        }).ToList();
    }
    public async Task<List<StudentExerciseSectionDto>> GetMyExerciseSectionsAsync(
    int studentUserId,
    SkillType? skill)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .Include(x => x.ClassRoom)
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null ||
            studentRole.ClassRoomId == null)
        {
            return new List<StudentExerciseSectionDto>();
        }

        var courseId = studentRole.ClassRoom!.CourseId;
        var classRoomId = studentRole.ClassRoomId.Value;

        var sections = await _unitOfWork.ExerciseSections
    .Where(x =>
                x.CourseId == courseId &&
                x.ClassRoomId == classRoomId &&
                (
                    !skill.HasValue ||
                    x.ExerciseCategoryId == (int)skill.Value
                ))
            .OrderBy(x => x.CreatedAt)
            .Select(x => new StudentExerciseSectionDto
            {
                ExerciseSectionId = x.ExerciseSectionId,
                Title = x.Title
            })
            .ToListAsync();

        var exercises = await GetMyExercisesAsync(
    studentUserId,
    skill);

        foreach (var section in sections)
        {
            section.Exercises = exercises
                .Where(x =>
                    x.ExerciseSectionId ==
                    section.ExerciseSectionId)
                .ToList();
        }

        return sections;
    }
    public async Task<StudentExerciseProgressDto> GetMyProgressAsync(
    int studentUserId)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null ||
            studentRole.ClassRoomId == null)
        {
            return new StudentExerciseProgressDto();
        }

        var assignedExercises = await _unitOfWork.ExerciseAssignments
            .Query()
            .Where(x =>
                x.ClassRoomId == studentRole.ClassRoomId &&
                x.IsActive &&
                x.Exercise.IsActive)
            .ToListAsync();

        var attempts = await _unitOfWork.ExerciseAttempts
            .Query()
            .Where(x => x.StudentRoleId == studentRole.StudentRoleId)
            .ToListAsync();

        var submittedAttempts = attempts
    .Where(x =>
        x.Status == ExerciseAttemptStatus.Submitted ||
        x.Status == ExerciseAttemptStatus.Reviewed ||
        x.Status == ExerciseAttemptStatus.AiGraded)
    .ToList();

        var completedAssignmentIds = submittedAttempts
            .Select(x => x.ExerciseAssignmentId)
            .Distinct()
            .ToList();

        var totalAssigned = assignedExercises.Count;

        var completed = assignedExercises
            .Count(x =>
                completedAssignmentIds.Contains(
                    x.ExerciseAssignmentId));

        var totalScore = submittedAttempts.Sum(x => x.Score);

        var totalPossibleScore = submittedAttempts.Sum(x => x.TotalPoints);

        return new StudentExerciseProgressDto
        {
            TotalAssignedExercises = totalAssigned,
            CompletedExercises = completed,
            InProgressAttempts = attempts.Count(x =>
                x.Status == ExerciseAttemptStatus.InProgress),

            CompletionPercent = totalAssigned == 0
                ? 0
                : Math.Round((double)completed / totalAssigned * 100, 2),

            AverageScore = submittedAttempts.Any()
                ? Math.Round(submittedAttempts.Average(x => x.Score), 2)
                : 0,

            TotalScore = totalScore,
            TotalPossibleScore = totalPossibleScore
        };
    }
    public async Task<ExerciseResultDto> SubmitAttemptAsync(
    int studentUserId,
    SubmitExerciseAttemptDto dto)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null)
        {
            throw new Exception("Student not found");
        }

        var attempt = await _unitOfWork.ExerciseAttempts
            .GetByIdAsync(dto.ExerciseAttemptId);

        if (attempt == null ||
            attempt.StudentRoleId != studentRole.StudentRoleId)
        {
            throw new Exception("Exercise attempt not found");
        }

        if (attempt.Status == ExerciseAttemptStatus.Submitted)
        {
            throw new Exception("Exercise attempt already submitted");
        }

        var assignment = attempt.ExerciseAssignment;

        var now = DateTime.UtcNow;

       

        var exerciseQuestions = assignment.Exercise.ExerciseQuestions
    .Concat(
        assignment.Exercise.QuestionGroups
            .SelectMany(g => g.ExerciseQuestions)
    )
    .OrderBy(x => x.OrderIndex)
    .ToList();
        var listeningQuestions = assignment.Exercise.ListeningTasks
    .SelectMany(x => x.ListeningTask.Questions)
    .OrderBy(x => x.OrderIndex)
    .ToList();
        var speakingLinks = assignment.Exercise.SpeakingTasks
    .OrderBy(x => x.OrderIndex)
    .ToList();
        var readingQuestions = assignment.Exercise.ReadingTasks
    .SelectMany(x => x.ReadingTask.Questions)
    .OrderBy(x => x.OrderIndex)
    .ToList();
        var writingTasks = assignment.Exercise.WritingTasks
    .OrderBy(x => x.OrderIndex)
    .ToList();
        if (exerciseQuestions.Count == 0 &&
    listeningQuestions.Count == 0 &&
    readingQuestions.Count == 0 &&
    writingTasks.Count == 0 &&
    speakingLinks.Count == 0)
        {
            throw new Exception("Exercise has no questions");
        }

        var existingSpeakingAnswers = attempt.Answers
    .Where(x => x.SpeakingTaskId != null)
    .ToList();

        attempt.Answers.Clear();

        foreach (var speakingAnswer in existingSpeakingAnswers)
        {
            attempt.Answers.Add(speakingAnswer);
        }

        var correctAnswers = 0;
        var score = 0.0;
        var rawTotalPoints =
    exerciseQuestions.Sum(x => x.Point) +
    listeningQuestions.Sum(x => x.Point) +
    readingQuestions.Sum(x => x.Point)  +
     (speakingLinks.Any() ? 10 : 0);

        var totalPoints = 10;

        foreach (var exerciseQuestion in exerciseQuestions)
        {
            var submittedAnswer = dto.Answers
    .FirstOrDefault(x =>
        x.QuestionId == exerciseQuestion.QuestionId);

            var question = exerciseQuestion.Question;

            var selectedOptionId =
                submittedAnswer?.SelectedQuestionOptionId;

            var textAnswer =
                submittedAnswer?.TextAnswer;

            var audioAnswerUrl =
                submittedAnswer?.AudioAnswerUrl;

            var isCorrect = false;
            var pointEarned = 0.0;

            if (
                question.QuestionType == QuestionType.SingleChoice ||
                question.QuestionType == QuestionType.TrueFalse
            )
            {
                var correctOption = question.Options
                    .FirstOrDefault(x => x.IsCorrect);

                var selectedOption = question.Options
                    .FirstOrDefault(x =>
                        x.QuestionOptionId == selectedOptionId);

                isCorrect =
                    selectedOption != null &&
                    correctOption != null &&
                    selectedOption.QuestionOptionId ==
                        correctOption.QuestionOptionId;

                pointEarned = isCorrect
                    ? exerciseQuestion.Point
                    : 0;

                if (isCorrect)
                {
                    correctAnswers++;
                }
            }

            if (question.QuestionType == QuestionType.TextAnswer)
            {
                var correctText =
                    question.CorrectTextAnswer;

                isCorrect =
                    !string.IsNullOrWhiteSpace(correctText) &&
                    NormalizeAnswer(textAnswer) ==
                    NormalizeAnswer(correctText);

                pointEarned = isCorrect
                    ? exerciseQuestion.Point
                    : 0;

                if (isCorrect)
                {
                    correctAnswers++;
                }
            }

            if (question.QuestionType == QuestionType.AudioAnswer)
            {
                pointEarned = 0;
            }

            score += pointEarned;

            attempt.Answers.Add(new ExerciseAnswer
            {
                QuestionId = exerciseQuestion.QuestionId,
                SelectedQuestionOptionId = selectedOptionId,
                TextAnswer = textAnswer,
                AudioAnswerUrl = audioAnswerUrl,
                IsCorrect = isCorrect,
                PointEarned = pointEarned
            });
        }
        foreach (var listeningQuestion in listeningQuestions)
        {
            var submittedAnswer = dto.Answers
                .FirstOrDefault(x =>
                    x.ListeningQuestionId ==
                    listeningQuestion.ListeningQuestionId);

            var selectedOptionId =
                submittedAnswer?.SelectedListeningOptionId;

            var correctOption = listeningQuestion.Options
                .FirstOrDefault(x => x.IsCorrect);

            var selectedOption = listeningQuestion.Options
                .FirstOrDefault(x =>
                    x.ListeningOptionId == selectedOptionId);

            var isCorrect =
                selectedOption != null &&
                correctOption != null &&
                selectedOption.ListeningOptionId ==
                correctOption.ListeningOptionId;

            var pointEarned =
                isCorrect
                    ? listeningQuestion.Point
                    : 0;

            if (isCorrect)
            {
                correctAnswers++;
            }

            score += pointEarned;

            attempt.Answers.Add(
                new ExerciseAnswer
                {
                    ListeningQuestionId =
                        listeningQuestion.ListeningQuestionId,

                    SelectedListeningOptionId =
                        selectedOptionId,

                    IsCorrect = isCorrect,

                    PointEarned = pointEarned
                });
        }
        var pointPerSpeakingTask = speakingLinks.Count == 0
    ? 0
    : Math.Round(10.0 / speakingLinks.Count, 2);

        foreach (var speakingLink in speakingLinks)
        {
            var existingAnswer = attempt.Answers
                .FirstOrDefault(x =>
                    x.SpeakingTaskId == speakingLink.SpeakingTaskId);

            if (existingAnswer == null)
            {
                existingAnswer = new ExerciseAnswer
                {
                    SpeakingTaskId = speakingLink.SpeakingTaskId,
                    IsCorrect = false,
                    PointEarned = 0,
                    TeacherFeedback = "Student did not submit speaking audio"
                };

                attempt.Answers.Add(existingAnswer);
            }

            await GradeSpeakingAnswerAsync(
                existingAnswer,
                speakingLink,
                pointPerSpeakingTask);

            score += existingAnswer.PointEarned;

            if (existingAnswer.PointEarned > 0)
            {
                correctAnswers++;
            }
        }
        foreach (var readingQuestion in readingQuestions)
        {
            var submittedAnswer = dto.Answers
                .FirstOrDefault(x =>
                    x.ReadingQuestionId ==
                    readingQuestion.ReadingQuestionId);

            var selectedOptionId =
                submittedAnswer?.SelectedReadingOptionId;

            var correctOption = readingQuestion.Options
                .FirstOrDefault(x => x.IsCorrect);

            var selectedOption = readingQuestion.Options
                .FirstOrDefault(x =>
                    x.ReadingOptionId == selectedOptionId);

            var isCorrect =
                selectedOption != null &&
                correctOption != null &&
                selectedOption.ReadingOptionId ==
                correctOption.ReadingOptionId;

            var pointEarned =
                isCorrect
                    ? readingQuestion.Point
                    : 0;

            if (isCorrect)
            {
                correctAnswers++;
            }

            score += pointEarned;

            attempt.Answers.Add(
                new ExerciseAnswer
                {
                    ReadingQuestionId =
                        readingQuestion.ReadingQuestionId,

                    SelectedReadingOptionId =
                        selectedOptionId,

                    IsCorrect = isCorrect,

                    PointEarned = pointEarned
                });
        }
        var pendingWritingSubmissionIds = new List<int>();
        foreach (var writingTaskLink in writingTasks)
        {
            var submittedAnswer = dto.Answers
                .FirstOrDefault(x =>
                    x.WritingTaskId ==
                    writingTaskLink.WritingTaskId);

            var essayText = submittedAnswer?.TextAnswer;

            if (string.IsNullOrWhiteSpace(essayText))
            {
                essayText = "";
            }

            attempt.Answers.Add(new ExerciseAnswer
            {
                WritingTaskId = writingTaskLink.WritingTaskId,
                TextAnswer = essayText,
                IsCorrect = false,
                PointEarned = 0
            });

            var writingSubmission = new WritingSubmission
            {
                ExerciseAttemptId = attempt.ExerciseAttemptId,
                WritingTaskId = writingTaskLink.WritingTaskId,
                StudentUserId = studentUserId,
                EssayText = essayText,
                Status = WritingSubmissionStatus.Pending
            };

            await _unitOfWork.WritingSubmissions.AddAsync(
                writingSubmission);
        }
        var needManualReview = false;

        var needAiGrading = writingTasks.Any();
        attempt.TotalQuestions =
    exerciseQuestions.Count +
    listeningQuestions.Count +
    readingQuestions.Count +
    writingTasks.Count +
    speakingLinks.Count;

        if (!writingTasks.Any() && rawTotalPoints > 0)
        {
            foreach (var answer in attempt.Answers)
            {
                answer.PointEarned = Math.Round(
                    answer.PointEarned / rawTotalPoints * 10,
                    2);
            }

            score = Math.Round(
                attempt.Answers.Sum(x => x.PointEarned),
                2);
        }

        attempt.CorrectAnswers = correctAnswers;
        attempt.TotalPoints = totalPoints;
        attempt.Score = writingTasks.Any()
            ? 0
            : score;
        attempt.SubmittedAt = now;
        attempt.Status = needAiGrading
    ? ExerciseAttemptStatus.PendingAiGrading
    : needManualReview
        ? ExerciseAttemptStatus.PendingReview
        : ExerciseAttemptStatus.Submitted;
        attempt.UpdatedAt = now;

        await _unitOfWork.SaveChangesAsync();

        if (attempt.Status == ExerciseAttemptStatus.Submitted)
        {
            await _studentGamificationService.AwardExerciseSubmissionAsync(attempt.ExerciseAttemptId);
        }
        else
        {
            await _studentGamificationService.RecordDailyActivityAsync(
                attempt.StudentRoleId,
                attempt.SubmittedAt!.Value);
        }
        pendingWritingSubmissionIds = _unitOfWork.WritingSubmissions
     .Query()
     .Where(x =>
         x.ExerciseAttemptId == attempt.ExerciseAttemptId &&
         x.Status == WritingSubmissionStatus.Pending)
     .Select(x => x.WritingSubmissionId)
     .ToList();

        return new ExerciseResultDto
        {
            ExerciseAttemptId = attempt.ExerciseAttemptId,
            ExerciseTitle = assignment.Exercise.Title,
            TotalQuestions = attempt.TotalQuestions,
            CorrectAnswers = attempt.CorrectAnswers,
            TotalPoints = attempt.TotalPoints,
            Score = attempt.Score,
            PendingWritingSubmissionIds = pendingWritingSubmissionIds,
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt!.Value,
            Answers = attempt.Answers
    .Select(answer =>
    {
        if (answer.SpeakingTaskId != null)
        {
            var speakingLink =
                speakingLinks.FirstOrDefault(x =>
                    x.SpeakingTaskId ==
                    answer.SpeakingTaskId);

            return new ExerciseAnswerResultDto
            {
                ExerciseAnswerId =
                    answer.ExerciseAnswerId,

                QuestionId = null,

                SpeakingTaskId =
                    answer.SpeakingTaskId,

                SpeakingTaskTitle =
                    speakingLink?.SpeakingTask.Title
                    ?? "Speaking Task",

                SpeakingInstruction =
                    speakingLink?.SpeakingTask.Instruction,

                SpeakingPromptText =
                    speakingLink?.SpeakingTask.PromptText,

                SpeakingPromptImageUrl =
                    speakingLink?.SpeakingTask.PromptImageUrl,

                SpeakingReferenceText =
                    speakingLink?.SpeakingTask.ReferenceText,

                QuestionContent =
                    speakingLink?.SpeakingTask.Title
                    ?? "Speaking Task",

                AudioAnswerUrl =
                    answer.SpeakingAudioUrl
                    ?? answer.AudioAnswerUrl,

                SpeakingTranscript =
                    answer.SpeakingTranscript
                    ?? answer.TextAnswer,

                TextAnswer =
                    answer.TextAnswer
                    ?? answer.SpeakingTranscript,

                IsCorrect =
                    answer.IsCorrect,

                PointEarned =
                    answer.PointEarned,

                ManualScore =
                    answer.ManualScore,

                TeacherFeedback =
                    answer.TeacherFeedback,

                OverallSpeakingScore =
                    answer.OverallSpeakingScore,

                PronunciationScore =
                    answer.PronunciationScore,

                FluencyScore =
                    answer.FluencyScore,

                GrammarScore =
                    answer.GrammarScore,

                VocabularyScore =
                    answer.VocabularyScore,

                AiSpeakingFeedback =
                    answer.AiSpeakingFeedback
            };
        }
        if (answer.QuestionId != null)
        {
            var exerciseQuestion = exerciseQuestions
                .FirstOrDefault(x =>
                    x.QuestionId == answer.QuestionId);

            if (exerciseQuestion == null)
            {
                
                return null;
            }

            var question = exerciseQuestion.Question;

            var orderedOptions = question.Options
                .OrderBy(x => x.OrderIndex)
                .ToList();

            var selectedOption = orderedOptions
                .FirstOrDefault(x =>
                    x.QuestionOptionId ==
                    answer.SelectedQuestionOptionId);

            var correctOption = orderedOptions
                .FirstOrDefault(x => x.IsCorrect);

            var selectedIndex =
                selectedOption == null
                    ? -1
                    : orderedOptions.FindIndex(x =>
                        x.QuestionOptionId ==
                        selectedOption.QuestionOptionId);

            var correctIndex =
                correctOption == null
                    ? -1
                    : orderedOptions.FindIndex(x =>
                        x.QuestionOptionId ==
                        correctOption.QuestionOptionId);

            return new ExerciseAnswerResultDto
            {
                ExerciseAnswerId =
                    answer.ExerciseAnswerId,

                QuestionId =
                    question.QuestionId,

                QuestionContent =
                    question.Content,

                Explanation =
                    question.Explanation,

                SelectedQuestionOptionId =
                    answer.SelectedQuestionOptionId,

                CorrectQuestionOptionId =
                    correctOption?.QuestionOptionId,

                SelectedAnswerLabel =
                    selectedOption == null
                        ? null
                        : GetOptionLabel(
                            selectedIndex,
                            selectedOption.Content),

                CorrectAnswerLabel =
                    correctOption == null
                        ? null
                        : GetOptionLabel(
                            correctIndex,
                            correctOption.Content),

                Options = orderedOptions
                    .Select((option, index) =>
                        new ExerciseAnswerOptionResultDto
                        {
                            OptionId =
                                option.QuestionOptionId,

                            Label =
                                ((char)('A' + index))
                                .ToString(),

                            Content =
                                option.Content,

                            IsSelected =
                                option.QuestionOptionId ==
                                answer.SelectedQuestionOptionId,

                            IsCorrect =
                                option.IsCorrect
                        })
                    .ToList(),

                IsCorrect =
                    answer.IsCorrect,

                PointEarned =
                    answer.PointEarned,

                TextAnswer =
                    answer.TextAnswer,

                AudioAnswerUrl =
                    answer.AudioAnswerUrl,

                ManualScore =
                    answer.ManualScore,

                TeacherFeedback =
                    answer.TeacherFeedback,

                GradedByTeacherUserId =
                    answer.GradedByTeacherUserId,

                GradedAt =
                    answer.GradedAt
            };
        }

        if (answer.ListeningQuestionId != null)
        {
            var listeningQuestion = listeningQuestions
                .FirstOrDefault(x =>
                    x.ListeningQuestionId ==
                    answer.ListeningQuestionId);

            if (listeningQuestion == null)
            {
                return null;
            }

            var correctOption = listeningQuestion.Options
                .OrderBy(x => x.OrderIndex)
                .FirstOrDefault(x => x.IsCorrect);

            return new ExerciseAnswerResultDto
            {
                ExerciseAnswerId =
                    answer.ExerciseAnswerId,

                QuestionId = 0,

                QuestionContent =
                    listeningQuestion.Content,

                Explanation =
                    listeningQuestion.Explanation,

                SelectedQuestionOptionId =
                    answer.SelectedListeningOptionId,

                CorrectQuestionOptionId =
                    correctOption?.ListeningOptionId,

                IsCorrect =
                    answer.IsCorrect,

                PointEarned =
                    answer.PointEarned,

           

                ManualScore =
                    answer.ManualScore,

                TeacherFeedback =
                    answer.TeacherFeedback,

                GradedByTeacherUserId =
                    answer.GradedByTeacherUserId,

                GradedAt =
                    answer.GradedAt
            };
        }
        if (answer.ReadingQuestionId != null)
        {
            var readingLink =
                assignment.Exercise
                    .ReadingTasks
                    .FirstOrDefault(link =>
                        link.ReadingTask
                            .Questions
                            .Any(question =>
                                question.ReadingQuestionId ==
                                answer.ReadingQuestionId));

            var readingQuestion =
                readingLink?
                    .ReadingTask
                    .Questions
                    .FirstOrDefault(question =>
                        question.ReadingQuestionId ==
                        answer.ReadingQuestionId);

            if (readingQuestion == null)
            {
                return null;
            }

            var correctOption = readingQuestion.Options
                .OrderBy(x => x.OrderIndex)
                .FirstOrDefault(x => x.IsCorrect);

            return new ExerciseAnswerResultDto
            {
                ExerciseAnswerId =
                    answer.ExerciseAnswerId,

                QuestionId = 0,
                ReadingQuestionId =
    answer.ReadingQuestionId,

                ReadingTaskId =
    readingLink?.ReadingTaskId,

                ReadingTaskTitle =
    readingLink?.ReadingTask.Title,

                ReadingPassageTitle =
    readingLink?.ReadingTask.Title,

                ReadingInstruction =
    readingLink?.ReadingTask.Instruction,

                ReadingPassageText =
    readingLink?.ReadingTask.PassageText,
                QuestionContent =
                    readingQuestion.Content,

                Explanation =
                    readingQuestion.Explanation,

                SelectedQuestionOptionId =
                    answer.SelectedReadingOptionId,

                CorrectQuestionOptionId =
                    correctOption?.ReadingOptionId,

                IsCorrect =
                    answer.IsCorrect,

                PointEarned =
                    answer.PointEarned,

              

                ManualScore =
                    answer.ManualScore,

                TeacherFeedback =
                    answer.TeacherFeedback,

                GradedByTeacherUserId =
                    answer.GradedByTeacherUserId,

                GradedAt =
                    answer.GradedAt
            };
        }
        return null;
    })
.Where(x => x != null)
.Cast<ExerciseAnswerResultDto>()
.ToList()!
        };
    }
    private static string GetOptionLabel(
    int index,
    string content)
    {
        var label = ((char)('A' + index)).ToString();

        return string.IsNullOrWhiteSpace(content)
            ? label
            : $"{label}. {content}";
    }
    private async Task GradeSpeakingAnswerAsync(
    ExerciseAnswer answer,
    ExerciseSpeakingTask speakingLink,
    double pointPerSpeakingTask)
    {
        if (string.IsNullOrWhiteSpace(answer.SpeakingAudioUrl))
        {
            answer.PointEarned = 0;
            answer.IsCorrect = false;
            answer.TeacherFeedback = "Student did not submit speaking audio";
            return;
        }

        using var httpClient = new HttpClient();

        var audioBytes = await httpClient.GetByteArrayAsync(
            answer.SpeakingAudioUrl);

        await using var audioStream = new MemoryStream(audioBytes);

        var speechResult =
    await _speechAssessmentService.AssessAsync(
        audioStream,
        $"speaking-{speakingLink.SpeakingTaskId}.webm",
        speakingLink.SpeakingTask.ReferenceText);

        var transcript =
            speechResult.Transcript?.Trim() ?? "";

        var meaningfulWords = transcript
            .Split(
                new[]
                {
            ' ',
            '\t',
            '\r',
            '\n',
            '.',
            ',',
            '!',
            '?',
            ';',
            ':'
                },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(word =>
                word.Any(char.IsLetterOrDigit))
            .ToList();

        /*
         * Gatekeeper tầng 1:
         * transcript quá ngắn thì không gọi AI chấm nội dung.
         */
        if (meaningfulWords.Count < 5)
        {
            answer.SpeakingTranscript = transcript;
            answer.TextAnswer = transcript;

            answer.PronunciationScore =
                speechResult.PronunciationScore;

            answer.AccuracyScore =
                speechResult.AccuracyScore;

            answer.FluencyScore =
                speechResult.FluencyScore;

            answer.CompletenessScore =
                speechResult.CompletenessScore;

            answer.ProsodyScore =
                speechResult.ProsodyScore;

            answer.WordsPerMinute =
                speechResult.WordsPerMinute;

            answer.SpeakingWordCount =
                meaningfulWords.Count;

            answer.FillerWordCount =
                speechResult.FillerWordCount;

            answer.RepetitionCount =
                speechResult.RepetitionCount;

            answer.LongPauseCount =
                speechResult.LongPauseCount;

            answer.AveragePauseSeconds =
                speechResult.AveragePauseSeconds;

            answer.SpeakingAnalysisJson =
                speechResult.AnalysisJson;

            answer.GrammarScore = 0;
            answer.VocabularyScore = 0;
            answer.ContentRelevanceScore = 0;
            answer.CoherenceScore = 0;
            answer.OverallSpeakingScore = 0;

            answer.AiSpeakingFeedback =
                "Bài nói quá ngắn để đánh giá nội dung.";

            answer.PointEarned = 0;
            answer.IsCorrect = false;

            answer.TeacherFeedback =
                "Bài nói có ít hơn 5 từ có nghĩa nên không đủ điều kiện chấm điểm.";

            answer.UpdatedAt =
                DateTime.UtcNow;

            return;
        }

        var aiResult =
            await _speakingAiEvaluationService.EvaluateAsync(
                speakingLink.SpeakingTask.PromptText ?? "",
                speakingLink.SpeakingTask.ReferenceText,
                transcript);

        /*
         * Gatekeeper tầng 2:
         * Có nói đủ dài nhưng sai chủ đề hoặc không trả lời yêu cầu.
         */
        if (
            aiResult.ContentRelevanceScore <= 1 ||
            aiResult.OverallScore <= 1
        )
        {
            answer.SpeakingTranscript = transcript;
            answer.TextAnswer = transcript;

            answer.PronunciationScore =
                speechResult.PronunciationScore;

            answer.AccuracyScore =
                speechResult.AccuracyScore;

            answer.FluencyScore =
                speechResult.FluencyScore;

            answer.CompletenessScore =
                speechResult.CompletenessScore;

            answer.ProsodyScore =
                speechResult.ProsodyScore;

            answer.WordsPerMinute =
                speechResult.WordsPerMinute;

            answer.SpeakingWordCount =
                meaningfulWords.Count;

            answer.FillerWordCount =
                speechResult.FillerWordCount;

            answer.RepetitionCount =
                speechResult.RepetitionCount;

            answer.LongPauseCount =
                speechResult.LongPauseCount;

            answer.AveragePauseSeconds =
                speechResult.AveragePauseSeconds;

            answer.SpeakingAnalysisJson =
                speechResult.AnalysisJson;

            answer.GrammarScore =
                aiResult.GrammarScore;

            answer.VocabularyScore =
                aiResult.VocabularyScore;

            answer.ContentRelevanceScore =
                aiResult.ContentRelevanceScore;

            answer.CoherenceScore =
                aiResult.CoherenceScore;

            answer.OverallSpeakingScore = 0;

            answer.AiSpeakingFeedback =
                aiResult.Feedback;

            answer.PointEarned = 0;
            answer.IsCorrect = false;

            answer.TeacherFeedback =
                aiResult.Feedback;

            answer.UpdatedAt =
                DateTime.UtcNow;

            return;
        }

        var finalScore =
            Math.Round(
                (
                    speechResult.AccuracyScore * 0.15 +
                    speechResult.FluencyScore * 0.15 +
                    speechResult.CompletenessScore * 0.15 +
                    speechResult.ProsodyScore * 0.05 +
                    aiResult.ContentRelevanceScore * 0.25 +
                    aiResult.GrammarScore * 0.10 +
                    aiResult.VocabularyScore * 0.10 +
                    aiResult.CoherenceScore * 0.05
                ) / 10 * pointPerSpeakingTask,
                2);

        answer.SpeakingTranscript = speechResult.Transcript;
        answer.TextAnswer = speechResult.Transcript;

        answer.PronunciationScore = speechResult.PronunciationScore;
        answer.AccuracyScore = speechResult.AccuracyScore;
        answer.FluencyScore = speechResult.FluencyScore;
        answer.CompletenessScore = speechResult.CompletenessScore;
        answer.ProsodyScore = speechResult.ProsodyScore;
        answer.WordsPerMinute = speechResult.WordsPerMinute;
        answer.SpeakingWordCount = speechResult.WordCount;
        answer.FillerWordCount = speechResult.FillerWordCount;
        answer.RepetitionCount = speechResult.RepetitionCount;
        answer.LongPauseCount = speechResult.LongPauseCount;
        answer.AveragePauseSeconds = speechResult.AveragePauseSeconds;
        answer.SpeakingAnalysisJson = speechResult.AnalysisJson;

        answer.GrammarScore = aiResult.GrammarScore;
        answer.VocabularyScore = aiResult.VocabularyScore;
        answer.ContentRelevanceScore = aiResult.ContentRelevanceScore;
        answer.CoherenceScore = aiResult.CoherenceScore;
        answer.OverallSpeakingScore = aiResult.OverallScore;
        answer.AiSpeakingFeedback = aiResult.Feedback;

        answer.PointEarned = finalScore;
        answer.IsCorrect = finalScore > 0;
        answer.TeacherFeedback =
            $"AI Feedback: {aiResult.Feedback}\n\n" +
            $"Pronunciation Estimate: {speechResult.PronunciationScore}/10\n" +
            $"Accuracy Estimate: {speechResult.AccuracyScore}/10\n" +
            $"Fluency Estimate: {speechResult.FluencyScore}/10\n" +
            $"Completeness Estimate: {speechResult.CompletenessScore}/10\n" +
            $"Prosody Estimate: {speechResult.ProsodyScore}/10\n" +
            $"Words Per Minute: {speechResult.WordsPerMinute}\n" +
            $"Long Pauses: {speechResult.LongPauseCount}";

        answer.UpdatedAt = DateTime.UtcNow;
    }
    public async Task<ExerciseResultDto> GetAttemptResultAsync(
    int studentUserId,
    int exerciseAttemptId)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null)
        {
            throw new Exception("Student not found");
        }

        var attempt = await _unitOfWork.ExerciseAttempts
            .GetByIdAsync(exerciseAttemptId);

        if (attempt == null ||
            attempt.StudentRoleId != studentRole.StudentRoleId)
        {
            throw new Exception("Exercise attempt not found");
        }

        var writingSubmissions = await _unitOfWork.WritingSubmissions
            .Query()
            .Where(x => x.ExerciseAttemptId == attempt.ExerciseAttemptId)
            .ToListAsync();

        var writingSubmissionIds = writingSubmissions
            .Select(x => x.WritingSubmissionId)
            .ToList();

        var writingScores = await _unitOfWork.WritingScores
            .Query()
            .Where(x => writingSubmissionIds.Contains(x.WritingSubmissionId))
            .ToListAsync();

        return new ExerciseResultDto
        {
            ExerciseAttemptId = attempt.ExerciseAttemptId,
            ExerciseTitle = attempt.ExerciseAssignment.Exercise.Title,
            TotalQuestions = attempt.TotalQuestions,
            CorrectAnswers = attempt.CorrectAnswers,
            TotalPoints = attempt.TotalPoints,
            Score = attempt.Score,
            Status = attempt.Status.ToString(),
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt ?? attempt.StartedAt,
            Answers = attempt.Answers.Select(answer =>
            {
                if (answer.WritingTaskId != null)
                {
                    var writingTaskLink = attempt.ExerciseAssignment
                        .Exercise
                        .WritingTasks
                        .FirstOrDefault(x =>
                            x.WritingTaskId == answer.WritingTaskId);

                    var submission = writingSubmissions
                        .FirstOrDefault(x =>
                            x.WritingTaskId == answer.WritingTaskId);

                    var score = submission == null
                        ? null
                        : writingScores.FirstOrDefault(x =>
                            x.WritingSubmissionId ==
                            submission.WritingSubmissionId);

                    return new ExerciseAnswerResultDto
                    {
                        ExerciseAnswerId = answer.ExerciseAnswerId,
                        QuestionId = 0,
                        QuestionContent = writingTaskLink?.WritingTask.Title ?? "Writing Task",
                        WritingTaskId = answer.WritingTaskId,
                        WritingTaskTitle = writingTaskLink?.WritingTask.Title,
                        WritingInstruction =
                            writingTaskLink?.WritingTask.Instruction,
                        WritingPromptText =
                            writingTaskLink?.WritingTask.PromptText,
                        WritingPromptImageUrl =
                            writingTaskLink?.WritingTask.PromptImageUrl,
                        WritingReferenceText =
                            writingTaskLink?.WritingTask.ReferenceText,
                        WritingSourceText =
                            writingTaskLink?.WritingTask.SourceText,
                        WritingRequirements =
                            writingTaskLink?.WritingTask.Requirements,
                        WritingMinimumWords =
                            writingTaskLink?.WritingTask.MinimumWords,
                        TextAnswer = answer.TextAnswer,
                        IsCorrect = answer.IsCorrect,
                        PointEarned = answer.PointEarned,
                        ManualScore = answer.ManualScore,
                        TeacherFeedback = answer.TeacherFeedback,
                        WritingStatus = submission?.Status.ToString(),
                        WritingOverallBand = score?.OverallBand,
                        WritingTaskAchievement = score?.TaskAchievement,
                        WritingCoherenceAndCohesion = score?.CoherenceAndCohesion,
                        WritingLexicalResource = score?.LexicalResource,
                        WritingGrammaticalRangeAndAccuracy =
                            score?.GrammaticalRangeAndAccuracy,
                        WritingFeedback = score?.Feedback,
                        WritingImprovementSuggestionsJson =
                            score?.ImprovementSuggestionsJson
                    };
                }
                if (answer.SpeakingTaskId != null)
                {
                    var speakingTaskLink =
                        attempt.ExerciseAssignment
                            .Exercise
                            .SpeakingTasks
                            .FirstOrDefault(x =>
                                x.SpeakingTaskId ==
                                answer.SpeakingTaskId);

                    var speakingTask =
                        speakingTaskLink?.SpeakingTask;

                    return new ExerciseAnswerResultDto
                    {
                        ExerciseAnswerId =
                            answer.ExerciseAnswerId,

                        QuestionId = null,

                        SpeakingTaskId =
                            answer.SpeakingTaskId,

                        SpeakingTaskTitle =
                            speakingTask?.Title
                            ?? "Speaking Task",

                        SpeakingInstruction =
                            speakingTask?.Instruction,

                        SpeakingPromptText =
                            speakingTask?.PromptText,

                        SpeakingPromptImageUrl =
                            speakingTask?.PromptImageUrl,

                        SpeakingReferenceText =
                            speakingTask?.ReferenceText,

                        QuestionContent =
                            speakingTask?.Title
                            ?? "Speaking Task",

                        AudioAnswerUrl =
                            answer.SpeakingAudioUrl
                            ?? answer.AudioAnswerUrl,

                        SpeakingTranscript =
                            answer.SpeakingTranscript
                            ?? answer.TextAnswer,

                        TextAnswer =
                            answer.TextAnswer
                            ?? answer.SpeakingTranscript,

                        IsCorrect =
                            answer.IsCorrect,

                        PointEarned =
                            answer.PointEarned,

                        ManualScore =
                            answer.ManualScore,

                        TeacherFeedback =
                            answer.TeacherFeedback,

                        GradedByTeacherUserId =
                            answer.GradedByTeacherUserId,

                        GradedAt =
                            answer.GradedAt,

                        OverallSpeakingScore =
                            answer.OverallSpeakingScore,

                        PronunciationScore =
                            answer.PronunciationScore,

                        FluencyScore =
                            answer.FluencyScore,

                        GrammarScore =
                            answer.GrammarScore,

                        VocabularyScore =
                            answer.VocabularyScore,

                        AiSpeakingFeedback =
                            answer.AiSpeakingFeedback
                    };
                }
                if (answer.ListeningQuestionId != null)
                {
                    var listeningLink =
                        attempt.ExerciseAssignment
                            .Exercise
                            .ListeningTasks
                            .FirstOrDefault(link =>
                                link.ListeningTask
                                    .Questions
                                    .Any(question =>
                                        question.ListeningQuestionId ==
                                        answer.ListeningQuestionId));

                    var question =
                        listeningLink?
                            .ListeningTask
                            .Questions
                            .FirstOrDefault(x =>
                                x.ListeningQuestionId ==
                                answer.ListeningQuestionId);

                    var orderedOptions =
                        question?.Options
                            .OrderBy(x => x.OrderIndex)
                            .ToList()
                        ?? new List<ListeningOption>();

                    var selectedOption =
                        orderedOptions.FirstOrDefault(x =>
                            x.ListeningOptionId ==
                            answer.SelectedListeningOptionId);

                    var correctOption =
                        orderedOptions.FirstOrDefault(x =>
                            x.IsCorrect);

                    var selectedIndex =
                        selectedOption == null
                            ? -1
                            : orderedOptions.FindIndex(x =>
                                x.ListeningOptionId ==
                                selectedOption.ListeningOptionId);

                    var correctIndex =
                        correctOption == null
                            ? -1
                            : orderedOptions.FindIndex(x =>
                                x.ListeningOptionId ==
                                correctOption.ListeningOptionId);

                    return new ExerciseAnswerResultDto
                    {
                        ExerciseAnswerId =
                            answer.ExerciseAnswerId,

                        QuestionId =
                            null,

                        ListeningQuestionId =
                            answer.ListeningQuestionId,

                        ListeningTaskId =
                            listeningLink?.ListeningTaskId,

                        ListeningPromptImageUrl =
                            question?.PromptImageUrl,

                        QuestionContent =
                            question?.Content ?? "",

                        Explanation =
                            question?.Explanation,

                        SelectedQuestionOptionId =
                            answer.SelectedListeningOptionId,

                        CorrectQuestionOptionId =
                            correctOption?.ListeningOptionId,

                        SelectedAnswerLabel =
                            selectedOption == null
                                ? null
                                : GetOptionLabel(
                                    selectedIndex,
                                    selectedOption.Content),

                        CorrectAnswerLabel =
                            correctOption == null
                                ? null
                                : GetOptionLabel(
                                    correctIndex,
                                    correctOption.Content),

                        Options = orderedOptions
                            .Select((option, index) =>
                                new ExerciseAnswerOptionResultDto
                                {
                                    OptionId =
                                        option.ListeningOptionId,

                                    Label =
                                        ((char)('A' + index))
                                        .ToString(),

                                    Content =
                                        option.Content,

                                    IsSelected =
                                        option.ListeningOptionId ==
                                        answer.SelectedListeningOptionId,

                                    IsCorrect =
                                        option.IsCorrect
                                })
                            .ToList(),

                        IsCorrect =
                            answer.IsCorrect,

                        PointEarned =
                            answer.PointEarned,

                        ManualScore =
                            answer.ManualScore,

                        TeacherFeedback =
                            answer.TeacherFeedback,

                        GradedByTeacherUserId =
                            answer.GradedByTeacherUserId,

                        GradedAt =
                            answer.GradedAt
                    };
                }

                if (answer.ReadingQuestionId != null)
                {
                    var readingLink =
                        attempt.ExerciseAssignment
                            .Exercise
                            .ReadingTasks
                            .FirstOrDefault(link =>
                                link.ReadingTask
                                    .Questions
                                    .Any(question =>
                                        question.ReadingQuestionId ==
                                        answer.ReadingQuestionId));

                    var question =
                        readingLink?
                            .ReadingTask
                            .Questions
                            .FirstOrDefault(x =>
                                x.ReadingQuestionId ==
                                answer.ReadingQuestionId);

                    var orderedOptions =
                        question?.Options
                            .OrderBy(x => x.OrderIndex)
                            .ToList()
                        ?? new List<ReadingOption>();

                    var selectedOption =
                        orderedOptions.FirstOrDefault(x =>
                            x.ReadingOptionId ==
                            answer.SelectedReadingOptionId);

                    var correctOption =
                        orderedOptions.FirstOrDefault(x =>
                            x.IsCorrect);

                    var selectedIndex =
                        selectedOption == null
                            ? -1
                            : orderedOptions.FindIndex(x =>
                                x.ReadingOptionId ==
                                selectedOption.ReadingOptionId);

                    var correctIndex =
                        correctOption == null
                            ? -1
                            : orderedOptions.FindIndex(x =>
                                x.ReadingOptionId ==
                                correctOption.ReadingOptionId);

                    return new ExerciseAnswerResultDto
                    {
                        ExerciseAnswerId =
                            answer.ExerciseAnswerId,

                        QuestionId =
                            null,

                        ReadingQuestionId =
                            answer.ReadingQuestionId,

                        ReadingTaskId =
                            readingLink?.ReadingTaskId,
                        ReadingTaskTitle =
    readingLink?.ReadingTask.Title,

                        ReadingPassageTitle =
    readingLink?.ReadingTask.Title,

                        ReadingInstruction =
    readingLink?.ReadingTask.Instruction,

                        ReadingPassageText =
    readingLink?.ReadingTask.PassageText,
                        QuestionContent =
                            question?.Content ?? "",

                        Explanation =
                            question?.Explanation,

                        SelectedQuestionOptionId =
                            answer.SelectedReadingOptionId,

                        CorrectQuestionOptionId =
                            correctOption?.ReadingOptionId,

                        SelectedAnswerLabel =
                            selectedOption == null
                                ? null
                                : GetOptionLabel(
                                    selectedIndex,
                                    selectedOption.Content),

                        CorrectAnswerLabel =
                            correctOption == null
                                ? null
                                : GetOptionLabel(
                                    correctIndex,
                                    correctOption.Content),

                        Options = orderedOptions
                            .Select((option, index) =>
                                new ExerciseAnswerOptionResultDto
                                {
                                    OptionId =
                                        option.ReadingOptionId,

                                    Label =
                                        ((char)('A' + index))
                                        .ToString(),

                                    Content =
                                        option.Content,

                                    IsSelected =
                                        option.ReadingOptionId ==
                                        answer.SelectedReadingOptionId,

                                    IsCorrect =
                                        option.IsCorrect
                                })
                            .ToList(),

                        IsCorrect =
                            answer.IsCorrect,

                        PointEarned =
                            answer.PointEarned,

                        ManualScore =
                            answer.ManualScore,

                        TeacherFeedback =
                            answer.TeacherFeedback,

                        GradedByTeacherUserId =
                            answer.GradedByTeacherUserId,

                        GradedAt =
                            answer.GradedAt
                    };
                }
                if (answer.QuestionId != null)
                {
                    var question = answer.Question;

                    if (question == null)
                    {
                        return null;
                    }

                    var orderedOptions = question.Options
                        .OrderBy(x => x.OrderIndex)
                        .ToList();

                    var selectedOption = orderedOptions
                        .FirstOrDefault(x =>
                            x.QuestionOptionId ==
                            answer.SelectedQuestionOptionId);

                    var correctOption = orderedOptions
                        .FirstOrDefault(x => x.IsCorrect);

                    var selectedIndex =
                        selectedOption == null
                            ? -1
                            : orderedOptions.FindIndex(x =>
                                x.QuestionOptionId ==
                                selectedOption.QuestionOptionId);

                    var correctIndex =
                        correctOption == null
                            ? -1
                            : orderedOptions.FindIndex(x =>
                                x.QuestionOptionId ==
                                correctOption.QuestionOptionId);

                    return new ExerciseAnswerResultDto
                    {
                        ExerciseAnswerId =
                            answer.ExerciseAnswerId,

                        QuestionId =
                            answer.QuestionId,

                        QuestionContent =
                            question.Content,

                        Explanation =
                            question.Explanation,

                        SelectedQuestionOptionId =
                            answer.SelectedQuestionOptionId,

                        CorrectQuestionOptionId =
                            correctOption?.QuestionOptionId,

                        SelectedAnswerLabel =
                            selectedOption == null
                                ? null
                                : GetOptionLabel(
                                    selectedIndex,
                                    selectedOption.Content),

                        CorrectAnswerLabel =
                            correctOption == null
                                ? null
                                : GetOptionLabel(
                                    correctIndex,
                                    correctOption.Content),

                        Options = orderedOptions
                            .Select((option, index) =>
                                new ExerciseAnswerOptionResultDto
                                {
                                    OptionId =
                                        option.QuestionOptionId,

                                    Label =
                                        ((char)('A' + index))
                                        .ToString(),

                                    Content =
                                        option.Content,

                                    IsSelected =
                                        option.QuestionOptionId ==
                                        answer.SelectedQuestionOptionId,

                                    IsCorrect =
                                        option.IsCorrect
                                })
                            .ToList(),

                        TextAnswer =
                            answer.TextAnswer,

                        AudioAnswerUrl =
                            answer.AudioAnswerUrl,

                        IsCorrect =
                            answer.IsCorrect,

                        PointEarned =
                            answer.PointEarned,

                        ManualScore =
                            answer.ManualScore,

                        TeacherFeedback =
                            answer.TeacherFeedback,

                        GradedByTeacherUserId =
                            answer.GradedByTeacherUserId,

                        GradedAt =
                            answer.GradedAt
                    };
                }

                return null;
            })
.Where(x => x != null)
.Cast<ExerciseAnswerResultDto>()
.ToList()
        };
    }
    public async Task<ExerciseAnswerResultDto> SubmitSpeakingAnswerAsync(
    int studentUserId,
    int exerciseAttemptId,
    int speakingTaskId,
    Stream audioStream,
    string fileName,
    string contentType)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null)
        {
            throw new Exception("Student not found");
        }

        var attempt = await _unitOfWork.ExerciseAttempts
            .GetByIdAsync(exerciseAttemptId);

        if (attempt == null ||
            attempt.StudentRoleId != studentRole.StudentRoleId)
        {
            throw new Exception("Exercise attempt not found");
        }

        if (attempt.Status != ExerciseAttemptStatus.InProgress)
        {
            throw new Exception("Only in-progress attempt can submit speaking audio");
        }

        var speakingLink = attempt.ExerciseAssignment.Exercise
            .SpeakingTasks
            .FirstOrDefault(x => x.SpeakingTaskId == speakingTaskId);

        if (speakingLink == null)
        {
            throw new Exception("Speaking task not found in this exercise");
        }

        var objectName =
            $"speaking/answers/{studentUserId}/{Guid.NewGuid()}_{fileName}";

        var memoryStream = new MemoryStream();

        await audioStream.CopyToAsync(memoryStream);

        memoryStream.Position = 0;

        var audioUrl = await _minioService.UploadFileAsync(
            memoryStream,
            objectName,
            contentType);

        var oldAnswer = attempt.Answers
            .FirstOrDefault(x => x.SpeakingTaskId == speakingTaskId);

        if (oldAnswer != null)
        {
            oldAnswer.SpeakingAudioUrl = audioUrl;
            oldAnswer.AudioAnswerUrl = audioUrl;
            oldAnswer.PointEarned = 0;
            oldAnswer.IsCorrect = false;
            oldAnswer.TeacherFeedback = "Speaking audio uploaded. Waiting for final submission.";
            oldAnswer.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            attempt.Answers.Add(new ExerciseAnswer
            {
                SpeakingTaskId = speakingTaskId,
                SpeakingAudioUrl = audioUrl,
                AudioAnswerUrl = audioUrl,
                IsCorrect = false,
                PointEarned = 0,
                TeacherFeedback = "Speaking audio uploaded. Waiting for final submission."
            });
        }

        await _unitOfWork.SaveChangesAsync();

        return new ExerciseAnswerResultDto
        {
            QuestionId = null,

            SpeakingTaskId =
        speakingTaskId,

            SpeakingTaskTitle =
        speakingLink.SpeakingTask.Title,

            SpeakingInstruction =
        speakingLink.SpeakingTask.Instruction,

            SpeakingPromptText =
        speakingLink.SpeakingTask.PromptText,

            SpeakingPromptImageUrl =
        speakingLink.SpeakingTask.PromptImageUrl,

            SpeakingReferenceText =
        speakingLink.SpeakingTask.ReferenceText,

            QuestionContent =
        speakingLink.SpeakingTask.Title,

            AudioAnswerUrl =
        audioUrl,

            IsCorrect =
        false,

            PointEarned =
        0,

            TeacherFeedback =
        "Đã lưu audio. Hệ thống sẽ chấm khi bạn bấm Nộp bài."
        };
    }
    public async Task<List<StudentExerciseHistoryDto>> GetMyHistoryAsync(
    int studentUserId)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null)
        {
            return new List<StudentExerciseHistoryDto>();
        }

        return await _unitOfWork.ExerciseAttempts
            .Query()
            .Where(x => x.StudentRoleId == studentRole.StudentRoleId)
            .OrderByDescending(x => x.StartedAt)
            .Select(x => new StudentExerciseHistoryDto
            {
                ExerciseAttemptId = x.ExerciseAttemptId,
                ExerciseAssignmentId = x.ExerciseAssignmentId,
                ExerciseTitle = x.ExerciseAssignment.Exercise.Title,
                TotalQuestions = x.TotalQuestions,
                CorrectAnswers = x.CorrectAnswers,
                TotalPoints = x.TotalPoints,
                Score = x.Score,
                StartedAt = x.StartedAt,
                SubmittedAt = x.SubmittedAt,
                Status = x.Status.ToString()
            })
            .ToListAsync();
    }
   

    private string NormalizeAnswer(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim().ToLower();
    }
    private ExerciseAttemptDto BuildExerciseAttemptDto(
    ExerciseAttempt attempt,
    ExerciseAssignment assignment)
    {
        var normalQuestions = assignment.Exercise.ExerciseQuestions
            .Concat(
                assignment.Exercise.QuestionGroups
                    .SelectMany(g => g.ExerciseQuestions)
            )
            .OrderBy(x => x.OrderIndex)
            .Select(x => new StudentAttemptQuestionDto
            {
                QuestionId = x.QuestionId,
                Content = x.Question.Content,
                QuestionType = x.Question.QuestionType,
                OrderIndex = x.OrderIndex,
                Point = x.Point,
                IsListeningQuestion = false,
                Options = x.Question.Options
                    .OrderBy(o => o.OrderIndex)
                    .Select(o => new StudentQuestionOptionDto
                    {
                        QuestionOptionId = o.QuestionOptionId,
                        Content = o.Content,
                        OrderIndex = o.OrderIndex
                    })
                    .ToList()
            })
            .ToList();

        var listeningQuestions = new List<StudentAttemptQuestionDto>();

        var globalOrderIndex = normalQuestions.Count;

        foreach (var link in assignment.Exercise.ListeningTasks
            .OrderBy(x => x.OrderIndex))
        {
            foreach (var q in link.ListeningTask.Questions
                .OrderBy(x => x.OrderIndex))
            {
                globalOrderIndex++;

                listeningQuestions.Add(new StudentAttemptQuestionDto
                {
                    QuestionId = 0,
                    ListeningQuestionId = q.ListeningQuestionId,
                    ListeningTaskId = link.ListeningTaskId,
                    Content = q.Content,
                    QuestionType = QuestionType.SingleChoice,
                    OrderIndex = globalOrderIndex,
                    Point = q.Point,
                    IsListeningQuestion = true,
                    ListeningCourseType =
                        link.ListeningTask.CourseType,
                    ListeningPartTitle =
                        link.ListeningTask.PartTitle,
                    ListeningDirection =
                        link.ListeningTask.Direction,
                    ListeningLittlePart =
                        link.ListeningTask.LittlePart,
                    AudioUrl = link.ListeningTask.AudioUrl,
                    ListeningPromptImageUrl =
                        q.PromptImageUrl,
                    Transcript = link.ListeningTask.Transcript,
                    Options = q.Options
                        .OrderBy(o => o.OrderIndex)
                        .Select(o => new StudentQuestionOptionDto
                        {
                            QuestionOptionId = 0,
                            ListeningOptionId = o.ListeningOptionId,
                            Content = o.Content,
                            OrderIndex = o.OrderIndex
                        })
                        .ToList()
                });
            }
        }
        var readingQuestions = new List<StudentAttemptQuestionDto>();

        foreach (var link in assignment.Exercise.ReadingTasks
            .OrderBy(x => x.OrderIndex))
        {
            foreach (var q in link.ReadingTask.Questions
                .OrderBy(x => x.OrderIndex))
            {
                globalOrderIndex++;

                readingQuestions.Add(new StudentAttemptQuestionDto
                {
                    QuestionId = 0,
                    ReadingQuestionId = q.ReadingQuestionId,
                    ReadingTaskId = link.ReadingTaskId,
                    ReadingQuestionOrderIndex = q.OrderIndex,
                    Content = q.Content,
                    QuestionType = QuestionType.SingleChoice,
                    OrderIndex = globalOrderIndex,
                    Point = q.Point,
                    IsListeningQuestion = false,
                    IsReadingQuestion = true,
                    ReadingPassageTitle = link.ReadingTask.Title,
                    ReadingPassageText = link.ReadingTask.PassageText,
                    ReadingInstruction = link.ReadingTask.Instruction,
                    Options = q.Options
                        .OrderBy(o => o.OrderIndex)
                        .Select(o => new StudentQuestionOptionDto
                        {
                            QuestionOptionId = 0,
                            ReadingOptionId = o.ReadingOptionId,
                            Content = o.Content,
                            OrderIndex = o.OrderIndex
                        })
                        .ToList()
                });
            }
        }
        var speakingQuestions = new List<StudentAttemptQuestionDto>();

        foreach (var link in assignment.Exercise.SpeakingTasks
            .OrderBy(x => x.OrderIndex))
        {
            globalOrderIndex++;

            speakingQuestions.Add(new StudentAttemptQuestionDto
            {
                QuestionId = 0,
                SpeakingTaskId = link.SpeakingTaskId,
                Content = link.SpeakingTask.Title,
                QuestionType = QuestionType.AudioAnswer,
                OrderIndex = globalOrderIndex,
                Point = link.Point,
                IsListeningQuestion = false,
                IsSpeakingQuestion = true,
                SpeakingPdfUrl = link.SpeakingTask.PdfUrl,
                SpeakingInstruction = link.SpeakingTask.Instruction,
                SpeakingPromptText = link.SpeakingTask.PromptText,
                SpeakingPromptImageUrl =
                    link.SpeakingTask.PromptImageUrl,
                SpeakingReferenceText = link.SpeakingTask.ReferenceText,
                PreparationTimeSeconds = link.SpeakingTask.PreparationTimeSeconds,
                SpeakingTimeSeconds = link.SpeakingTask.SpeakingTimeSeconds,
                Options = new List<StudentQuestionOptionDto>()
            });
        }
        var writingQuestions = new List<StudentAttemptQuestionDto>();

        foreach (var link in assignment.Exercise.WritingTasks
            .OrderBy(x => x.OrderIndex))
        {
            globalOrderIndex++;

            writingQuestions.Add(new StudentAttemptQuestionDto
            {
                QuestionId = 0,
                WritingTaskId = link.WritingTaskId,
                Content = link.WritingTask.Title,
                QuestionType = QuestionType.TextAnswer,
                OrderIndex = globalOrderIndex,
                Point = link.Point,
                IsListeningQuestion = false,
                WritingSourceText = link.WritingTask.SourceText,
                WritingRequirements = link.WritingTask.Requirements,
                WritingScoringCriteria = link.WritingTask.ScoringCriteria,
                WritingMinimumWords = link.WritingTask.MinimumWords,
                WritingTaskType = link.WritingTask.TaskType.ToString(),
                IsWritingQuestion = true,
                WritingInstruction = link.WritingTask.Instruction,
                WritingPromptText = link.WritingTask.PromptText,
                WritingPromptImageUrl = link.WritingTask.PromptImageUrl,
                WritingReferenceText = link.WritingTask.ReferenceText,
                Options = new List<StudentQuestionOptionDto>()
            });
        }
        return new ExerciseAttemptDto
        {
            ExerciseAttemptId = attempt.ExerciseAttemptId,
            ExerciseAssignmentId = assignment.ExerciseAssignmentId,
            ExerciseTitle = assignment.Exercise.Title,
            StartedAt = attempt.StartedAt,
            Questions = normalQuestions
    .Concat(listeningQuestions)
    .Concat(readingQuestions)
    .Concat(speakingQuestions)
    .Concat(writingQuestions)
    .OrderBy(x => x.OrderIndex)
    .ToList()
        };
    }
    public async Task<List<StudentExerciseSkillProgressDto>>
    GetMySkillProgressAsync(int studentUserId)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .Include(x => x.ClassRoom)
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null ||
            studentRole.ClassRoomId == null)
        {
            return new List<StudentExerciseSkillProgressDto>();
        }

        var classRoomId = studentRole.ClassRoomId.Value;

        var assignments = await _unitOfWork.ExerciseAssignments
            .Query()
            .Where(x =>
                x.ClassRoomId == classRoomId &&
                x.IsActive &&
                x.Exercise.IsActive)
            .ToListAsync();

        var completedAttempts = await _unitOfWork.ExerciseAttempts
            .Query()
            .Where(x =>
                x.StudentRoleId == studentRole.StudentRoleId &&
                (
                    x.Status == ExerciseAttemptStatus.Submitted ||
x.Status == ExerciseAttemptStatus.Reviewed ||
x.Status == ExerciseAttemptStatus.AiGraded
                ))
            .ToListAsync();

        var skills = new[]
        {
        SkillType.Grammar,
        SkillType.Listening,
        SkillType.Speaking,
        SkillType.Reading,
        SkillType.Writing
    };

        return skills.Select(skill =>
        {
            var skillAssignments = assignments
                .Where(x => x.Exercise.Skill == skill)
                .ToList();

            var skillAssignmentIds = skillAssignments
                .Select(x => x.ExerciseAssignmentId)
                .ToList();

            var skillAttempts = completedAttempts
                .Where(x =>
                    skillAssignmentIds.Contains(
                        x.ExerciseAssignmentId))
                .ToList();

            var completed = skillAttempts
                .Select(x => x.ExerciseAssignmentId)
                .Distinct()
                .Count();

            var total = skillAssignments.Count;

            var lastAttemptAt = skillAttempts
                .Where(x => x.SubmittedAt != null)
                .OrderByDescending(x => x.SubmittedAt)
                .Select(x => x.SubmittedAt)
                .FirstOrDefault();

            return new StudentExerciseSkillProgressDto
            {
                Skill = skill,
                SkillName = skill.ToString(),
                TotalAssigned = total,
                Completed = completed,
                CompletionPercent = total == 0
                    ? 0
                    : Math.Round((double)completed / total * 100, 2),
                AverageScore = skillAttempts.Any()
                    ? Math.Round(skillAttempts.Average(x => x.Score), 2)
                    : 0,
                HighestScore = skillAttempts.Any()
                    ? Math.Round(skillAttempts.Max(x => x.Score), 2)
                    : 0,
                LastAttemptAt = lastAttemptAt,
                CurrentStreakDays = CalculateStreakDays(skillAttempts)
            };
        }).ToList();
    }
    private int CalculateStreakDays(List<ExerciseAttempt> attempts)
    {
        var submittedDates = attempts
            .Where(x => x.SubmittedAt != null)
            .Select(x => x.SubmittedAt!.Value.Date)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        if (!submittedDates.Any())
        {
            return 0;
        }

        var today = DateTime.UtcNow.Date;
        var streak = 0;

        foreach (var date in submittedDates)
        {
            var expectedDate = today.AddDays(-streak);

            if (date == expectedDate)
            {
                streak++;
                continue;
            }

            if (streak == 0 && date == today.AddDays(-1))
            {
                streak++;
                continue;
            }

            break;
        }

        return streak;
    }
    public async Task<ExerciseAttemptDto> StartAttemptAsync(
        int studentUserId,
        StartExerciseAttemptDto dto)
    {
        var studentRole = await _unitOfWork.StudentRoles
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);

        if (studentRole == null ||
            studentRole.ClassRoomId == null)
        {
            throw new Exception("Student has no class");
        }

        var assignment = await _unitOfWork.ExerciseAssignments
    .Query()
    .Include(x => x.Exercise)
        .ThenInclude(x => x.ExerciseQuestions)
            .ThenInclude(x => x.Question)
                .ThenInclude(x => x.Options)
    .Include(x => x.Exercise)
        .ThenInclude(x => x.QuestionGroups)
            .ThenInclude(x => x.ExerciseQuestions)
                .ThenInclude(x => x.Question)
                    .ThenInclude(x => x.Options)
    .Include(x => x.Exercise)
        .ThenInclude(x => x.ListeningTasks)
            .ThenInclude(x => x.ListeningTask)
                .ThenInclude(x => x.Questions)
                    .ThenInclude(x => x.Options)
    .Include(x => x.Exercise)
        .ThenInclude(x => x.ReadingTasks)
            .ThenInclude(x => x.ReadingTask)
                .ThenInclude(x => x.Questions)
                    .ThenInclude(x => x.Options)
    .Include(x => x.Exercise)
        .ThenInclude(x => x.SpeakingTasks)
            .ThenInclude(x => x.SpeakingTask)
            .Include(x => x.Exercise)
    .ThenInclude(x => x.WritingTasks)
        .ThenInclude(x => x.WritingTask)
    .FirstOrDefaultAsync(x =>
        x.ExerciseAssignmentId == dto.ExerciseAssignmentId);

        if (assignment == null ||
            !assignment.IsActive)
        {
            throw new Exception("Exercise assignment not found");
        }

        if (assignment.ClassRoomId != studentRole.ClassRoomId.Value)
        {
            throw new Exception("You cannot access this exercise");
        }

        var now = DateTime.UtcNow;

        

        var inProgressAttempt = await _unitOfWork.ExerciseAttempts
    .Query()
    .FirstOrDefaultAsync(x =>
        x.ExerciseAssignmentId == dto.ExerciseAssignmentId &&
        x.StudentRoleId == studentRole.StudentRoleId &&
        x.Status == ExerciseAttemptStatus.InProgress);

        if (inProgressAttempt != null)
        {
            return BuildExerciseAttemptDto(
                inProgressAttempt,
                assignment
            );
        }

        var usedAttemptCount = await _unitOfWork.ExerciseAttempts
            .Query()
            .CountAsync(x =>
                x.ExerciseAssignmentId == dto.ExerciseAssignmentId &&
                x.StudentRoleId == studentRole.StudentRoleId &&
                x.Status != ExerciseAttemptStatus.InProgress);

        if (usedAttemptCount >= assignment.MaxAttempts)
        {
            throw new Exception("Maximum attempts reached");
        }

        var attempt = new ExerciseAttempt
        {
            ExerciseAssignmentId = dto.ExerciseAssignmentId,
            StudentRoleId = studentRole.StudentRoleId,
            StartedAt = now,
            Status = ExerciseAttemptStatus.InProgress
        };

        await _unitOfWork.ExerciseAttempts.AddAsync(attempt);
        await _unitOfWork.SaveChangesAsync();

        return BuildExerciseAttemptDto(
            attempt,
            assignment
        );
    
        
    }
}
