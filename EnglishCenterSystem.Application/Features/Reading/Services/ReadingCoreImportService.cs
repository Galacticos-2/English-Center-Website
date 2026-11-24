using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Reading;
using EnglishCenterSystem.Application.Features.Reading.DTOs;
using EnglishCenterSystem.Domain.Enums;

using System.Text.RegularExpressions;

namespace EnglishCenterSystem.Application.Features.Reading.Services;

public class ReadingCoreImportService
{
    private readonly IExerciseDocumentParserService _documentParserService;
    private readonly IReadingAiParserService _readingAiParserService;
    private readonly IQuestionExplanationService _questionExplanationService;

    public ReadingCoreImportService(
        IExerciseDocumentParserService documentParserService,
        IReadingAiParserService readingAiParserService,
        IQuestionExplanationService questionExplanationService)
    {
        _documentParserService = documentParserService;
        _readingAiParserService = readingAiParserService;
        _questionExplanationService = questionExplanationService;
    }

    public async Task<List<ReadingTask>> CreateReadingTasksAsync(
    int teacherUserId,
    string title,
    string? description,
    CourseType courseType,
    Stream questionStream,
    string questionFileName,
    int? expectedQuestionCount = null)
    {
        var extension = Path.GetExtension(questionFileName).ToLower();

        if (extension != ".pdf" &&
            extension != ".docx" &&
            extension != ".txt")
        {
            throw new Exception(
                "Only PDF, Word or TXT reading files are allowed");
        }

        var memoryStream = new MemoryStream();

        await questionStream.CopyToAsync(memoryStream);

        memoryStream.Position = 0;

        var extractedText =
            await _documentParserService.ExtractTextAsync(
                memoryStream,
                questionFileName);

        if (string.IsNullOrWhiteSpace(extractedText) ||
            extractedText.Contains(
                "No readable text found",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Cannot read text from reading file");
        }

        var answerCount =
            ExtractVstepReadingAnswerMap(extractedText).Count;

        var resolvedExpectedQuestionCount =
            expectedQuestionCount ??
            (answerCount > 0 ? answerCount : null);

        if (courseType == CourseType.TOEIC)
        {
            var localToeicReadingTasks =
                TryCreateToeicReadingTasksLocally(
                    teacherUserId,
                    title,
                    description,
                    courseType,
                    extractedText,
                    expectedQuestionCount);

            if (localToeicReadingTasks.Any())
            {
                return localToeicReadingTasks;
            }
        }

        if (courseType == CourseType.VSTEP)
        {
            var localReadingTasks =
                TryCreateVstepReadingTasksLocally(
                    teacherUserId,
                    title,
                    description,
                    courseType,
                    extractedText,
                    resolvedExpectedQuestionCount);

            if (localReadingTasks.Any())
            {
                return localReadingTasks;
            }
        }

        var aiResult = await _readingAiParserService
            .ParseReadingAsync(extractedText);

        var aiReadingTasks = CreateReadingTasksFromAiResult(
            teacherUserId,
            title,
            description,
            courseType,
            aiResult,
            resolvedExpectedQuestionCount);

        if (aiReadingTasks.Any())
        {
            return aiReadingTasks;
        }

        throw new Exception(
            $"The AI reading parser could not parse this {courseType} file. " +
            "Verify that the document contains readable passages, questions, " +
            "options, and an answer key.");
    }

    private static List<ReadingTask> CreateReadingTasksFromAiResult(
        int teacherUserId,
        string title,
        string? description,
        CourseType courseType,
        AiParsedReadingExerciseDto aiResult,
        int? expectedQuestionCount)
    {
        var readingTasks = aiResult.Passages
            .Where(passage =>
                !string.IsNullOrWhiteSpace(passage.PassageText) &&
                passage.Questions.Any())
            .OrderBy(passage => passage.PassageNumber)
            .Select((passage, passageIndex) => new ReadingTask
            {
                TeacherUserId = teacherUserId,
                Title = string.IsNullOrWhiteSpace(passage.Title)
                    ? $"{title} - Passage {passageIndex + 1}"
                    : passage.Title.Trim(),
                Instruction = string.IsNullOrWhiteSpace(passage.Instruction)
                    ? description
                    : passage.Instruction.Trim(),
                PassageText = passage.PassageText.Trim(),
                CourseType = courseType,
                IsActive = true,
                Questions = passage.Questions
                    .OrderBy(question => question.QuestionNumber)
                    .Select((question, questionIndex) =>
                        new ReadingQuestion
                        {
                            Content = question.Content.Trim(),
                            Explanation = string.IsNullOrWhiteSpace(
                                question.Explanation)
                                ? null
                                : question.Explanation.Trim(),
                            OrderIndex = question.QuestionNumber > 0
                                ? question.QuestionNumber
                                : questionIndex + 1,
                            Point = 1,
                            Options = question.Options
                                .Where(option =>
                                    !string.IsNullOrWhiteSpace(
                                        option.Content))
                                .Take(4)
                                .Select((option, optionIndex) =>
                                    new ReadingOption
                                    {
                                        Content = option.Content.Trim(),
                                        IsCorrect = option.IsCorrect,
                                        OrderIndex = optionIndex + 1
                                    })
                                .ToList()
                        })
                    .Where(question =>
                        !string.IsNullOrWhiteSpace(question.Content) &&
                        question.Options.Count >= 2 &&
                        question.Options.Count(option =>
                            option.IsCorrect) == 1)
                    .ToList()
            })
            .Where(task => task.Questions.Any())
            .ToList();

        if (expectedQuestionCount.HasValue &&
            readingTasks.Sum(task => task.Questions.Count) !=
            expectedQuestionCount.Value)
        {
            return new List<ReadingTask>();
        }

        return readingTasks;
    }

    private static List<ReadingTask>
        TryCreateToeicReadingTasksLocally(
            int teacherUserId,
            string title,
            string? description,
            CourseType courseType,
            string rawText,
            int? expectedQuestionCount)
    {
        var normalizedText = NormalizeReadingText(rawText);
        var allAnswerMap = ExtractVstepReadingAnswerMap(normalizedText);

        if (!allAnswerMap.Any())
        {
            return new List<ReadingTask>();
        }

        var questionText = TrimAnswerSection(normalizedText);
        var partPositions = GetToeicPartPositions(questionText);

        if (!partPositions.ContainsKey(5) ||
            !partPositions.ContainsKey(6) ||
            !partPositions.ContainsKey(7))
        {
            return new List<ReadingTask>();
        }

        var readingQuestionNumbers =
            GetToeicReadingQuestionNumbers(
                questionText,
                partPositions);
        var answerMap = allAnswerMap
            .Where(answer =>
                readingQuestionNumbers.Contains(answer.Key))
            .ToDictionary(answer => answer.Key, answer => answer.Value);

        if (!answerMap.Any() ||
            expectedQuestionCount.HasValue &&
            answerMap.Count != expectedQuestionCount.Value)
        {
            return new List<ReadingTask>();
        }

        var parsedQuestions = ParseToeicReadingQuestions(
            questionText,
            answerMap,
            partPositions);

        if (parsedQuestions.Count != answerMap.Count)
        {
            return new List<ReadingTask>();
        }

        var result = new List<ReadingTask>();
        var assignedNumbers = new HashSet<int>();
        var partFiveQuestions = parsedQuestions
            .Where(question => question.PartNumber == 5)
            .OrderBy(question => question.QuestionNumber)
            .ToList();

        if (partFiveQuestions.Any())
        {
            AddToeicReadingTask(
                result,
                teacherUserId,
                "TOEIC Reading Part 5",
                ExtractToeicPartInstruction(questionText, 5),
                "Incomplete Sentences",
                courseType,
                partFiveQuestions,
                assignedNumbers);
        }

        var groupMatches = Regex.Matches(
                questionText,
                @"(?im)(?<title>Questions?\s+(?<start>\d{1,3})(?:\s*[-–—]\s*(?<end>\d{1,3}))?\s+refer\s+to\s+the\s+following\b[^\.\r\n]*(?:\.|(?=\r?$)))")
            .Cast<Match>()
            .OrderBy(match => match.Index)
            .ToList();

        foreach (var groupMatch in groupMatches)
        {
            var groupStart = int.Parse(
                groupMatch.Groups["start"].Value);
            var groupEnd = groupMatch.Groups["end"].Success
                ? int.Parse(groupMatch.Groups["end"].Value)
                : groupStart;
            var partNumber = GetToeicPartNumber(
                groupMatch.Index,
                partPositions);

            if (partNumber is not (6 or 7))
            {
                continue;
            }

            var groupQuestions = parsedQuestions
                .Where(question =>
                    question.PartNumber == partNumber &&
                    question.QuestionNumber >= groupStart &&
                    question.QuestionNumber <= groupEnd)
                .OrderBy(question => question.QuestionNumber)
                .ToList();

            if (groupQuestions.Count != groupEnd - groupStart + 1)
            {
                return new List<ReadingTask>();
            }

            var passageText = ExtractToeicGroupPassage(
                questionText,
                groupMatch,
                groupStart);

            if (string.IsNullOrWhiteSpace(passageText))
            {
                return new List<ReadingTask>();
            }

            AddToeicReadingTask(
                result,
                teacherUserId,
                $"TOEIC Reading Part {partNumber} - " +
                CleanReadingText(groupMatch.Groups["title"].Value),
                ExtractToeicPartInstruction(questionText, partNumber),
                passageText,
                courseType,
                groupQuestions,
                assignedNumbers);
        }

        if (assignedNumbers.Count != parsedQuestions.Count ||
            parsedQuestions.Any(question =>
                !assignedNumbers.Contains(question.QuestionNumber)))
        {
            return new List<ReadingTask>();
        }

        return result;
    }

    private static HashSet<int>
        GetToeicReadingQuestionNumbers(
            string questionText,
            IReadOnlyDictionary<int, int> partPositions)
    {
        return GetToeicQuestionMarkers(questionText)
            .Where(marker =>
                GetToeicPartNumber(
                    marker.Index,
                    partPositions) is 5 or 6 or 7)
            .Select(marker => marker.QuestionNumber)
            .ToHashSet();
    }

    private static Dictionary<int, int> GetToeicPartPositions(
        string text)
    {
        return Regex.Matches(
                text,
                @"(?im)^\s*PART\s*([5-7])\b")
            .Cast<Match>()
            .GroupBy(match => int.Parse(match.Groups[1].Value))
            .ToDictionary(
                group => group.Key,
                group => group.First().Index);
    }

    private static List<ToeicParsedReadingQuestion>
        ParseToeicReadingQuestions(
            string questionText,
            IReadOnlyDictionary<int, string> answerMap,
            IReadOnlyDictionary<int, int> partPositions)
    {
        var questionMatches = GetToeicQuestionMarkers(questionText)
            .Where(marker =>
                answerMap.ContainsKey(marker.QuestionNumber))
            .GroupBy(marker => marker.QuestionNumber)
            .Select(group => group.First())
            .OrderBy(marker => marker.Index)
            .ToList();

        var result = new List<ToeicParsedReadingQuestion>();

        for (var index = 0; index < questionMatches.Count; index++)
        {
            var current = questionMatches[index];
            var endIndex = index + 1 < questionMatches.Count
                ? questionMatches[index + 1].Index
                : questionText.Length;
            var block = questionText.Substring(
                current.Index,
                endIndex - current.Index);
            var partNumber = GetToeicPartNumber(
                current.Index,
                partPositions);
            var question = ParseToeicReadingQuestionBlock(
                current.QuestionNumber,
                partNumber,
                block,
                answerMap[current.QuestionNumber]);

            if (question != null)
            {
                result.Add(question);
            }
        }

        return result;
    }

    private static List<ToeicQuestionMarker>
        GetToeicQuestionMarkers(string text)
    {
        return Regex.Matches(
                text,
                @"(?im)^\s*(?<marker>(?<number>\d{1,3})\s*[\.\)]\s+)|(?<marker>(?<number>\d{1,3})\s*\.\s+(?=\(\s*A\s*\)\s+))")
            .Cast<Match>()
            .Select(match => new ToeicQuestionMarker(
                int.Parse(match.Groups["number"].Value),
                match.Groups["marker"].Index))
            .GroupBy(marker => marker.Index)
            .Select(group => group.First())
            .OrderBy(marker => marker.Index)
            .ToList();
    }

    private static ToeicParsedReadingQuestion?
        ParseToeicReadingQuestionBlock(
            int questionNumber,
            int partNumber,
            string block,
            string correctAnswer)
    {
        var bodyMatch = Regex.Match(
            block,
            $@"(?is)^\s*{questionNumber}\s*[\.\)]\s*(?<body>.*)$");

        if (!bodyMatch.Success)
        {
            return null;
        }

        var body = bodyMatch.Groups["body"].Value;
        var optionMatches = Regex.Matches(
                body,
                @"(?im)(?:^\s*[•●▪]\s*)?(?:\(\s*(?<label>[A-D])\s*\)|^\s*(?<label>[A-D])\s*[\.\)])\s*")
            .Cast<Match>()
            .ToList();

        if (optionMatches.Count < 4)
        {
            return null;
        }

        var firstFourOptions = optionMatches.Take(4).ToList();
        var labels = firstFourOptions
            .Select(option => option.Groups["label"].Value.ToUpperInvariant())
            .ToList();

        if (!labels.SequenceEqual(new[] { "A", "B", "C", "D" }))
        {
            return null;
        }

        var content = CleanReadingText(
            body[..firstFourOptions[0].Index]);

        if (partNumber == 6 && string.IsNullOrWhiteSpace(content))
        {
            content = $"Blank {questionNumber}";
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var trailingBoundary = Regex.Match(
            body,
            @"(?im)^\s*(?:Questions?\s+\d{1,3}(?:\s*[-–—]\s*\d{1,3})?\s+refer\b|PART\s*[5-7]\b)");
        var options = new List<ReadingOption>();

        for (var index = 0; index < firstFourOptions.Count; index++)
        {
            var option = firstFourOptions[index];
            var optionStart = option.Index + option.Length;
            var optionEnd = index + 1 < firstFourOptions.Count
                ? firstFourOptions[index + 1].Index
                : trailingBoundary.Success &&
                  trailingBoundary.Index > optionStart
                    ? trailingBoundary.Index
                    : body.Length;
            var optionText = CleanReadingText(
                body.Substring(optionStart, optionEnd - optionStart));

            if (string.IsNullOrWhiteSpace(optionText))
            {
                return null;
            }

            var label = labels[index];
            options.Add(new ReadingOption
            {
                Content = optionText,
                IsCorrect = label.Equals(
                    correctAnswer,
                    StringComparison.OrdinalIgnoreCase),
                OrderIndex = index + 1
            });
        }

        if (options.Count(option => option.IsCorrect) != 1)
        {
            return null;
        }

        return new ToeicParsedReadingQuestion
        {
            QuestionNumber = questionNumber,
            PartNumber = partNumber,
            Content = content,
            Point = 1,
            Options = options
        };
    }

    private static int GetToeicPartNumber(
        int textIndex,
        IReadOnlyDictionary<int, int> partPositions)
    {
        return partPositions
            .Where(item => item.Value <= textIndex)
            .OrderByDescending(item => item.Value)
            .Select(item => item.Key)
            .FirstOrDefault();
    }

    private static string ExtractToeicPartInstruction(
        string text,
        int partNumber)
    {
        var match = Regex.Match(
            text,
            $@"(?ims)^\s*PART\s*{partNumber}\s*$\s*(?<instruction>.*?)(?=^\s*(?:Questions?\s+\d{{1,3}}|\d{{1,3}}\s*[\.\)]))");

        return match.Success
            ? NormalizeToeicPassageText(match.Groups["instruction"].Value)
            : "";
    }

    private static string ExtractToeicGroupPassage(
        string text,
        Match groupMatch,
        int firstQuestionNumber)
    {
        var remainingText = text[(groupMatch.Index + groupMatch.Length)..];
        var firstQuestionMarker = GetToeicQuestionMarkers(
                remainingText)
            .FirstOrDefault(marker =>
                marker.QuestionNumber == firstQuestionNumber);

        if (firstQuestionMarker == null)
        {
            return "";
        }

        return NormalizeToeicPassageText(
            remainingText[..firstQuestionMarker.Index]);
    }

    private static string NormalizeToeicPassageText(string value)
    {
        var lines = value
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(line => line.Trim())
            .ToList();
        var result = new List<string>();
        var previousWasEmpty = false;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (!previousWasEmpty && result.Count > 0)
                {
                    result.Add("");
                }

                previousWasEmpty = true;
                continue;
            }

            result.Add(line);
            previousWasEmpty = false;
        }

        return string.Join("\n", result)
            .Trim();
    }

