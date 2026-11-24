using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Application.Features.Writing.DTOs;
using EnglishCenterSystem.Domain.Entities.Writing;
using EnglishCenterSystem.Domain.Enums;

using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EnglishCenterSystem.Application.Features.Writing.Services;

public class WritingCoreImportService
{
    private readonly IExerciseDocumentParserService _documentParserService;
    private readonly IWritingAiParserService _writingAiParserService;
    private readonly IMinioService _minioService;
    private readonly IToeicPdfPromptImageExtractorService
        _toeicPdfPromptImageExtractorService;

    public WritingCoreImportService(
        IExerciseDocumentParserService documentParserService,
        IWritingAiParserService writingAiParserService,
        IMinioService minioService,
        IToeicPdfPromptImageExtractorService
            toeicPdfPromptImageExtractorService)
    {
        _documentParserService = documentParserService;
        _writingAiParserService = writingAiParserService;
        _minioService = minioService;
        _toeicPdfPromptImageExtractorService =
            toeicPdfPromptImageExtractorService;
    }

    public async Task<List<WritingTask>> CreateWritingTasksAsync(
        int teacherUserId,
        CourseType courseType,
        Stream fileStream,
        string fileName,
        byte[]? promptImageDocumentBytes = null,
        string? promptImageDocumentFileName = null,
        int? expectedTaskCount = null)
    {
        var extension = Path.GetExtension(fileName).ToLower();

        if (extension != ".pdf" &&
            extension != ".docx" &&
            extension != ".txt")
        {
            throw new Exception("Only PDF, Word or TXT files are allowed");
        }

        var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);

        memoryStream.Position = 0;

