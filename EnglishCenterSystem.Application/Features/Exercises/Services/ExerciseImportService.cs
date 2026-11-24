using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Imports.Services;
using EnglishCenterSystem.Application.Features.Notifications.DTOs;
using EnglishCenterSystem.Application.Features.Notifications.Services;
using EnglishCenterSystem.Application.Common.Helpers;
using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace EnglishCenterSystem.Application.Features.Exercises.Services;

public class ExerciseImportService
{
    private readonly IUnitOfWork _unitOfWork;
  
    private readonly IExerciseDocumentParserService
    _documentParserService;

    private readonly IExerciseAiParserService
        _aiParserService;
    private readonly IQuestionExplanationService
        _questionExplanationService;
    private readonly ImportPreflightService
        _importPreflightService;
    private readonly IMinioService _minioService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<ExerciseImportService> _logger;
    public ExerciseImportService(
    IUnitOfWork unitOfWork,
   
    IExerciseDocumentParserService documentParserService,
    IExerciseAiParserService aiParserService,
    IQuestionExplanationService questionExplanationService,
    ImportPreflightService importPreflightService,
    IMinioService minioService,
    NotificationService notificationService,
    ILogger<ExerciseImportService> logger)
    {
        _unitOfWork = unitOfWork;
        
        _documentParserService = documentParserService;
        _aiParserService = aiParserService;
        _questionExplanationService = questionExplanationService;
        _importPreflightService = importPreflightService;
        _minioService = minioService;
        _notificationService = notificationService;
        _logger = logger;
    }
   
   
    