    private static void AddToeicReadingTask(
        ICollection<ReadingTask> tasks,
        int teacherUserId,
        string taskTitle,
        string instruction,
        string passageText,
        CourseType courseType,
        IReadOnlyList<ToeicParsedReadingQuestion> questions,
        ISet<int> assignedNumbers)
    {
        tasks.Add(new ReadingTask
        {
            TeacherUserId = teacherUserId,
            Title = taskTitle,
            Instruction = string.IsNullOrWhiteSpace(instruction)
                ? null
                : instruction,
            PassageText = passageText,
            CourseType = courseType,
            IsActive = true,
            Questions = questions
                .Select((question, index) =>
                {
                    assignedNumbers.Add(question.QuestionNumber);
                    question.OrderIndex = index + 1;
                    return (ReadingQuestion)question;
                })
                .ToList()
        });
    }

    private static List<ReadingTask> TryCreateVstepReadingTasksLocally(
        int teacherUserId,
        string title,
        string? description,
        CourseType courseType,
        string rawText,
        int? expectedQuestionCount)
    {
        var normalizedText = NormalizeReadingText(rawText);
        var answerMap = ExtractVstepReadingAnswerMap(
            normalizedText);

        if (!answerMap.Any())
        {
            return new List<ReadingTask>();
        }

        if (expectedQuestionCount.HasValue &&
            answerMap.Count != expectedQuestionCount.Value)
        {
            return new List<ReadingTask>();
        }

        var questionText = TrimAnswerSection(normalizedText);
        var segments = SplitVstepReadingSegments(questionText);
        var sectionInstruction = ExtractVstepReadingSectionInstruction(
            questionText);
        var readingTasks = new List<ReadingTask>();
        var usedQuestionNumbers = new HashSet<int>();
        var passageNumber = 1;

        foreach (var segment in segments)
        {
            var parsedQuestions = ParseVstepReadingQuestions(
                    segment.Text,
                    answerMap)
                .Where(question =>
                    usedQuestionNumbers.Add(question.QuestionNumber))
                .OrderBy(question =>
                    question.QuestionNumber)
                .ToList();

            if (!parsedQuestions.Any())
            {
                continue;
            }

            var segmentContent = ExtractVstepSegmentContent(
                segment.Text,
                parsedQuestions.First().QuestionNumber);
            var passageText = string.Join(
                "\n",
                new[]
                {
                    segmentContent.Title,
                    segmentContent.PassageText
                }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            if (string.IsNullOrWhiteSpace(passageText))
            {
                passageText = segment.Title;
            }

            var instruction = string.IsNullOrWhiteSpace(
                    segmentContent.Instruction)
                ? sectionInstruction ?? description
                : segmentContent.Instruction;

            readingTasks.Add(new ReadingTask
            {
                TeacherUserId = teacherUserId,
                Title = string.IsNullOrWhiteSpace(segmentContent.Title)
                    ? $"{title} - Passage {passageNumber}"
                    : segmentContent.Title,
                Instruction = string.Join(
                    "\n\n",
                    new[]
                    {
                        segment.Title,
                        instruction
                    }
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                PassageText = passageText,
                CourseType = courseType,
                IsActive = true,
                Questions = parsedQuestions
                    .Select((question, index) =>
                    {
                        question.OrderIndex = question.QuestionNumber;
                        return (ReadingQuestion)question;
                    })
                    .ToList()
            });

            passageNumber++;
        }

        var totalQuestions = readingTasks
            .Sum(task => task.Questions.Count);

        var expectedCount =
            expectedQuestionCount ?? answerMap.Count;

        if (totalQuestions != expectedCount)
        {
            return new List<ReadingTask>();
        }

        var invalidQuestion = readingTasks
            .SelectMany(task => task.Questions)
            .FirstOrDefault(question =>
                question.Options.Count < 2 ||
                question.Options.Count(option => option.IsCorrect) != 1);

        return invalidQuestion == null
            ? readingTasks
            : new List<ReadingTask>();
    }

    private static List<VstepReadingSegment> SplitVstepReadingSegments(
        string questionText)
    {
        var headingMatches = Regex.Matches(
                questionText,
                @"(?im)^\s*((?:Reading\s+)?(?:Passage|Text|Part)\s+\d+[^\r\n]*)\s*$")
            .Cast<Match>()
            .OrderBy(match => match.Index)
            .ToList();

        if (!headingMatches.Any())
        {
            return new List<VstepReadingSegment>
            {
                new(
                    "Reading Passage 1",
                    questionText)
            };
        }

        var segments = new List<VstepReadingSegment>();

        for (var i = 0; i < headingMatches.Count; i++)
        {
            var current = headingMatches[i];
            var endIndex = i + 1 < headingMatches.Count
                ? headingMatches[i + 1].Index
                : questionText.Length;

            var text = questionText
                .Substring(
                    current.Index,
                    endIndex - current.Index)
                .Trim();

            segments.Add(new VstepReadingSegment(
                CleanReadingText(current.Groups[1].Value),
                text));
        }

        return segments;
    }

    private static string? ExtractVstepReadingSectionInstruction(
        string questionText)
    {
        var match = Regex.Match(
            questionText,
            @"(?ims)^\s*Directions?\s*:\s*(?<content>.*?)(?=^\s*(?:Reading\s+)?(?:Passage|Text|Part)\s+\d+\b|\z)");

        return match.Success
            ? CleanReadingText(match.Groups["content"].Value)
            : null;
    }

    private static List<VstepParsedReadingQuestion>
        ParseVstepReadingQuestions(
            string segmentText,
            IReadOnlyDictionary<int, string> answerMap)
    {
        var questionMatches = Regex.Matches(
                segmentText,
                @"(?im)^\s*(?:Question\s*)?(\d{1,3})\s*[\.\):]\s+")
            .Cast<Match>()
            .Select(match => new
            {
                Number = int.Parse(match.Groups[1].Value),
                Match = match
            })
            .Where(item =>
                answerMap.ContainsKey(item.Number))
            .GroupBy(item => item.Number)
            .Select(group => group.First())
            .OrderBy(item => item.Match.Index)
            .ToList();

        var questions =
            new List<VstepParsedReadingQuestion>();

        for (var i = 0; i < questionMatches.Count; i++)
        {
            var current = questionMatches[i];
            var endIndex = i + 1 < questionMatches.Count
                ? questionMatches[i + 1].Match.Index
                : segmentText.Length;

            var block = segmentText
                .Substring(
                    current.Match.Index,
                    endIndex - current.Match.Index)
                .Trim();

            var question = ParseVstepReadingQuestionBlock(
                current.Number,
                block,
                answerMap[current.Number]);

            if (question != null)
            {
                questions.Add(question);
            }
        }

        return questions;
    }

    private static VstepParsedReadingQuestion?
        ParseVstepReadingQuestionBlock(
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

        var body = headerMatch.Groups["body"].Value.Trim();
        var optionMarkers = Regex.Matches(
                body,
                @"(?im)(?<![A-Z.])([A-D])\s*[\.\)]\s+(?=\S)")
            .Cast<Match>()
            .ToList();

        if (optionMarkers.Count < 2)
        {
            return null;
        }

        var firstOption = optionMarkers
            .FirstOrDefault(marker =>
                marker.Groups[1].Value.Equals(
                    "A",
                    StringComparison.OrdinalIgnoreCase));

        if (firstOption == null)
        {
            return null;
        }

        var content = CleanReadingText(
            body[..firstOption.Index]);

        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var options = new List<ReadingOption>();

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
            var optionText = CleanReadingText(
                body.Substring(
                    optionStart,
                    nextIndex - optionStart));

            if (!string.IsNullOrWhiteSpace(optionText))
            {
                options.Add(new ReadingOption
                {
                    Content = optionText,
                    IsCorrect = label.Equals(
                        correctAnswer,
                        StringComparison.OrdinalIgnoreCase),
                    OrderIndex = options.Count + 1
                });
            }
        }

        options = options
            .Take(4)
            .ToList();

        if (options.Count < 2 ||
            options.Count(option => option.IsCorrect) != 1)
        {
            return null;
        }

        return new VstepParsedReadingQuestion
        {
            QuestionNumber = questionNumber,
            Content = content,
            Point = 1,
            Options = options
        };
    }

    private static VstepSegmentContent ExtractVstepSegmentContent(
        string segmentText,
        int firstQuestionNumber)
    {
        var match = Regex.Match(
            segmentText,
            $@"(?im)^\s*(?:Question\s*)?{firstQuestionNumber}\s*[\.\):]\s+");

        var contentBeforeQuestions = match.Success
            ? segmentText[..match.Index]
            : segmentText;

        var lines = contentBeforeQuestions
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count > 0 && Regex.IsMatch(
                lines[0],
                @"(?i)^(?:Reading\s+)?(?:Passage|Text|Part)\s+\d+"))
        {
            lines.RemoveAt(0);
        }

        string? instruction = null;
        if (lines.Count > 0)
        {
            var instructionMatch = Regex.Match(
                lines[0],
                @"(?i)^Instruction\s*:\s*(?<content>.+)$");

            if (instructionMatch.Success)
            {
                instruction = CleanReadingText(
                    instructionMatch.Groups["content"].Value);
                lines.RemoveAt(0);
            }
        }

        var passageTitle = lines.Count > 0
            ? CleanReadingText(lines[0])
            : null;

        if (lines.Count > 0)
        {
            lines.RemoveAt(0);
        }

        return new VstepSegmentContent(
            passageTitle,
            instruction,
            CleanReadingText(string.Join("\n", lines)));
    }

