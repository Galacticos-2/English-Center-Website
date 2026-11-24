using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace EnglishCenterSystem.Application.Features.Exercises.Services;
public class QuestionMapService
{
    private readonly IUnitOfWork _uow;
    public QuestionMapService(IUnitOfWork uow) => _uow = uow;
    public async Task<List<QuestionMapDto>> GetTeacherMapsAsync(int teacherId) => (await _uow.QuestionMaps.Query().Where(x => x.TeacherUserId == teacherId).OrderByDescending(x => x.CreatedAt).ToListAsync()).Select(Map).ToList();
    public async Task<int> CreateMapAsync(int teacherId, CreateQuestionMapDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) throw new Exception("Map title is required");
        var map = new QuestionMap { TeacherUserId = teacherId, Title = dto.Title.Trim(), Description = dto.Description?.Trim(), CourseType = dto.CourseType };
        await _uow.QuestionMaps.AddAsync(map); await _uow.SaveChangesAsync(); return map.QuestionMapId;
    }
    public async Task GenerateMapsFromExistingQuestionsAsync(int teacherId)
    {
        var questions = await _uow.Questions.Query().Where(x => x.TeacherUserId == teacherId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.QuestionId).ToListAsync();
        var existingCourseTypes = await _uow.QuestionMaps.Query().Where(x => x.TeacherUserId == teacherId)
            .Select(x => x.CourseType).ToListAsync();
        var teacherClasses = await _uow.TeacherRoleRepository.Query().Where(x => x.UserId == teacherId)
            .SelectMany(x => x.TeacherClasses).Select(x => new { x.ClassRoomId, x.ClassRoom.Course.CourseType }).ToListAsync();
        foreach (var courseQuestions in questions.GroupBy(x => x.CourseType).Where(x => !existingCourseTypes.Contains(x.Key)))
        {
            var classIds = teacherClasses.Where(x => MatchesCourseType(x.CourseType, courseQuestions.Key)).Select(x => x.ClassRoomId).Distinct().ToList();
            if (!classIds.Any()) continue;
            var map = new QuestionMap { TeacherUserId = teacherId, Title = $"{courseQuestions.Key} Question Map", Description = "Auto-generated from the existing question bank.", CourseType = courseQuestions.Key };
            var groupIndex = 1;
            foreach (var batch in courseQuestions.Chunk(10))
            {
                var questionIds = batch.Select(x => x.QuestionId).ToList();
                map.Groups.Add(new QuestionGroup { Title = $"Group {groupIndex}", QuestionCount = questionIds.Count, OrderIndex = groupIndex++, Questions = questionIds.Select((id, index) => new QuestionGroupQuestion { QuestionId = id, OrderIndex = index + 1 }).ToList(), Assignments = classIds.Select(id => new QuestionGroupAssignment { ClassRoomId = id }).ToList() });
            }
            await _uow.QuestionMaps.AddAsync(map);
        }
        await _uow.SaveChangesAsync();
    }
    public async Task<int> CreateGroupAsync(int teacherId, int mapId, CreateQuestionGroupDto dto)
    {
        var map = await _uow.QuestionMaps.GetByIdAsync(mapId);
        if (map == null || map.TeacherUserId != teacherId) throw new Exception("Question map not found");
        var questionIds = dto.QuestionIds.Distinct().ToList();
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.QuestionCount <= 0 || questionIds.Count != dto.QuestionCount || !dto.ClassRoomIds.Any()) throw new Exception("Group needs a title, classes, and exactly the configured number of questions");
        var validQuestionCount = await _uow.Questions.Query().CountAsync(x => questionIds.Contains(x.QuestionId) && x.TeacherUserId == teacherId && x.IsActive && x.CourseType == map.CourseType);
        if (validQuestionCount != questionIds.Count) throw new Exception("Selected questions do not belong to this map course");
        var classIds = dto.ClassRoomIds.Distinct().ToList();
        var teacherClasses = await _uow.TeacherRoleRepository.Query()
            .Where(x => x.UserId == teacherId)
            .SelectMany(x => x.TeacherClasses)
            .Where(x => classIds.Contains(x.ClassRoomId))
            .Select(x => new { x.ClassRoomId, x.ClassRoom.Course.CourseType })
            .ToListAsync();
        if (teacherClasses.Count != classIds.Count || teacherClasses.Any(x => !MatchesCourseType(x.CourseType, map.CourseType)))
            throw new Exception("Select only classes you teach in this map course");
        var group = new QuestionGroup { Title = dto.Title.Trim(), Description = dto.Description?.Trim(), Icon = dto.Icon, Color = dto.Color, QuestionCount = dto.QuestionCount, OrderIndex = map.Groups.Count + 1, Questions = questionIds.Select((id, i) => new QuestionGroupQuestion { QuestionId = id, OrderIndex = i + 1 }).ToList(), Assignments = classIds.Select(id => new QuestionGroupAssignment { ClassRoomId = id }).ToList() };
        map.Groups.Add(group); await _uow.SaveChangesAsync(); return group.QuestionGroupId;
    }
    public async Task<List<QuestionMapDto>> GetStudentMapsAsync(int userId)
    {
        var student = await _uow.StudentRoleRepository.Query().FirstOrDefaultAsync(x => x.UserId == userId);
        if (student?.ClassRoomId == null) return new();
        var maps = await _uow.QuestionMaps.Query().Where(x => x.Groups.Any(g => g.Assignments.Any(a => a.ClassRoomId == student.ClassRoomId))).OrderBy(x => x.CreatedAt).ToListAsync();
        var progressIds = await GetProgressIdsAsync(student.StudentRoleId);
        return maps.Select(map => MapForStudent(map, student.ClassRoomId.Value, progressIds)).ToList();
    }
    public async Task<List<QuestionDto>> GetStudentGroupQuestionsAsync(int userId, int groupId)
    {
        var student = await _uow.StudentRoleRepository.Query().FirstOrDefaultAsync(x => x.UserId == userId);
        if (student?.ClassRoomId == null) throw new Exception("Student is not assigned to a class");
        var classRoomId = student.ClassRoomId.Value;
        var group = await _uow.QuestionMaps.Query().SelectMany(x => x.Groups).FirstOrDefaultAsync(x => x.QuestionGroupId == groupId && x.Assignments.Any(a => a.ClassRoomId == classRoomId));
        if (group == null) throw new Exception("Question group not available");
        var map = await _uow.QuestionMaps.Query().FirstAsync(x => x.QuestionMapId == group.QuestionMapId);
        var previous = map.Groups.Where(x => x.OrderIndex < group.OrderIndex && x.Assignments.Any(a => a.ClassRoomId == classRoomId)).OrderByDescending(x => x.OrderIndex).FirstOrDefault();
        if (previous != null && !await HasProgressAsync(student.StudentRoleId, previous.QuestionGroupId)) throw new Exception("Complete the previous group first");
        return group.Questions.OrderBy(x => x.OrderIndex).Select(x => new QuestionDto { QuestionId=x.Question.QuestionId, Content=x.Question.Content, QuestionType=x.Question.QuestionType, CourseType=x.Question.CourseType, Skill=x.Question.Skill, Passage=x.Question.Passage, AudioUrl=x.Question.AudioUrl, Explanation=x.Question.Explanation, Options=x.Question.Options.OrderBy(o=>o.OrderIndex).Select(o=>new QuestionOptionDto { QuestionOptionId=o.QuestionOptionId, Content=o.Content, OrderIndex=o.OrderIndex }).ToList() }).ToList();
    }
    public async Task CompleteGroupAsync(int userId, int groupId)
    {
        var student = await _uow.StudentRoleRepository.Query().FirstOrDefaultAsync(x => x.UserId == userId);
        if (student?.ClassRoomId == null) throw new Exception("Student is not assigned to a class");
        if (await HasProgressAsync(student.StudentRoleId, groupId)) return;
        var group = await _uow.QuestionMaps.Query().SelectMany(x => x.Groups)
            .FirstOrDefaultAsync(x => x.QuestionGroupId == groupId && x.Assignments.Any(a => a.ClassRoomId == student.ClassRoomId));
        if (group == null) throw new Exception("Question group not available");
        var map = await _uow.QuestionMaps.Query().FirstAsync(x => x.QuestionMapId == group.QuestionMapId);
        var previous = map.Groups.Where(x => x.OrderIndex < group.OrderIndex && x.Assignments.Any(a => a.ClassRoomId == student.ClassRoomId)).OrderByDescending(x => x.OrderIndex).FirstOrDefault();
        if (previous != null && !await HasProgressAsync(student.StudentRoleId, previous.QuestionGroupId)) throw new Exception("Complete the previous group first");
        await _uow.QuestionMaps.AddProgressAsync(new StudentQuestionGroupProgress { StudentRoleId=student.StudentRoleId, QuestionGroupId=groupId, CompletedAt=DateTime.UtcNow }); await _uow.SaveChangesAsync();
    }
    private async Task<HashSet<int>> GetProgressIdsAsync(int studentRoleId) => (await _uow.StudentQuestionGroupProgresses.Where(x=>x.StudentRoleId==studentRoleId).Select(x=>x.QuestionGroupId).ToListAsync()).ToHashSet();
    private Task<bool> HasProgressAsync(int studentRoleId, int groupId) => _uow.StudentQuestionGroupProgresses.AnyAsync(x=>x.StudentRoleId==studentRoleId && x.QuestionGroupId==groupId);
    private static QuestionMapDto Map(QuestionMap map) => new() { QuestionMapId=map.QuestionMapId, Title=map.Title, Description=map.Description, CourseType=map.CourseType, Groups=map.Groups.OrderBy(x=>x.OrderIndex).Select(g=>new QuestionGroupDto { QuestionGroupId=g.QuestionGroupId, Title=g.Title, Description=g.Description, Icon=g.Icon, Color=g.Color, OrderIndex=g.OrderIndex, QuestionCount=g.QuestionCount, ClassNames=g.Assignments.Select(a=>a.ClassRoom.ClassName).ToList() }).ToList() };
    private static QuestionMapDto MapForStudent(QuestionMap map, int classId, HashSet<int> progress) { var dto=Map(map); dto.Groups=dto.Groups.Where((g,i)=>map.Groups.First(x=>x.QuestionGroupId==g.QuestionGroupId).Assignments.Any(a=>a.ClassRoomId==classId)).ToList(); for(var i=0;i<dto.Groups.Count;i++){dto.Groups[i].IsCompleted=progress.Contains(dto.Groups[i].QuestionGroupId);dto.Groups[i].IsUnlocked=i==0||dto.Groups[i-1].IsCompleted;} return dto; }
    private static bool MatchesCourseType(string courseType, CourseType expected) => Enum.TryParse<CourseType>(courseType, true, out var parsed) && parsed == expected;
}