    private List<Question> ParseQuestionsFromJson(
    string parsedJson,
    int teacherUserId,
    int questionCategoryId,
    CourseType courseType,
    SkillType skill)
    {
        var result =
            System.Text.Json.JsonSerializer
                .Deserialize<AiParsedExerciseDto>(
                    parsedJson,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

        if (result == null)
        {
            return new List<Question>();
        }

        var questions = new List<Question>();

        foreach (var questionDto in result.Questions)
        {
            if (string.IsNullOrWhiteSpace(questionDto.Content))
            {
                continue;
            }

            var questionType = MapAiQuestionType(
                questionDto.QuestionType,
                questionDto.Options
            );

            var question = new Question
            {
                TeacherUserId = teacherUserId,
                QuestionCategoryId = questionCategoryId,
                Content = questionDto.Content.Trim(),
                CorrectTextAnswer =
                    string.IsNullOrWhiteSpace(questionDto.CorrectAnswer)
                        ? null
                        : questionDto.CorrectAnswer.Trim(),
                QuestionType = questionType,
                Explanation =
                    string.IsNullOrWhiteSpace(questionDto.Explanation)
                        ? null
                        : questionDto.Explanation.Trim(),
                CourseType = courseType,
                Skill = skill,
            
                IsActive = true
            };

            if (
                questionType == QuestionType.SingleChoice ||
                questionType == QuestionType.TrueFalse
            )
            {
                if (questionDto.Options.Count < 2)
                {
                    continue;
                }

                question.Options = questionDto.Options
                    .Select((option, index) => new QuestionOption
                    {
                        Content = option.Content.Trim(),
                        OrderIndex = index + 1,
                        IsCorrect = option.IsCorrect
                    })
                    .ToList();
            }

            if (questionType == QuestionType.TextAnswer)
            {
                
            }

            questions.Add(question);
        }

        return questions;
    }
    private QuestionType MapAiQuestionType(
    string? aiQuestionType,
    List<AiParsedOptionDto> options)
    {
        if (!string.IsNullOrWhiteSpace(aiQuestionType))
        {
            var normalized =
                aiQuestionType.Trim().ToLower();

            if (normalized == "truefalse" ||
                normalized == "true/false")
            {
                return QuestionType.TrueFalse;
            }

            if (normalized == "textanswer" ||
                normalized == "fillblank" ||
                normalized == "fillintheblank" ||
                normalized == "shortanswer")
            {
                return QuestionType.TextAnswer;
            }

            if (normalized == "audioanswer")
            {
                return QuestionType.AudioAnswer;
            }
        }

        if (options.Count == 2 &&
            options.Any(x =>
                x.Content.Equals(
                    "True",
                    StringComparison.OrdinalIgnoreCase)) &&
            options.Any(x =>
                x.Content.Equals(
                    "False",
                    StringComparison.OrdinalIgnoreCase)))
        {
            return QuestionType.TrueFalse;
        }

        if (options.Count >= 2)
        {
            return QuestionType.SingleChoice;
        }

        return QuestionType.TextAnswer;
    }
    private List<Question> ParseMultipleChoiceQuestions(
    string rawText,
    int teacherUserId,
    int questionCategoryId,
    CourseType courseType,
    SkillType skill)
    {
        var questions = new List<Question>();

        var pattern =
            @"(?ms)(\d+)\.\s*(.*?)(?=\nA\.)\s*" +
            @"A\.\s*(.*?)\s*" +
            @"B\.\s*(.*?)\s*" +
            @"C\.\s*(.*?)\s*" +
            @"D\.\s*(.*?)\s*" +
            @"Answer:\s*([A-D])";

        var matches = System.Text.RegularExpressions.Regex.Matches(
            rawText,
            pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var content = match.Groups[2].Value.Trim();

            var correctLetter = match.Groups[7].Value.Trim();

            var options = new List<(string Letter, string Content)>
        {
            ("A", match.Groups[3].Value.Trim()),
            ("B", match.Groups[4].Value.Trim()),
            ("C", match.Groups[5].Value.Trim()),
            ("D", match.Groups[6].Value.Trim())
        };

            var question = new Question
            {
                TeacherUserId = teacherUserId,
                QuestionCategoryId = questionCategoryId,
                Content = content,
                QuestionType = QuestionType.SingleChoice,
                CourseType = courseType,
                Skill = skill,
               
             
                IsActive = true,
                Options = options.Select((option, index) =>
                    new QuestionOption
                    {
                        Content = option.Content,
                        OrderIndex = index + 1,
                        IsCorrect = option.Letter == correctLetter
                    })
                    .ToList()
            };

            questions.Add(question);
        }

        return questions;
    }
    
    public async Task<int> ImportAndAssignAsync(
    int teacherUserId,
    Stream fileStream,
    string fileName,
    
    ImportAssignExerciseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new Exception("Exercise set title is required");
        }

        

        if (dto.MaxAttempts <= 0)
        {
            throw new Exception("Max attempts must be greater than 0");
        }

        var section = await _unitOfWork.ExerciseSections
            .FirstOrDefaultAsync(x =>
                x.ExerciseSectionId == dto.ExerciseSectionId &&
                x.TeacherUserId == teacherUserId &&
                x.ClassRoomId == dto.ClassRoomId &&
                x.IsActive);

        if (section == null)
        {
            throw new Exception("Exercise section not found");
        }

        var extension = Path.GetExtension(fileName).ToLower();

        if (extension != ".pdf" &&
            extension != ".docx" &&
            extension != ".txt")
        {
            throw new Exception("Only PDF, Word or TXT files are allowed");
        }

        var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);

        var sourceFileBytes = memoryStream.ToArray();

        memoryStream.Position = 0;

        var extractedText =
            await _documentParserService.ExtractTextAsync(
                memoryStream,
                fileName);

        _importPreflightService.ValidateExerciseImport(
            extractedText,
            dto);

        var localParse =
            ParseAiQuestionsLocallyPartial(extractedText);

        var localParsingSucceeded =
            localParse.AnswerMap.Any() &&
            !localParse.FailedQuestionNumbers.Any() &&
            localParse.Parsed.Questions.Any();

        AiParsedExerciseDto aiResult;