    private static Dictionary<int, string> ExtractVstepReadingAnswerMap(
        string rawText)
    {
        var answerText = ExtractReadingAnswerSection(rawText);

        if (string.IsNullOrWhiteSpace(answerText))
        {
            return new Dictionary<int, string>();
        }

        return Regex.Matches(
                answerText,
                @"(?im)\b(\d{1,3})\s*[\.\):\-]?\s*([A-D])\b")
            .Cast<Match>()
            .Select(match => new
            {
                Number = int.Parse(match.Groups[1].Value),
                Answer = match.Groups[2].Value
                    .Trim()
                    .ToUpperInvariant()
            })
            .GroupBy(item => item.Number)
            .ToDictionary(
                group => group.Key,
                group => group.First().Answer);
    }

    private static string? ExtractReadingAnswerSection(
        string rawText)
    {
        var answerHeading = FindReadingAnswerHeading(rawText);

        if (!answerHeading.Success)
        {
            return null;
        }

        var contentStart = answerHeading.Index + answerHeading.Length;
        var nextHeading = Regex.Match(
            rawText[contentStart..],
            $@"(?im)^\s*{AnySkillAnswerHeadingPattern}\s*:?\s*$");
        var contentEnd = nextHeading.Success
            ? contentStart + nextHeading.Index
            : rawText.Length;

        return rawText
            .Substring(contentStart, contentEnd - contentStart)
            .Trim();
    }