        var rawText = await _documentParserService.ExtractTextAsync(
            memoryStream,
            fileName);

        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new Exception(
                "Cannot read writing document content");
        }

        if (courseType == CourseType.VSTEP)
        {
            rawText = NormalizeVstepWritingTaskMarkers(rawText);
        }

        IReadOnlyDictionary<int, string> promptImageUrls =
            new Dictionary<int, string>();

        if (courseType == CourseType.TOEIC)
        {
            promptImageUrls =
                await ExtractToeicWritingPromptImageUrlsAsync(
                    promptImageDocumentBytes ??
                        memoryStream.ToArray(),
                    Path.GetExtension(
                        promptImageDocumentFileName ??
                            fileName)
                        .ToLowerInvariant(),
                    teacherUserId);

            var toeicWritingTasks =
                TryCreateToeicWritingTasksFromMarkers(
                    teacherUserId,
                    courseType,
                    rawText,
                    promptImageUrls);

            if (toeicWritingTasks.Any() &&
                !expectedTaskCount.HasValue)
            {
                return toeicWritingTasks;
            }

            if (expectedTaskCount.HasValue &&
                toeicWritingTasks.Count == expectedTaskCount.Value)
            {
                return toeicWritingTasks;
            }

        }
        else
        {
            var structuredWritingTasks =
                TryCreateStructuredWritingTasksFromMarkers(
                    teacherUserId,
                    courseType,
                    rawText);

            if (structuredWritingTasks.Any() &&
                !expectedTaskCount.HasValue)
            {
                return structuredWritingTasks;
            }

            if (expectedTaskCount.HasValue &&
                structuredWritingTasks.Count == expectedTaskCount.Value)
            {
                return structuredWritingTasks;
            }

        }

        throw new Exception(
            $"Writing local parser could not parse the required " +
            $"{expectedTaskCount ?? 1} {courseType} task(s). " +
            "Required format: Task/Part markers and the required prompt fields. " +
            "AI parsing is temporarily disabled for imports.");
    }

    private static string NormalizeVstepWritingTaskMarkers(
        string rawText)
    {
        return Regex.Replace(
            rawText,
            @"(?im)(?<!^)(?<!\n)\s+(?=Task\s+[12]\b)",
            "\n");
    }

    private async Task<List<WritingTask>>
        RecoverMissingWritingTasksAsync(
            int teacherUserId,
            CourseType courseType,
            string rawText,
            List<WritingTask> localTasks,
            IReadOnlyList<int> expectedNumbers,
            IReadOnlyDictionary<int, string> promptImageUrls,
            bool toeicMarkers)
    {
        var existingNumbers = localTasks
            .Select(task => task.OrderIndex)
            .ToHashSet();
        var missingNumbers = expectedNumbers
            .Where(number => !existingNumbers.Contains(number))
            .OrderBy(number => number)
            .ToList();

        if (!missingNumbers.Any())
        {
            return localTasks
                .OrderBy(task => task.OrderIndex)
                .ToList();
        }

        var blocks = ExtractWritingTaskBlocks(
            rawText,
            toeicMarkers);

        if (missingNumbers.Any(number => !blocks.ContainsKey(number)))
        {
            return new List<WritingTask>();
        }

        var fallbackText = string.Join(
            "\n\n",
            missingNumbers.Select(number => blocks[number]));
        var parsedFallback =
            await _writingAiParserService
                .ParseWritingTaskAsync(fallbackText);
        var fallbackTasks = MapAiWritingTasks(
            parsedFallback,
            teacherUserId,
            courseType);

        for (var index = 0; index < fallbackTasks.Count; index++)
        {
            var task = fallbackTasks[index];

            if (!missingNumbers.Contains(task.OrderIndex) &&
                index < missingNumbers.Count)
            {
                task.OrderIndex = missingNumbers[index];
            }

            if (promptImageUrls.TryGetValue(
                    task.OrderIndex,
                    out var promptImageUrl))
            {
                task.PromptImageUrl = promptImageUrl;
            }
        }

        return localTasks
            .Concat(fallbackTasks)
            .Where(task => expectedNumbers.Contains(task.OrderIndex))
            .GroupBy(task => task.OrderIndex)
            .Select(group => group.First())
            .OrderBy(task => task.OrderIndex)
            .ToList();
    }

    private static Dictionary<int, string> ExtractWritingTaskBlocks(
        string rawText,
        bool toeicMarkers)
    {
        var normalized = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\f", "\n");
        var pattern = toeicMarkers
            ? @"(?im)^\s*Question\s+(\d{1,2})\s*:"
            : @"(?im)^\s*(?:Task|Part)\s+(\d{1,2})\s*[:\.\-]?";
        var matches = Regex.Matches(normalized, pattern)
            .Cast<Match>()
            .Select(match => new
            {
                Number = int.Parse(match.Groups[1].Value),
                Match = match
            })
            .GroupBy(item => item.Number)
            .Select(group => group.First())
            .OrderBy(item => item.Match.Index)
            .ToList();
        var result = new Dictionary<int, string>();

        for (var index = 0; index < matches.Count; index++)
        {
            var current = matches[index];
            var endIndex = index + 1 < matches.Count
                ? matches[index + 1].Match.Index
                : normalized.Length;
            result[current.Number] = normalized
                .Substring(
                    current.Match.Index,
                    endIndex - current.Match.Index)
                .Trim();
        }

        return result;
    }

    private static List<WritingTask> MapAiWritingTasks(
        AiParsedWritingTaskDto parsedResult,
        int teacherUserId,
        CourseType courseType)
    {
        return parsedResult.Tasks
            .OrderBy(task => task.OrderIndex)
            .Select((task, index) => new WritingTask
            {
                TeacherUserId = teacherUserId,
                Title = string.IsNullOrWhiteSpace(task.Title)
                    ? $"Writing Task {index + 1}"
                    : task.Title.Trim(),
                Instruction = task.Instruction,
                PromptText = task.PromptText,
                ReferenceText = task.TopicText,
                SourceText = task.SourceText,
                Requirements = task.Requirements,
                ScoringCriteria = task.ScoringCriteria,
                MinimumWords = task.MinimumWords.GetValueOrDefault() <= 0
                    ? 120
                    : task.MinimumWords.GetValueOrDefault(),
                OrderIndex = task.OrderIndex > 0
                    ? task.OrderIndex
                    : index + 1,
                TaskType = task.TaskType.Equals(
                    "Essay",
                    StringComparison.OrdinalIgnoreCase)
                    ? WritingTaskType.Essay
                    : WritingTaskType.EmailReply,
                CourseType = courseType,
                IsActive = true
            })
            .Where(task => !string.IsNullOrWhiteSpace(task.PromptText))
            .ToList();
    }

    private static List<WritingTask>
        TryCreateStructuredWritingTasksFromMarkers(
            int teacherUserId,
            CourseType courseType,
            string rawText)
    {
        var normalized = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\f", "\n");

        var matches = Regex.Matches(
                normalized,
                @"(?im)^\s*(Task|Part)\s+(\d{1,2})\s*[:\.\-]?\s*(.*)$",
                RegexOptions.CultureInvariant)
            .Cast<Match>()
            .Select(match => new
            {
                Kind = match.Groups[1].Value,
                Number = int.Parse(match.Groups[2].Value),
                Heading = match.Groups[3].Value.Trim(),
                Match = match
            })
            .Where(item =>
                item.Number > 0 &&
                item.Number <= 10)
            .GroupBy(item => item.Number)
            .Select(group => group.First())
            .OrderBy(item => item.Match.Index)
            .ToList();

        if (matches.Count == 0)
        {
            return new List<WritingTask>();
        }

        var orderedNumbers = matches
            .Select(item => item.Number)
            .OrderBy(number => number)
            .ToList();

        var expectedNumbers = Enumerable
            .Range(
                orderedNumbers.First(),
                orderedNumbers.Count)
            .ToList();

        if (!orderedNumbers.SequenceEqual(expectedNumbers))
        {
            return new List<WritingTask>();
        }

        var tasks = new List<WritingTask>();

        for (var index = 0; index < matches.Count; index++)
        {
            var current = matches[index];
            var nextIndex = index + 1 < matches.Count
                ? matches[index + 1].Match.Index
                : normalized.Length;

            var block = normalized
                .Substring(
                    current.Match.Index,
                    nextIndex - current.Match.Index)
                .Trim();

            block = CleanStructuredWritingBlock(block);

            if (string.IsNullOrWhiteSpace(block))
            {
                return new List<WritingTask>();
            }

            tasks.Add(new WritingTask
            {
                TeacherUserId = teacherUserId,
                Title = string.IsNullOrWhiteSpace(current.Heading)
                    ? $"{current.Kind} {current.Number}"
                    : $"{current.Kind} {current.Number} - {current.Heading}",
                Instruction = ExtractStructuredWritingInstruction(block),
                PromptText = block,
                ReferenceText = courseType == CourseType.VSTEP
                    ? null
                    : block,
                SourceText = courseType == CourseType.VSTEP
                    ? null
                    : ExtractStructuredWritingSourceText(block),
                Requirements = ExtractToeicWritingRequirements(block),
                ScoringCriteria = ExtractToeicWritingScoringCriteria(block),
                MinimumWords = ExtractMinimumWords(
                    block,
                    current.Number),
                OrderIndex = current.Number,
                TaskType = IsEssayWritingTask(block)
                    ? WritingTaskType.Essay
                    : WritingTaskType.EmailReply,
                CourseType = courseType,
                IsActive = true
            });
        }

        return tasks;
    }

    private static List<WritingTask> TryCreateToeicWritingTasksFromMarkers(
        int teacherUserId,
        CourseType courseType,
        string rawText,
        IReadOnlyDictionary<int, string> promptImageUrls)
    {
        var matches = Regex.Matches(
                rawText,
                @"(?im)\bQuestion\s+(\d{1,2})\s*:\s*(.*)$",
                RegexOptions.CultureInvariant)
            .Cast<Match>()
            .Select(match => new
            {
                Number = int.Parse(match.Groups[1].Value),
                Heading = match.Groups[2].Value.Trim(),
                Match = match
            })
            .Where(item =>
                item.Number >= 1 &&
                item.Number <= 8)
            .GroupBy(item => item.Number)
            .Select(group => group.First())
            .OrderBy(item => item.Number)
            .ToList();

        var tasks = new List<WritingTask>();

        for (var index = 0; index < matches.Count; index++)
        {
            var current = matches[index];
            var nextIndex = index + 1 < matches.Count
                ? matches[index + 1].Match.Index
                : rawText.Length;

            var block = rawText
                .Substring(
                    current.Match.Index,
                    nextIndex - current.Match.Index)
                .Trim();

            block = CleanToeicWritingBlock(block);

            var promptText =
                ExtractToeicWritingPromptText(
                    block,
                    current.Number);

            var sourceText =
                ExtractToeicWritingSourceText(
                    block,
                    current.Number);

            var instruction =
                ExtractToeicWritingInstruction(
                    rawText,
                    block,
                    current.Number,
                    current.Match.Index);

            tasks.Add(new WritingTask
            {
                TeacherUserId = teacherUserId,
                Title = string.IsNullOrWhiteSpace(current.Heading)
                    ? $"Question {current.Number}"
                    : current.Heading,
                Instruction = string.IsNullOrWhiteSpace(instruction)
                    ? GetToeicWritingInstruction(current.Number)
                    : instruction,
                PromptText = string.IsNullOrWhiteSpace(promptText)
                    ? block
                    : promptText,
                PromptImageUrl = promptImageUrls.TryGetValue(
                    current.Number,
                    out var promptImageUrl)
                    ? promptImageUrl
                    : null,
                ReferenceText = null,
                SourceText = sourceText,
                Requirements = ExtractToeicWritingRequirements(
                    block,
                    current.Number),
                ScoringCriteria = ExtractToeicWritingScoringCriteria(
                    block),
                MinimumWords = ExtractMinimumWords(
                    block,
                    current.Number),
                OrderIndex = current.Number,
                TaskType = current.Number == 8
                    ? WritingTaskType.Essay
                    : WritingTaskType.EmailReply,
                CourseType = courseType,
                IsActive = true
            });
        }

        return tasks;
    }

    private async Task<Dictionary<int, string>>
        ExtractToeicWritingPromptImageUrlsAsync(
            byte[] documentBytes,
            string extension,
            int teacherUserId)
    {
        var result = new Dictionary<int, string>();

        if (string.Equals(
                extension,
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            var pdfImages =
                await _toeicPdfPromptImageExtractorService
                    .ExtractWritingPromptImagesAsync(
                        documentBytes);

            foreach (var item in pdfImages)
            {
                await using var imageStream =
                    new MemoryStream(item.Value.Content);
                var objectName =
                    $"writing/toeic/{teacherUserId}/" +
                    $"{Guid.NewGuid():N}" +
                    $"{item.Value.FileExtension}";
                var imageUrl =
                    await _minioService.UploadFileAsync(
                        imageStream,
                        objectName,
                        item.Value.ContentType);

                result[item.Key] = imageUrl;
            }

            return result;
        }

        if (!string.Equals(
                extension,
                ".docx",
                StringComparison.OrdinalIgnoreCase))
        {
            return result;
        }

        using var documentStream =
            new MemoryStream(documentBytes);

        using var archive = new ZipArchive(
            documentStream,
            ZipArchiveMode.Read,
            leaveOpen: true);

        var documentEntry =
            archive.GetEntry("word/document.xml");
        var relationshipEntry =
            archive.GetEntry("word/_rels/document.xml.rels");

        if (documentEntry == null ||
            relationshipEntry == null)
        {
            return result;
        }

        var relationshipMap =
            LoadDocxImageRelationshipMap(relationshipEntry);

        var imageQuestionMap =
            LoadToeicWritingImageQuestionMap(
                documentEntry);

        foreach (var imageMapping in imageQuestionMap)
        {
            var relationshipId = imageMapping.Key;

            if (!relationshipMap.ContainsKey(
                    relationshipId))
            {
                continue;
            }

            var imagePath = relationshipMap[relationshipId];
            var imageEntry = archive.GetEntry(imagePath);

            if (imageEntry == null)
            {
                continue;
            }

            await using var imageEntryStream = imageEntry.Open();
            using var imageMemoryStream = new MemoryStream();

            await imageEntryStream.CopyToAsync(imageMemoryStream);

            imageMemoryStream.Position = 0;

            var fileName = Path.GetFileName(imageEntry.FullName);
            var objectName =
                $"writing/toeic/{teacherUserId}/{Guid.NewGuid():N}-{fileName}";
            var imageUrl =
                await _minioService.UploadFileAsync(
                    imageMemoryStream,
                    objectName,
                    GetImageContentType(fileName));

            result[imageMapping.Value] = imageUrl;
        }

        return result;
    }

    private static Dictionary<string, string> LoadDocxImageRelationshipMap(
        ZipArchiveEntry relationshipEntry)
    {
        using var relationshipStream = relationshipEntry.Open();
        var document = XDocument.Load(relationshipStream);

        XNamespace relNamespace =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        return document
            .Descendants(relNamespace + "Relationship")
            .Where(element =>
                ((string?)element.Attribute("Type") ?? string.Empty)
                .Contains(
                    "/image",
                    StringComparison.OrdinalIgnoreCase))
            .Select(element => new
            {
                Id = (string?)element.Attribute("Id"),
                Target = (string?)element.Attribute("Target")
            })
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Id) &&
                !string.IsNullOrWhiteSpace(item.Target))
            .ToDictionary(
                item => item.Id!,
                item => NormalizeDocxRelationshipTarget(item.Target!));
    }

    private static Dictionary<string, int>
        LoadToeicWritingImageQuestionMap(
        ZipArchiveEntry documentEntry)
    {
        using var documentStream = documentEntry.Open();
        var document = XDocument.Load(documentStream);

        XNamespace wordNamespace =
            "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XNamespace drawingNamespace =
            "http://schemas.openxmlformats.org/drawingml/2006/main";
        XNamespace relationshipNamespace =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var result = new Dictionary<string, int>();
        var inWritingSection = false;
        var currentQuestionNumber = 0;

        foreach (var paragraph in document
            .Descendants(wordNamespace + "p"))
        {
            var text = string.Concat(
                    paragraph
                        .Descendants(wordNamespace + "t")
                        .Select(element => element.Value))
                .Trim();

            if (Regex.IsMatch(
                    text,
                    @"(?i)\bWRITING\s+TEST\b") ||
                Regex.IsMatch(
                    text,
                    @"(?i)\bQuestions?\s*1\s*[-–]\s*5\b.*?\bpicture\b") ||
                Regex.IsMatch(
                    text,
                    @"(?i)\bQuestion\s*1\s*:\s*write\s+a\s+sentence\b"))
            {
                inWritingSection = true;
            }

            if (!inWritingSection)
            {
                continue;
            }

            var questionMatch = Regex.Match(
                text,
                @"(?i)\bQuestion\s*([1-8])\s*:");

            if (questionMatch.Success)
            {
                currentQuestionNumber =
                    int.Parse(
                        questionMatch.Groups[1].Value);
            }

            if (currentQuestionNumber is < 1 or > 5)
            {
                continue;
            }

            var imageIds = paragraph
                .Descendants(drawingNamespace + "blip")
                .Select(element =>
                    (string?)element.Attribute(
                        relationshipNamespace + "embed"))
                .Where(id =>
                    !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Distinct();

            foreach (var imageId in imageIds)
            {
                result.TryAdd(
                    imageId,
                    currentQuestionNumber);
            }
        }

        return result;
    }

    private static string NormalizeDocxRelationshipTarget(
        string target)
    {
        target = target.Replace("\\", "/");

        if (target.StartsWith("/"))
        {
            return target.TrimStart('/');
        }

        return target.StartsWith(
            "word/",
            StringComparison.OrdinalIgnoreCase)
            ? target
            : $"word/{target}";
    }

    private static string GetImageContentType(
        string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }

    private static string CleanToeicWritingBlock(string block)
    {
        block = RemoveToeicSampleTestFooter(block);

        var lines = block
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line =>
                !Regex.IsMatch(
                    line,
                    @"^\d*\s*TOEIC.*SAMPLE\s+TESTS",
                    RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(
                    line,
                    @"^\d+\s*$",
                    RegexOptions.IgnoreCase))
            .ToList();

        return string.Join(
            Environment.NewLine,
            lines).Trim();
    }

    private static string RemoveToeicSampleTestFooter(
        string value)
    {
        return Regex.Replace(
                value,
                @"(?im)\s*(?:\d+\s+)?TOEIC\s*(?:®|\(R\))?\s*S\s*A\s*M\s*P\s*L\s*E\s*T\s*E\s*S\s*T\s*S[^\r\n]*",
                string.Empty)
            .Trim();
    }

    private static string CleanStructuredWritingBlock(string block)
    {
        var lines = block
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line =>
                !Regex.IsMatch(
                    line,
                    @"^\d+\s*$",
                    RegexOptions.IgnoreCase))
            .ToList();

        return string.Join(
            Environment.NewLine,
            lines).Trim();
    }

    private static string? ExtractStructuredWritingInstruction(
        string block)
    {
        var match = Regex.Match(
            block,
            @"(?im)^\s*(Instructions?|Directions?)\s*:\s*(.+)$");

        return match.Success
            ? match.Groups[2].Value.Trim()
            : null;
    }

    private static string? ExtractStructuredWritingSourceText(
        string block)
    {
        var match = Regex.Match(
            block,
            @"(?is)\b(?:From|To|Subject|Dear|Email|Letter|Message)\b.*");

        return match.Success
            ? match.Value.Trim()
            : null;
    }

    private static bool IsEssayWritingTask(string block)
    {
        return Regex.IsMatch(
            block,
            @"\b(essay|opinion|agree\s+or\s+disagree|advantages?|disadvantages?|discuss|state\s+your\s+opinion)\b",
            RegexOptions.IgnoreCase);
    }

    private static string GetToeicWritingInstruction(
        int questionNumber)
    {
        if (questionNumber <= 5)
        {
            return "Write one sentence based on a picture.";
        }

        if (questionNumber <= 7)
        {
            return "Respond to a written request.";
        }

        return "Write an opinion essay.";
    }

    private static string? ExtractToeicWritingInstruction(
        string rawText,
        string block,
        int questionNumber,
        int questionMarkerIndex)
    {
        var blockInstruction =
            ExtractToeicWritingInstructionFromText(block);

        if (!string.IsNullOrWhiteSpace(blockInstruction))
        {
            return blockInstruction;
        }

        var sectionPattern = questionNumber <= 5
            ? @"(?im)^\s*Questions\s+1\s*[-–]\s*5\b.*$"
            : questionNumber <= 7
                ? @"(?im)^\s*Questions\s+6\s*[-–]\s*7\b.*$"
                : @"(?im)^\s*Question\s+8\b.*$";

        var sectionMatch = Regex
            .Matches(
                rawText,
                sectionPattern,
                RegexOptions.CultureInvariant)
            .Cast<Match>()
            .Where(match => match.Index < questionMarkerIndex)
            .OrderByDescending(match => match.Index)
            .FirstOrDefault();

        if (sectionMatch == null)
        {
            return null;
        }

        var sectionText = rawText
            .Substring(
                sectionMatch.Index,
                questionMarkerIndex - sectionMatch.Index);

        return ExtractToeicWritingInstructionFromText(
            CleanToeicWritingBlock(sectionText));
    }

    private static string? ExtractToeicWritingInstructionFromText(
        string text)
    {
        var normalizedText = Regex.Replace(
                text,
                @"\s+",
                " ")
            .Trim();
        var directionsMatch = Regex.Match(
            normalizedText,
            @"(?i)\b(?:Directions?|Instructions?)\s*:");

        if (directionsMatch.Success)
        {
            var instructionStart = directionsMatch.Index;
            var instructionSearchStart =
                directionsMatch.Index + directionsMatch.Length;
            var instructionRemainder = normalizedText
                .Substring(instructionSearchStart);
            var instructionEndCandidates = new[]
                {
                    Regex.Match(
                        instructionRemainder,
                        @"(?i)\bYour\s+(?:sentence|response)\s+will\s+be\s+scored\s+on\b"),
                    Regex.Match(
                        instructionRemainder,
                        @"(?i)\bQuestion\s*:")
                }
                .Where(match => match.Success)
                .Select(match =>
                    instructionSearchStart + match.Index)
                .ToList();
            var instructionEnd = instructionEndCandidates.Any()
                ? instructionEndCandidates.Min()
                : normalizedText.Length;
            var instructionParts = new List<string>
            {
                normalizedText
                    .Substring(
                        instructionStart,
                        instructionEnd - instructionStart)
                    .Trim()
            };

            var supplementalMatches = Regex.Matches(
                    normalizedText,
                    @"(?i)\b(?:In\s+this\s+part,\s+you\s+can\s+move|If\s+you\s+want\s+to\s+return|You\s+will\s+have)\b.*?(?:\.|$)")
                .Cast<Match>()
                .Select(match => match.Value.Trim())
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value) &&
                    instructionParts.All(existing =>
                        !existing.Contains(
                            value,
                            StringComparison.OrdinalIgnoreCase)))
                .ToList();

            instructionParts.AddRange(supplementalMatches);

            return string.Join(
                Environment.NewLine,
                instructionParts.Where(value =>
                    !string.IsNullOrWhiteSpace(value)));
        }

        var lines = text
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var directionsIndex = lines.FindIndex(line =>
            Regex.IsMatch(
                line,
                @"^(Directions?|Instructions?)\s*:",
                RegexOptions.IgnoreCase));

        if (directionsIndex < 0)
        {
            return null;
        }

        var instructionLines = new List<string>();

        for (var index = directionsIndex; index < lines.Count; index++)
        {
            var line = lines[index];

            if (Regex.IsMatch(
                    line,
                    @"^Your\s+(sentence|response)\s+will\s+be\s+scored\s+on\b",
                    RegexOptions.IgnoreCase))
            {
                break;
            }

            instructionLines.Add(line);
        }

        var extraInstructionLines = lines
            .Where(line =>
                Regex.IsMatch(
                    line,
                    @"^(In this part|You can move|If you want|You will have)\b",
                    RegexOptions.IgnoreCase))
            .Where(line =>
                instructionLines.All(existing =>
                    !string.Equals(
                        existing,
                        line,
                        StringComparison.OrdinalIgnoreCase)))
            .ToList();

        instructionLines.AddRange(extraInstructionLines);

        return instructionLines.Any()
            ? string.Join(Environment.NewLine, instructionLines)
            : null;
    }

    private static string? ExtractToeicWritingRequirements(
        string block)
    {
        var requirementLines = block
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line =>
                line.StartsWith("- ") ||
                line.StartsWith("* ") ||
                Regex.IsMatch(
                    line,
                    @"\b(at least|minimum|two|one)\b",
                    RegexOptions.IgnoreCase))
            .ToList();

        return requirementLines.Any()
            ? string.Join(Environment.NewLine, requirementLines)
            : null;
    }

    private static string? ExtractToeicWritingRequirements(
        string block,
        int questionNumber)
    {
        if (questionNumber == 8)
        {
            return null;
        }

        var requirementLines = block
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line =>
                line.StartsWith("- ") ||
                line.StartsWith("* ") ||
                Regex.IsMatch(
                    line,
                    @"\b(at least|minimum|two|one)\b",
                    RegexOptions.IgnoreCase))
            .Where(line =>
                !Regex.IsMatch(
                    line,
                    @"\bscored\s+on\b",
                    RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(
                    line,
                    @"\bminimum\s+of\s+\d+\s+words?\b",
                    RegexOptions.IgnoreCase))
            .ToList();

        return requirementLines.Any()
            ? string.Join(Environment.NewLine, requirementLines)
            : null;
    }

    private static string? ExtractToeicWritingScoringCriteria(
        string block)
    {
        var lines = block
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var startIndex = lines.FindIndex(line =>
            Regex.IsMatch(
                line,
                @"\bscored\s+on\b",
                RegexOptions.IgnoreCase));

        if (startIndex < 0)
        {
            return null;
        }

        var criteriaLines = new List<string>();

        for (var index = startIndex + 1; index < lines.Count; index++)
        {
            var line = lines[index];

            if (Regex.IsMatch(
                    line,
                    @"^(In this part|You can move|If you want|You will have|There are many ways|Question\s+\d+\s*:)",
                    RegexOptions.IgnoreCase))
            {
                break;
            }

            criteriaLines.Add(line);

            if (Regex.IsMatch(
                    line,
                    @"\borganization\.?$",
                    RegexOptions.IgnoreCase))
            {
                break;
            }
        }

        if (!criteriaLines.Any())
        {
            return null;
        }

        return string.Join(" ", criteriaLines)
            .Replace("•", string.Empty)
            .Trim();
    }

    private static string? ExtractToeicWritingPromptText(
        string block,
        int questionNumber)
    {
        var withoutHeading = Regex.Replace(
                block,
                @"(?im)^\s*Question\s+\d+\s*:\s*",
                string.Empty,
                RegexOptions.CultureInvariant)
            .Trim();

        if (questionNumber <= 5)
        {
            return FirstNonEmptyLine(withoutHeading);
        }

        if (questionNumber <= 7)
        {
            var questionMatch = Regex.Match(
                withoutHeading,
                @"(?im)^\s*Question\s*:\s*(.+)$");

            return questionMatch.Success
                ? questionMatch.Groups[1].Value.Trim()
                : FirstNonEmptyLine(withoutHeading);
        }

        var lines = withoutHeading
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var promptStartIndex = lines.FindIndex(line =>
            !Regex.IsMatch(
                line,
                @"^(Directions?|Write an opinion essay|Your response|whether|grammar|organization|You will have|In this part|Typically|•)",
                RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(
                line,
                @"\bscored\s+on\b",
                RegexOptions.IgnoreCase));

        if (promptStartIndex < 0)
        {
            return FirstNonEmptyLine(withoutHeading);
        }

        return string.Join(
            Environment.NewLine,
            lines.Skip(promptStartIndex));
    }

    private static string? ExtractToeicWritingSourceText(
        string block,
        int questionNumber)
    {
        if (questionNumber <= 5 ||
            questionNumber == 8)
        {
            return null;
        }

        var sourceMatch = Regex.Match(
            block,
            @"(?is)\b(?:From\s*:|From:|Dear\s+).*$");

        return sourceMatch.Success
            ? sourceMatch.Value.Trim()
            : null;
    }

    private static string? FirstNonEmptyLine(
        string text)
    {
        return text
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None)
            .Select(line => line.Trim())
            .FirstOrDefault(line =>
                !string.IsNullOrWhiteSpace(line));
    }

    private static int ExtractMinimumWords(
        string block,
        int questionNumber)
    {
        var match = Regex.Match(
            block,
            @"(?i)\b(?:minimum\s+of|at\s+least)\s+(\d+)\s+words?\b");

        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out var minimumWords) &&
            minimumWords > 0)
        {
            return minimumWords;
        }

        if (questionNumber <= 5)
        {
            return 1;
        }

        return questionNumber == 8
            ? 300
            : 120;
    }
}