        if (localParsingSucceeded)
        {
            aiResult = localParse.Parsed;
        }
        else if (IsGrammarAiFallbackEligible(dto))
        {
            try
            {
                aiResult = await _aiParserService
                    .ParseQuestionsAsync(extractedText);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Grammar exercise could not be parsed by the " +
                    "local parser or AI fallback.",
                    ex);
            }
        }
        else if (!localParse.AnswerMap.Any())
        {
            throw new Exception(
                "Exercise local parser could not find a numbered answer key. " +
                "Required format: numbered questions, A-D options, and answers " +
                "such as '1. B'.");
        }
        else
        {
            throw new Exception(
                "Exercise local parser could not parse questions: " +
                string.Join(", ", localParse.FailedQuestionNumbers) +
                ". Check each question has A-D options.");
        }

        if (dto.GenerateExplanations)
        {
            await _questionExplanationService
                .AddExerciseExplanationsAsync(aiResult.Questions);
        }

        var category = await _unitOfWork.Questions
            .QueryCategories()
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync();

        if (category == null)
        {
            throw new Exception("Question category is required");
        }

        var parsedQuestions =
            ParseQuestionsFromJson(
                System.Text.Json.JsonSerializer.Serialize(aiResult),
                teacherUserId,
                category.QuestionCategoryId,
                dto.CourseType,
                dto.Skill);

        if (!parsedQuestions.Any())
        {
            throw new Exception(
                "Cannot parse questions from document. Please check file format.");
        }

        var exercise = new Exercise
        {
            TeacherUserId = teacherUserId,
            ExerciseSectionId = dto.ExerciseSectionId,
            Title = dto.Title,
            Description = string.IsNullOrWhiteSpace(dto.Description)
    ? null
    : dto.Description.Trim(),
            CourseType = dto.CourseType,
            Skill = dto.Skill,
            
            IsActive = true,
            ExerciseQuestions = parsedQuestions
                .Select((question, index) => new ExerciseQuestion
                {
                    Question = question,
                    OrderIndex = index + 1,
                    Point = 1
                })
                .ToList()
        };

        var sourceObjectName = $"exercises/source/{Guid.NewGuid():N}_" +
            StoragePathHelper.SanitizeFileName(fileName);

        await using var sourceStream = new MemoryStream(sourceFileBytes);
        exercise.SourceFileUrl = await _minioService.UploadFileAsync(
            sourceStream,
            sourceObjectName,
            StoragePathHelper.GetDocumentContentType(fileName));
        exercise.SourceFileObjectName = sourceObjectName;

        await _unitOfWork.Exercises.AddAsync(exercise);
        await _unitOfWork.SaveChangesAsync();

        var assignment = new ExerciseAssignment
        {
            ExerciseId = exercise.ExerciseId,
            ClassRoomId = dto.ClassRoomId,
        
            MaxAttempts = dto.MaxAttempts,
            DeadlineAt = dto.DeadlineAt,
            IsActive = true
        };

        await _unitOfWork.ExerciseAssignments.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        await CreateExerciseNotificationSafelyAsync(
            teacherUserId,
            exercise.Title,
            dto.ClassRoomId,
            assignment.ExerciseAssignmentId);

        return exercise.ExerciseId;
    }

    private async Task CreateExerciseNotificationSafelyAsync(
        int teacherUserId,
        string exerciseTitle,
        int classRoomId,
        int exerciseAssignmentId)
    {
        try
        {
            await _notificationService.CreateForClassAsync(
                new CreateClassNotificationDto
                {
                    CreatedByUserId = teacherUserId,
                    ClassRoomId = classRoomId,
                    Type = NotificationType.ExerciseAssigned,
                    RelatedEntityId = exerciseAssignmentId,
                    Title = "New exercise assigned",
                    Message = $"Exercise \"{exerciseTitle}\" has been assigned to the class.",
                    TeacherActionUrl = "/teacher/exercises",
                    StudentActionUrl = "/student/exercises"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not create exercise notification for ExerciseAssignmentId {ExerciseAssignmentId}",
                exerciseAssignmentId);
        }
    }

    private static LocalExerciseParseResult
        ParseAiQuestionsLocallyPartial(string rawText)
    {
        var result = new LocalExerciseParseResult();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return result;
        }

        var normalizedText = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\f", "\n");

        var answerHeading = Regex.Match(
            normalizedText,
            @"(?im)^\s*(Answer\s*Key|Answers|Ans|Key|Đáp\s*án|Đáp\s*án\s*gợi\s*ý)\s*:?\s*");
        var questionText = answerHeading.Success
            ? normalizedText[..answerHeading.Index]
            : normalizedText;

        var questionMatches = Regex.Matches(
                questionText,
                @"(?im)^\s*(?:Question\s*)?(\d+)\s*[\.\):]\s+")
            .Cast<Match>()
            .Select(x => new
            {
                Number = int.Parse(x.Groups[1].Value),
                Index = x.Index
            })
            .GroupBy(x => x.Number)
            .Select(x => x.First())
            .OrderBy(x => x.Index)
            .ToList();

        for (var i = 0; i < questionMatches.Count; i++)
        {
            var current = questionMatches[i];
            var endIndex = i + 1 < questionMatches.Count
                ? questionMatches[i + 1].Index
                : questionText.Length;
            var block = questionText
                .Substring(current.Index, endIndex - current.Index)
                .Trim();

            result.QuestionBlocks[current.Number] = block;

            if (!result.AnswerMap.ContainsKey(current.Number) &&
                TryGetInlineAnswer(block, out var inlineAnswer))
            {
                result.AnswerMap[current.Number] = inlineAnswer;
            }
        }

        foreach (var answer in ExtractAnswerMap(normalizedText))
        {
            result.AnswerMap[answer.Key] = answer.Value;
        }

        if (!result.AnswerMap.Any())
        {
            return result;
        }

        foreach (var questionMatch in questionMatches
            .Where(x => result.AnswerMap.ContainsKey(x.Number)))
        {
            var parsed = ParseExerciseQuestionBlock(
                questionMatch.Number,
                result.QuestionBlocks[questionMatch.Number],
                result.AnswerMap[questionMatch.Number]);

            if (parsed != null)
            {
                result.Parsed.Questions.Add(parsed);
            }
        }

        result.FailedQuestionNumbers.AddRange(
            result.AnswerMap.Keys
                .Where(number => result.Parsed.Questions.All(
                    question => question.QuestionNumber != number))
                .OrderBy(number => number));

        return result;
    }

    private static string BuildExerciseFallbackText(
        LocalExerciseParseResult localParse)
    {
        var blocks = localParse.FailedQuestionNumbers
            .Where(localParse.QuestionBlocks.ContainsKey)
            .Select(number => localParse.QuestionBlocks[number])
            .ToList();

        if (!blocks.Any())
        {
            throw new Exception(
                "Exercise source is missing question blocks: " +
                string.Join(", ", localParse.FailedQuestionNumbers));
        }

        var answers = localParse.FailedQuestionNumbers
            .Select(number =>
                $"Correct answer for question {number}: " +
                localParse.AnswerMap[number]);

        return string.Join("\n\n", blocks) +
               "\n\nAuthoritative Answer Key:\n" +
               string.Join("\n", answers);
    }

    private static void MergeExerciseFallbackQuestions(
        AiParsedExerciseDto target,
        AiParsedExerciseDto fallback,
        LocalExerciseParseResult localParse)
    {
        var failedNumbers = localParse.FailedQuestionNumbers
            .OrderBy(number => number)
            .ToList();
        var candidates = fallback.Questions
            .Where(question => !string.IsNullOrWhiteSpace(question.Content))
            .ToList();

        for (var index = 0; index < candidates.Count; index++)
        {
            var question = candidates[index];

            if (!failedNumbers.Contains(question.QuestionNumber) &&
                index < failedNumbers.Count)
            {
                question.QuestionNumber = failedNumbers[index];
            }

            if (!localParse.AnswerMap.TryGetValue(
                    question.QuestionNumber,
                    out var correctAnswer))
            {
                continue;
            }

            ApplyExerciseCorrectAnswer(question, correctAnswer);

            if (IsValidParsedExerciseQuestion(question))
            {
                target.Questions.Add(question);
            }
        }
    }

    private static void ApplyExerciseCorrectAnswer(
        AiParsedQuestionDto question,
        string correctAnswer)
    {
        if (question.Options.Any())
        {
            var labels = new[] { "A", "B", "C", "D" };

            for (var index = 0; index < question.Options.Count; index++)
            {
                var option = question.Options[index];
                var label = string.IsNullOrWhiteSpace(option.Label) &&
                            index < labels.Length
                    ? labels[index]
                    : option.Label.Trim().ToUpperInvariant();
                option.Label = label;
                option.IsCorrect = label.Equals(
                    correctAnswer,
                    StringComparison.OrdinalIgnoreCase);
            }

            return;
        }

        question.CorrectAnswer = correctAnswer;
    }

    private static bool IsValidParsedExerciseQuestion(
        AiParsedQuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(question.Content))
        {
            return false;
        }

        if (!question.Options.Any())
        {
            return !string.IsNullOrWhiteSpace(question.CorrectAnswer);
        }

        return question.Options.Count >= 2 &&
               question.Options.Count(option => option.IsCorrect) == 1;
    }

    private static Dictionary<int, string> ExtractAnswerMap(
        string rawText)
    {
        var result = new Dictionary<int, string>();

        var answerMatch = Regex.Match(
            rawText,
            @"(?ims)^\s*(Answer\s*Key|Answers?|Ans|Key|Đáp\s*án|Đáp\s*án\s*gợi\s*ý)\s*:?\s*(?<content>.*)$");

        var answerText = answerMatch.Success
            ? answerMatch.Groups["content"].Value
            : rawText;

        foreach (Match match in Regex.Matches(
                     answerText,
                     @"(?im)\b(\d+)\s*[\.\)]\s*([A-D]|True|False|T|F)\b"))
        {
            result[int.Parse(match.Groups[1].Value)] =
                match.Groups[2].Value.Trim();
        }

        return result;
    }

    private static bool IsGrammarAiFallbackEligible(
        ImportAssignExerciseDto dto)
    {
        return dto.Skill == SkillType.Grammar &&
               (dto.CourseType == CourseType.TOEIC ||
                dto.CourseType == CourseType.VSTEP);
    }

    private static bool TryGetInlineAnswer(
        string questionBlock,
        out string answer)
    {
        answer = string.Empty;

        var answerMatch = Regex.Match(
            questionBlock,
            @"(?im)^\s*Answer\s*:\s*(?<answer>.+?)\s*$");

        if (!answerMatch.Success)
        {
            return false;
        }

        var rawAnswer = answerMatch.Groups["answer"].Value.Trim();
        var optionAnswer = Regex.Match(
            rawAnswer,
            @"^(?<option>[A-D])\s*[\.\)]\s*");

        answer = optionAnswer.Success
            ? optionAnswer.Groups["option"].Value
            : rawAnswer;

        return !string.IsNullOrWhiteSpace(answer);
    }

    private static AiParsedQuestionDto? ParseExerciseQuestionBlock(
        int questionNumber,
        string block,
        string correctAnswer)
    {
        var headerMatch = Regex.Match(
            block,
            $@"(?is)^\s*(?:Question\s*)?{questionNumber}\s*[\.\):]\s*(?<body>.+)$");

        if (!headerMatch.Success)
        {
            return null;
        }

        var body = Regex.Replace(
                headerMatch.Groups["body"].Value,
                @"(?im)^\s*Answer\s*:\s*.*$",
                string.Empty)
            .Trim();

        var optionMarkers = Regex.Matches(
                body,
                @"(?im)(?:^|\s)([A-D])\s*[\.\)]\s+")
            .Cast<Match>()
            .ToList();

        if (optionMarkers.Count >= 2)
        {
            var firstOption = optionMarkers
                .FirstOrDefault(x =>
                    x.Groups[1].Value.Equals(
                        "A",
                        StringComparison.OrdinalIgnoreCase));

            if (firstOption == null)
            {
                return null;
            }

            var content = CleanQuestionText(
                body[..firstOption.Index]);

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var options = new List<AiParsedOptionDto>();

            for (var i = 0; i < optionMarkers.Count; i++)
            {
                var current = optionMarkers[i];
                var nextIndex = i + 1 < optionMarkers.Count
                    ? optionMarkers[i + 1].Index
                    : body.Length;

                var label = current.Groups[1].Value
                    .Trim()
                    .ToUpperInvariant();

                var optionStart = current.Index + current.Length;
                var optionText = CleanQuestionText(
                    body.Substring(
                        optionStart,
                        nextIndex - optionStart));

                if (!string.IsNullOrWhiteSpace(optionText))
                {
                    options.Add(new AiParsedOptionDto
                    {
                        Label = label,
                        Content = optionText,
                        IsCorrect = label.Equals(
                            correctAnswer,
                            StringComparison.OrdinalIgnoreCase)
                    });
                }
            }

            options = options
                .Where(x => new[] { "A", "B", "C", "D" }
                    .Contains(x.Label))
                .GroupBy(x => x.Label)
                .Select(x => x.First())
                .OrderBy(x => x.Label)
                .Take(4)
                .ToList();

            return options.Count >= 2 &&
                   options.Count(x => x.IsCorrect) == 1
                ? new AiParsedQuestionDto
                {
                    QuestionNumber = questionNumber,
                    Content = content,
                    QuestionType = "SingleChoice",
                    Options = options
                }
                : null;
        }

        if (correctAnswer.Equals("True", StringComparison.OrdinalIgnoreCase) ||
            correctAnswer.Equals("False", StringComparison.OrdinalIgnoreCase) ||
            correctAnswer.Equals("T", StringComparison.OrdinalIgnoreCase) ||
            correctAnswer.Equals("F", StringComparison.OrdinalIgnoreCase))
        {
            var isTrue = correctAnswer.Equals(
                             "True",
                             StringComparison.OrdinalIgnoreCase) ||
                         correctAnswer.Equals(
                             "T",
                             StringComparison.OrdinalIgnoreCase);

            return new AiParsedQuestionDto
            {
                QuestionNumber = questionNumber,
                Content = CleanQuestionText(body),
                QuestionType = "TrueFalse",
                Options = new List<AiParsedOptionDto>
                {
                    new()
                    {
                        Label = "A",
                        Content = "True",
                        IsCorrect = isTrue
                    },
                    new()
                    {
                        Label = "B",
                        Content = "False",
                        IsCorrect = !isTrue
                    }
                }
            };
        }

        return new AiParsedQuestionDto
        {
            QuestionNumber = questionNumber,
            Content = CleanQuestionText(body),
            QuestionType = "TextAnswer",
            CorrectAnswer = correctAnswer,
            Options = new List<AiParsedOptionDto>()
        };
    }

    private static string CleanQuestionText(string value)
    {
        return Regex.Replace(
                value
                    .Replace("\u00A0", " ")
                    .Replace("\u200B", "")
                    .Replace("\uFEFF", ""),
                @"\s+",
                " ")
            .Trim();
    }

    private sealed class LocalExerciseParseResult
    {
        public AiParsedExerciseDto Parsed { get; } = new();

        public Dictionary<int, string> AnswerMap { get; } = new();

        public Dictionary<int, string> QuestionBlocks { get; } = new();

        public List<int> FailedQuestionNumbers { get; } = new();
    }
   
  
}