    private static string TrimAnswerSection(string rawText)
    {
        var answerHeading = FindReadingAnswerHeading(rawText);

        return answerHeading.Success
            ? rawText[..answerHeading.Index].Trim()
            : rawText;
    }

    private static Match FindReadingAnswerHeading(string rawText)
    {
        var answerHeading = Regex.Match(
            rawText,
            $@"(?im)^\s*{ReadingAnswerHeadingPattern}\s*:?\s*$");

        return answerHeading.Success
            ? answerHeading
            : Regex.Match(
                rawText,
                $@"(?im)^\s*{ReadingAnswerHeadingPattern}\s*:\s*(?=\S)");
    }

    private const string ReadingAnswerHeadingPattern =
        @"(?:Answer\s+for\s+(?:(?:TOEIC|VSTEP)\s+)?Reading|(?:(?:TOEIC|VSTEP)\s+)?Reading\s+Answers?|Answer\s*Key|Answers?|Đáp\s*án\s*gợi\s*ý|Đáp\s*án)";

    private const string AnySkillAnswerHeadingPattern =
        @"(?:Answer\s+for\s+(?:(?:TOEIC|VSTEP)\s+)?(?:Listening|Reading|Writing|Speaking)|(?:(?:TOEIC|VSTEP)\s+)?(?:Listening|Reading|Writing|Speaking)\s+Answers?|Answer\s*Key|Answers?|Đáp\s*án\s*gợi\s*ý|Đáp\s*án)";

    private static string NormalizeReadingText(string value)
    {
        return value
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\f", "\n")
            .Replace("\u00A0", " ");
    }

    private static string CleanReadingText(string value)
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

    private sealed record VstepReadingSegment(
        string Title,
        string Text);

    private sealed record ToeicQuestionMarker(
        int QuestionNumber,
        int Index);

    private sealed record VstepSegmentContent(
        string? Title,
        string? Instruction,
        string PassageText);

    private sealed class ToeicParsedReadingQuestion
        : ReadingQuestion
    {
        public int QuestionNumber { get; set; }

        public int PartNumber { get; set; }
    }

    private sealed class VstepParsedReadingQuestion
        : ReadingQuestion
    {
        public int QuestionNumber { get; set; }
    }
}
