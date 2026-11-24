using System.Text.Json;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class QuestionService
{
    private readonly IUnitOfWork _unitOfWork;

    public QuestionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<QuestionDto>> GetQuestionsAsync(
        int teacherUserId,
        int? categoryId,
        QuestionType? questionType,
        CourseType? courseType,
        SkillType? skill)
    {
        var query = _unitOfWork.Questions.Query()
            .Where(x => x.TeacherUserId == teacherUserId);

        if (categoryId.HasValue)
            query = query.Where(x => x.QuestionCategoryId == categoryId.Value);
        if (questionType.HasValue)
            query = query.Where(x => x.QuestionType == questionType.Value);
        if (courseType.HasValue)
            query = query.Where(x => x.CourseType == courseType.Value);
        if (skill.HasValue)
            query = query.Where(x => x.Skill == skill.Value);

        var questions = await query.OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return questions.Select(MapQuestion).ToList();
    }

    public async Task<List<QuestionCategoryDto>> GetCategoriesAsync()
    {
        return await _unitOfWork.Questions.QueryCategories()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new QuestionCategoryDto
            {
                QuestionCategoryId = x.QuestionCategoryId,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<int> CreateQuestionAsync(
        int teacherUserId,
        CreateQuestionDto dto)
    {
        ValidateQuestion(dto.Content, dto.QuestionType, dto.Options,
            dto.AcceptedAnswers, dto.CorrectTextAnswer);
        await EnsureCategoryExistsAsync(dto.QuestionCategoryId);

        var question = new Question
        {
            TeacherUserId = teacherUserId,
            QuestionCategoryId = dto.QuestionCategoryId,
            Content = dto.Content.Trim(),
            QuestionType = dto.QuestionType,
            CourseType = dto.CourseType,
            Skill = dto.Skill,
            Explanation = TrimOrNull(dto.Explanation),
            Passage = TrimOrNull(dto.Passage),
            AudioUrl = TrimOrNull(dto.AudioUrl),
            CorrectTextAnswer = TrimOrNull(dto.CorrectTextAnswer),
            AcceptedAnswersJson = SerializeAnswers(dto.AcceptedAnswers),
            Options = dto.Options.Select((option, index) =>
                new QuestionOption
                {
                    Content = option.Content.Trim(),
                    IsCorrect = option.IsCorrect,
                    OrderIndex = index + 1
                }).ToList()
        };

        await _unitOfWork.Questions.AddAsync(question);
        await _unitOfWork.SaveChangesAsync();
        return question.QuestionId;
    }

    public async Task UpdateQuestionAsync(
        int teacherUserId,
        int questionId,
        UpdateQuestionDto dto)
    {
        ValidateQuestion(dto.Content, dto.QuestionType, dto.Options,
            dto.AcceptedAnswers, dto.CorrectTextAnswer);
        await EnsureCategoryExistsAsync(dto.QuestionCategoryId);

        var question = await _unitOfWork.Questions.GetByIdAsync(questionId);
        if (question == null || question.TeacherUserId != teacherUserId)
            throw new Exception("Question not found");

        question.QuestionCategoryId = dto.QuestionCategoryId;
        question.Content = dto.Content.Trim();
        question.QuestionType = dto.QuestionType;
        question.CourseType = dto.CourseType;
        question.Skill = dto.Skill;
        question.Explanation = TrimOrNull(dto.Explanation);
        question.Passage = TrimOrNull(dto.Passage);
        question.AudioUrl = TrimOrNull(dto.AudioUrl);
        question.CorrectTextAnswer = TrimOrNull(dto.CorrectTextAnswer);
        question.AcceptedAnswersJson = SerializeAnswers(dto.AcceptedAnswers);
        question.IsActive = dto.IsActive;
        question.UpdatedAt = DateTime.UtcNow;
        question.Options.Clear();
        foreach (var (option, index) in dto.Options.Select((value, index) => (value, index)))
        {
            question.Options.Add(new QuestionOption
            {
                Content = option.Content.Trim(),
                IsCorrect = option.IsCorrect,
                OrderIndex = index + 1
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteQuestionAsync(int teacherUserId, int questionId)
    {
        var question = await _unitOfWork.Questions.GetByIdAsync(questionId);
        if (question == null || question.TeacherUserId != teacherUserId)
            throw new Exception("Question not found");

        _unitOfWork.Questions.Remove(question);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task AssignQuestionsAsync(
        int teacherUserId,
        CreateQuestionAssignmentDto dto)
    {
        var questionIds = dto.QuestionIds.Distinct().ToList();
        if (!questionIds.Any() || dto.GroupNumber < 1)
            throw new Exception("Select at least one question");

        var classRoom = await _unitOfWork.ClassRooms.Query()
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.ClassRoomId == dto.ClassRoomId && x.IsOpen);
        if (classRoom == null)
            throw new Exception("Class room not found");

        var teacherHasClass = await _unitOfWork.TeacherRoleRepository.Query()
            .Where(x => x.UserId == teacherUserId)
            .SelectMany(x => x.TeacherClasses)
            .AnyAsync(x => x.ClassRoomId == dto.ClassRoomId);
        if (!teacherHasClass)
            throw new Exception("Teacher is not assigned to this class");

        var questions = await _unitOfWork.Questions.Query()
            .Where(x => questionIds.Contains(x.QuestionId) &&
                x.TeacherUserId == teacherUserId && x.IsActive)
            .ToListAsync();
        if (questions.Count != questionIds.Count)
            throw new Exception("Some questions are invalid");

        var courseType = ParseCourseType(classRoom.Course.CourseType);
        if (questions.Any(x => x.CourseType != courseType))
            throw new Exception("Questions must belong to the class course");

        var existingAssignments = await _unitOfWork
            .QuestionAssignments.QueryIncludingDeleted()
            .Where(x => x.ClassRoomId == dto.ClassRoomId &&
                questionIds.Contains(x.QuestionId))
            .ToListAsync();

        foreach (var question in questions)
        {
            var existingAssignment = existingAssignments.FirstOrDefault(x =>
                x.QuestionId == question.QuestionId);

            if (existingAssignment != null)
            {
                if (existingAssignment.IsDeleted || !existingAssignment.IsActive)
                {
                    existingAssignment.IsDeleted = false;
                    existingAssignment.IsActive = true;
                    existingAssignment.UpdatedAt = DateTime.UtcNow;
                }
                existingAssignment.GroupNumber = dto.GroupNumber;

                continue;
            }

            await _unitOfWork.QuestionAssignments.AddAsync(new QuestionAssignment
            {
                QuestionId = question.QuestionId,
                ClassRoomId = dto.ClassRoomId
                ,GroupNumber = dto.GroupNumber
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<QuestionAssignmentDto>> GetAssignmentsAsync(
        int teacherUserId)
    {
        return await _unitOfWork.QuestionAssignments.Query()
            .Where(x => x.Question.TeacherUserId == teacherUserId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new QuestionAssignmentDto
            {
                QuestionAssignmentId = x.QuestionAssignmentId,
                QuestionId = x.QuestionId,
                ClassRoomId = x.ClassRoomId,
                ClassRoomName = x.ClassRoom.ClassName,
                IsActive = x.IsActive
                ,GroupNumber = x.GroupNumber
            }).ToListAsync();
    }

    public async Task RemoveAssignmentAsync(int teacherUserId, int questionAssignmentId)
    {
        var assignment = await _unitOfWork.QuestionAssignments
            .GetByIdAsync(questionAssignmentId);
        if (assignment == null || assignment.Question.TeacherUserId != teacherUserId)
            throw new Exception("Question assignment not found");

        _unitOfWork.QuestionAssignments.Remove(assignment);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<QuestionDto>> GetStudentQuestionsAsync(int studentUserId)
    {
        var student = await _unitOfWork.StudentRoleRepository.Query()
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);
        if (student?.ClassRoomId == null || student.CourseId == null)
            return new List<QuestionDto>();

        var assignments = await _unitOfWork.QuestionAssignments.Query()
            .Where(x => x.ClassRoomId == student.ClassRoomId &&
                x.IsActive && x.Question.IsActive)
            .OrderBy(x => x.QuestionAssignmentId)
            .ToListAsync();

        var questions = assignments.Select(x => { var question = MapQuestion(x.Question); question.GroupNumber = x.GroupNumber; return question; }).ToList();

        foreach (var question in questions)
        {
            question.CorrectTextAnswer = null;
            question.AcceptedAnswers.Clear();
            foreach (var option in question.Options)
                option.IsCorrect = false;
        }

        return questions;
    }

    public async Task<QuestionAnswerResultDto> CheckStudentAnswerAsync(
        int studentUserId,
        int questionId,
        SubmitQuestionAnswerDto dto)
    {
        var student = await _unitOfWork.StudentRoleRepository.Query()
            .FirstOrDefaultAsync(x => x.UserId == studentUserId);
        if (student?.ClassRoomId == null)
            throw new Exception("Student is not assigned to a class");

        var directAssignmentExists = await _unitOfWork.QuestionAssignments.Query()
            .Where(x => x.ClassRoomId == student.ClassRoomId &&
                x.QuestionId == questionId && x.IsActive && x.Question.IsActive)
            .AnyAsync();
        if (!directAssignmentExists)
            throw new Exception("Question is not assigned to your class");
        var question = await _unitOfWork.Questions.Query()
            .FirstOrDefaultAsync(x => x.QuestionId == questionId && x.IsActive);
        if (question == null) throw new Exception("Question not found");

        var correctOptionIds = question.Options.Where(x => x.IsCorrect)
            .Select(x => x.QuestionOptionId).ToList();
        var isCorrect = question.QuestionType is QuestionType.SingleChoice or QuestionType.TrueFalse
            ? dto.SelectedOptionId.HasValue && correctOptionIds.Contains(dto.SelectedOptionId.Value)
            : GetAcceptedAnswers(question).Any(answer =>
                Normalize(answer) == Normalize(dto.AnswerText));

        return new QuestionAnswerResultDto
        {
            IsCorrect = isCorrect,
            Explanation = question.Explanation,
            CorrectOptionIds = correctOptionIds,
            CorrectTextAnswer = question.QuestionType is QuestionType.SingleChoice or QuestionType.TrueFalse
                ? null
                : GetAcceptedAnswers(question).FirstOrDefault()
        };
    }

    private async Task EnsureCategoryExistsAsync(int questionCategoryId)
    {
        if (!await _unitOfWork.Questions.QueryCategories()
            .AnyAsync(x => x.QuestionCategoryId == questionCategoryId && x.IsActive))
            throw new Exception("Question category not found");
    }

    private static void ValidateQuestion(
        string content,
        QuestionType questionType,
        List<CreateQuestionOptionDto> options,
        List<string> acceptedAnswers,
        string? correctTextAnswer)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new Exception("Question content is required");

        if (questionType is QuestionType.SingleChoice or QuestionType.TrueFalse)
        {
            if (options.Count < 2 || options.Any(x => string.IsNullOrWhiteSpace(x.Content)) ||
                options.Count(x => x.IsCorrect) != 1)
                throw new Exception("A choice question needs at least two options and one correct answer");
            return;
        }

        if (!acceptedAnswers.Any(x => !string.IsNullOrWhiteSpace(x)) &&
            string.IsNullOrWhiteSpace(correctTextAnswer))
            throw new Exception("Provide at least one accepted answer");
    }

    private static QuestionDto MapQuestion(Question question) => new()
    {
        QuestionId = question.QuestionId,
        QuestionCategoryId = question.QuestionCategoryId,
        QuestionCategoryName = question.QuestionCategory.Name,
        TeacherUserId = question.TeacherUserId,
        TeacherName = question.TeacherUser.FullName,
        Content = question.Content,
        QuestionType = question.QuestionType,
        CourseType = question.CourseType,
        Skill = question.Skill,
        Explanation = question.Explanation,
        Passage = question.Passage,
        AudioUrl = question.AudioUrl,
        CorrectTextAnswer = question.CorrectTextAnswer,
        AcceptedAnswers = GetAcceptedAnswers(question),
        IsActive = question.IsActive,
        Options = question.Options.OrderBy(x => x.OrderIndex).Select(x => new QuestionOptionDto
        {
            QuestionOptionId = x.QuestionOptionId,
            Content = x.Content,
            IsCorrect = x.IsCorrect,
            OrderIndex = x.OrderIndex
        }).ToList()
    };

    private static string? SerializeAnswers(IEnumerable<string> answers)
    {
        var values = answers.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return values.Count == 0 ? null : JsonSerializer.Serialize(values);
    }

    private static List<string> GetAcceptedAnswers(Question question)
    {
        if (!string.IsNullOrWhiteSpace(question.AcceptedAnswersJson))
        {
            try { return JsonSerializer.Deserialize<List<string>>(question.AcceptedAnswersJson) ?? new(); }
            catch (JsonException) { }
        }
        return string.IsNullOrWhiteSpace(question.CorrectTextAnswer)
            ? new List<string>() : new List<string> { question.CorrectTextAnswer };
    }

    private static string Normalize(string? value)
        => new string((value ?? string.Empty).Trim().ToLowerInvariant()
            .Where(x => !char.IsPunctuation(x)).ToArray())
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Aggregate(string.Empty, (current, part) =>
                string.IsNullOrEmpty(current) ? part : $"{current} {part}");

    private static CourseType ParseCourseType(string courseType)
    {
        if (Enum.TryParse<CourseType>(courseType, true, out var value))
            return value;
        throw new Exception("Class course type is invalid");
    }

    private static string? TrimOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
