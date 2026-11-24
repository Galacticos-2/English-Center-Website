using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Speaking;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Application.Common.Enums;
using EnglishCenterSystem.Application.Common.Helpers;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
namespace EnglishCenterSystem.Application.Features.Speaking.Services;

public class SpeakingCoreImportService
{
    private readonly IMinioService _minioService;
    private readonly IExerciseDocumentParserService _documentParserService;
    private readonly ISpeakingAiParserService _speakingAiParserService;
    private readonly IToeicPdfPromptImageExtractorService
        _toeicPdfPromptImageExtractorService;

    public SpeakingCoreImportService(
        IMinioService minioService,
        IExerciseDocumentParserService documentParserService,
        ISpeakingAiParserService speakingAiParserService,
        IToeicPdfPromptImageExtractorService
            toeicPdfPromptImageExtractorService)
    {
        _minioService = minioService;
        _documentParserService = documentParserService;
        _speakingAiParserService = speakingAiParserService;
        _toeicPdfPromptImageExtractorService =
            toeicPdfPromptImageExtractorService;
    }

    public async Task<List<SpeakingTask>>
    CreateSpeakingTasksAsync(
        int teacherUserId,
        ImportStorageModule storageModule,
        string storageFolderName,
        Stream questionStream,
        string questionFileName,
        string contentType,
        string? description,
        CourseType courseType,
        int? preparationTimeSeconds,
        int? speakingTimeSeconds,
        int? expectedTaskCount = null,
        byte[]? promptImageDocumentBytes = null,
        string? promptImageDocumentFileName = null,
        string? promptImageDocumentContentType = null)
    {
        if (string.IsNullOrWhiteSpace(
            storageFolderName))
        {
            throw new Exception(
                "Storage folder name is required");
        }

        var extension =
            Path.GetExtension(
                questionFileName)
            .ToLowerInvariant();

        if (extension != ".pdf" &&
            extension != ".docx" &&
            extension != ".txt")
        {
            throw new Exception(
                "Only PDF, Word or TXT question files are allowed");
        }

        byte[] questionFileBytes;

        using (var tempStream =
            new MemoryStream())
        {
            await questionStream.CopyToAsync(
                tempStream);

            questionFileBytes =
                tempStream.ToArray();
        }

        await using var extractStream =
            new MemoryStream(
                questionFileBytes);

        var promptText =
            await _documentParserService
                .ExtractTextAsync(
                    extractStream,
                    questionFileName);

        if (string.IsNullOrWhiteSpace(
            promptText))
        {
            throw new Exception(
                "Cannot read speaking document content");
        }

        if (courseType == CourseType.VSTEP)
        {
            promptText = NormalizeVstepSpeakingPartMarkers(
                promptText);
        }

        var usePromptImageDocument =
            courseType == CourseType.TOEIC &&
            promptImageDocumentBytes != null;
        var sourceDocumentBytes = usePromptImageDocument
            ? promptImageDocumentBytes!
            : questionFileBytes;
        var sourceDocumentFileName = usePromptImageDocument
            ? promptImageDocumentFileName ?? questionFileName
            : questionFileName;
        var sourceDocumentExtension = Path
            .GetExtension(sourceDocumentFileName)
            .ToLowerInvariant();
        var sourceDocumentContentType = usePromptImageDocument
            ? promptImageDocumentContentType ?? contentType
            : contentType;

        var moduleFolder =
            StoragePathHelper.GetModuleFolder(
                storageModule);

        var resourceFolder =
            StoragePathHelper.BuildResourceFolder(
                storageFolderName,
                "speaking-set");

        var safeQuestionFileName =
            StoragePathHelper.SanitizeFileName(
                sourceDocumentFileName);

        var objectName =
            $"{moduleFolder}/speaking/" +
            $"{resourceFolder}/source/" +
            $"{Guid.NewGuid():N}_" +
            $"{safeQuestionFileName}";

        string? pdfUrl = null;
        var promptImageUrls = new Dictionary<int, string>();

        try
        {
            await using var uploadStream =
                new MemoryStream(
                    sourceDocumentBytes);

            pdfUrl =
                await _minioService
                    .UploadFileAsync(
                        uploadStream,
                        objectName,
                        sourceDocumentContentType);

            if (courseType == CourseType.TOEIC)
            {
                promptImageUrls =
                    await ExtractToeicSpeakingPromptImageUrlsAsync(
                        sourceDocumentBytes,
                        sourceDocumentExtension,
                        teacherUserId,
                        moduleFolder,
                        resourceFolder);
            }

            var speakingTasks = courseType switch
            {
                CourseType.TOEIC =>
                    TryCreateToeicSpeakingTasksFromMarkers(
                        teacherUserId,
                        pdfUrl,
                        objectName,
                        promptText,
                        description,
                        courseType,
                        promptImageUrls),

                CourseType.VSTEP =>
                    TryCreateVstepSpeakingTasksFromParts(
                        teacherUserId,
                        pdfUrl,
                        objectName,
                        promptText,
                        preparationTimeSeconds,
                        speakingTimeSeconds),

                _ => new List<SpeakingTask>()
            };

            var localParseComplete = speakingTasks.Any() &&
                (!expectedTaskCount.HasValue ||
                 speakingTasks.Count == expectedTaskCount.Value);

            if (!localParseComplete)
            {
                throw new Exception(
                    $"Speaking local parser could not parse the required " +
                    $"{expectedTaskCount ?? 1} {courseType} task(s). " +
                    "Use the supported task markers in the source file. " +
                    "AI parsing is temporarily disabled for imports.");
            }

            if (expectedTaskCount.HasValue &&
                speakingTasks.Count != expectedTaskCount.Value)
            {
                throw new Exception(
                    $"Speaking requires {expectedTaskCount.Value} tasks " +
                    "but parsed " +
                    speakingTasks.Count);
            }

            if (!speakingTasks.Any())
            {
                throw new Exception(
                    "Speaking local parser found no valid task. " +
                    "AI parsing is temporarily disabled for imports.");
            }

            return speakingTasks;
        }
        catch
        {
            foreach (var promptImageUrl in
                promptImageUrls.Values.Distinct())
            {
                try
                {
                    await _minioService
                        .DeleteFileByUrlAsync(
                            promptImageUrl);
                }
                catch
                {
                    // Preserve the original import exception.
                }
            }

            if (!string.IsNullOrWhiteSpace(
                pdfUrl))
            {
                try
                {
                    await _minioService
                        .DeleteFileAsync(
                            objectName);
                }
                catch
                {
                    // Có thể bổ sung logger sau.
                }
            }

            throw;
        }
    }

    private static string NormalizeVstepSpeakingPartMarkers(
        string promptText)
    {
        return Regex.Replace(
            promptText,
            @"(?im)(?<!^)(?<!\n)\s+(?=Part\s+[1-3]\s*:\s*(?:Social\s+Interaction|Solution\s+Discussion|Topic\s+Development)\b)",
            "\n");
    }

    private static List<SpeakingTask>
        TryCreateVstepSpeakingTasksFromParts(
            int teacherUserId,
            string pdfUrl,
            string objectName,
            string rawText,
            int? preparationTimeSeconds,
            int? speakingTimeSeconds)
    {
        var normalized = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\f", "\n");

        var parts = Regex.Matches(
                normalized,
                @"(?i)\bPart\s*([1-3])\s*[:.\-–—]\s*(Social\s+Interaction|Solution\s+Discussion|Topic\s+Development)\b")
            .Cast<Match>()
            .Select(match => new VstepSpeakingPartMarker(
                int.Parse(match.Groups[1].Value),
                $"Part {match.Groups[1].Value}: " +
                    match.Groups[2].Value.Trim(),
                match))
            .GroupBy(part => part.Number)
            .Select(group => group.First())
            .OrderBy(part => part.Number)
            .ToList();

        if (parts.Count != 3 ||
            parts.Select(part => part.Number)
                .SequenceEqual(new[] { 1, 2, 3 }) == false)
        {
            return new List<SpeakingTask>();
        }

        var tasks = new List<SpeakingTask>();

        for (var index = 0; index < parts.Count; index++)
        {
            var current = parts[index];
            var contentStart = current.Match.Index + current.Match.Length;
            var contentEnd = index + 1 < parts.Count
                ? parts[index + 1].Match.Index
                : normalized.Length;
            var promptText = normalized
                .Substring(contentStart, contentEnd - contentStart)
                .Trim();

            if (string.IsNullOrWhiteSpace(promptText))
            {
                return new List<SpeakingTask>();
            }

            tasks.Add(new SpeakingTask
            {
                TeacherUserId = teacherUserId,
                Title = $"Part {current.Number}",
                Instruction = current.Heading,
                PdfUrl = pdfUrl,
                PdfObjectName = objectName,
                PromptText = promptText,
                CourseType = CourseType.VSTEP,
                PreparationTimeSeconds = preparationTimeSeconds,
                SpeakingTimeSeconds = speakingTimeSeconds,
                IsActive = true
            });
        }

        return tasks;
    }

    private async Task<Dictionary<int, string>>
        ExtractToeicSpeakingPromptImageUrlsAsync(
            byte[] documentBytes,
            string extension,
            int teacherUserId,
            string moduleFolder,
            string resourceFolder)
    {
        var result = new Dictionary<int, string>();

        if (extension == ".pdf")
        {
            var pdfImages =
                await _toeicPdfPromptImageExtractorService
                    .ExtractSpeakingPromptImagesAsync(
                        documentBytes);

            foreach (var item in pdfImages)
            {
                await using var imageStream =
                    new MemoryStream(item.Value.Content);
                var objectName =
                    $"{moduleFolder}/speaking/" +
                    $"{resourceFolder}/images/" +
                    $"{teacherUserId}_{Guid.NewGuid():N}" +
                    item.Value.FileExtension;

                result[item.Key] =
                    await _minioService.UploadFileAsync(
                        imageStream,
                        objectName,
                        item.Value.ContentType);
            }

            return result;
        }

        if (extension != ".docx")
        {
            return result;
        }

        using var documentStream =
            new MemoryStream(documentBytes);
        using var archive = new ZipArchive(
            documentStream,
            ZipArchiveMode.Read,
            leaveOpen: false);
        var documentEntry =
            archive.GetEntry("word/document.xml");
        var relationshipEntry =
            archive.GetEntry(
                "word/_rels/document.xml.rels");

        if (documentEntry == null ||
            relationshipEntry == null)
        {
            return result;
        }

        var relationshipMap =
            LoadDocxImageRelationshipMap(
                relationshipEntry);
        var imageQuestionMap =
            LoadToeicSpeakingImageQuestionMap(
                documentEntry);

        foreach (var imageMapping in imageQuestionMap)
        {
            if (!relationshipMap.TryGetValue(
                    imageMapping.Key,
                    out var imagePath))
            {
                continue;
            }

            var imageEntry = archive.GetEntry(imagePath);

            if (imageEntry == null)
            {
                continue;
            }

            await using var imageEntryStream =
                imageEntry.Open();
            using var imageMemoryStream =
                new MemoryStream();

            await imageEntryStream.CopyToAsync(
                imageMemoryStream);
            imageMemoryStream.Position = 0;

            var fileName =
                Path.GetFileName(imageEntry.FullName);
            var objectName =
                $"{moduleFolder}/speaking/" +
                $"{resourceFolder}/images/" +
                $"{teacherUserId}_{Guid.NewGuid():N}_" +
                fileName;

            result[imageMapping.Value] =
                await _minioService.UploadFileAsync(
                    imageMemoryStream,
                    objectName,
                    GetImageContentType(fileName));
        }

        return result;
    }

    private static Dictionary<string, string>
        LoadDocxImageRelationshipMap(
            ZipArchiveEntry relationshipEntry)
    {
        using var relationshipStream =
            relationshipEntry.Open();
        var document = XDocument.Load(
            relationshipStream);
        XNamespace relationshipNamespace =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        return document
            .Descendants(
                relationshipNamespace + "Relationship")
            .Where(element =>
                ((string?)element.Attribute("Type") ??
                    string.Empty)
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
                item => NormalizeDocxRelationshipTarget(
                    item.Target!));
    }

    private static Dictionary<string, int>
        LoadToeicSpeakingImageQuestionMap(
            ZipArchiveEntry documentEntry)
    {
        using var documentStream = documentEntry.Open();
        var document = XDocument.Load(documentStream);
        XNamespace wordNamespace =
            "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XNamespace relationshipNamespace =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var result = new Dictionary<string, int>();
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
                    @"(?i)\bQuestions?\s*3\s*[-–—]\s*4\b.*?\bDescribe\s+(?:a\s+)?picture\b"))
            {
                currentQuestionNumber = 3;
            }

            if (Regex.IsMatch(
                    text,
                    @"(?i)\bQuestions?\s*5\s*[-–—]\s*7\b"))
            {
                currentQuestionNumber = 0;
            }

            var questionMatch = Regex.Match(
                text,
                @"(?i)\bQuestion\s*([34])\s*:");

            if (questionMatch.Success)
            {
                currentQuestionNumber =
                    int.Parse(
                        questionMatch.Groups[1].Value);
            }

            if (currentQuestionNumber is not (3 or 4))
            {
                continue;
            }

            var imageIds = paragraph
                .Descendants()
                .SelectMany(element =>
                    element.Attributes())
                .Where(attribute =>
                    attribute.Name.Namespace ==
                        relationshipNamespace &&
                    (attribute.Name.LocalName == "embed" ||
                     attribute.Name.LocalName == "id"))
                .Select(attribute => attribute.Value)
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .Distinct()
                .ToList();

            foreach (var imageId in imageIds)
            {
                if (!result.ContainsValue(
                        currentQuestionNumber))
                {
                    result[imageId] =
                        currentQuestionNumber;
                }
            }

            if (imageIds.Any() &&
                currentQuestionNumber == 3)
            {
                currentQuestionNumber = 4;
            }
        }

        return result;
    }

    private static string NormalizeDocxRelationshipTarget(
        string target)
    {
        var normalized = target
            .Replace('\\', '/')
            .TrimStart('/');

        while (normalized.StartsWith("../"))
        {
            normalized = normalized[3..];
        }

        return normalized.StartsWith(
            "word/",
            StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"word/{normalized}";
    }

    private static string GetImageContentType(
        string fileName)
    {
        return Path.GetExtension(fileName)
            .ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };
    }

    private static List<SpeakingTask>
        TryCreateToeicSpeakingTasksFromMarkers(
            int teacherUserId,
            string pdfUrl,
            string objectName,
            string rawText,
            string? description,
            CourseType courseType,
            IReadOnlyDictionary<int, string> promptImageUrls)
    {
        var normalized = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\f", "\n");

        var matches = Regex.Matches(
                normalized,
                @"(?im)\bQuestion\s+(\d{1,2})\s*:",
                RegexOptions.CultureInvariant)
            .Cast<Match>()
            .Select(match =>
                new ToeicSpeakingQuestionMarker(
                    int.Parse(match.Groups[1].Value),
                    match))
            .Where(item =>
                item.Number >= 1 &&
                item.Number <= 11)
            .GroupBy(item => item.Number)
            .Select(group => group.First())
            .OrderBy(item => item.Number)
            .ToList();

        var groupMatches = Regex.Matches(
                normalized,
                @"(?im)\bQuestions?\s+(\d{1,2})\s*[-–—]\s*(\d{1,2})\s*:\s*(Read\s+a\s+text\s+aloud|Describe\s+(?:a\s+)?picture|Respond\s+to\s+questions\s+using\s+information\s+provided|Respond\s+to\s+questions|Express\s+an\s+opinion)")
            .Cast<Match>()
            .ToList();
        var groupContexts =
            CreateToeicSpeakingGroupContexts(
                normalized,
                matches,
                groupMatches);
        var structuralIndexes = matches
            .Select(item => item.Match.Index)
            .Concat(groupMatches.Select(match => match.Index))
            .Distinct()
            .OrderBy(index => index)
            .ToList();

        var tasks = new List<SpeakingTask>();

        for (var index = 0; index < matches.Count; index++)
        {
            var current = matches[index];
            var nextIndex = structuralIndexes
                .FirstOrDefault(position =>
                    position > current.Match.Index);

            if (nextIndex == 0)
            {
                nextIndex = normalized.Length;
            }

            var block = normalized
                .Substring(
                    current.Match.Index,
                    nextIndex - current.Match.Index)
                .Trim();

            block = CleanToeicSpeakingBlock(block);
            block = Regex.Replace(
                    block,
                    $@"(?is)^\s*Question\s+{current.Number}\s*:\s*",
                    string.Empty)
                .Trim();

            groupContexts.TryGetValue(
                current.Number,
                out var groupContext);

            tasks.Add(new SpeakingTask
            {
                TeacherUserId = teacherUserId,
                Title = $"Question {current.Number}",
                Instruction =
                    groupContext?.Instruction ??
                    GetToeicSpeakingInstruction(
                        current.Number) ?? description,
                PdfUrl = pdfUrl,
                PdfObjectName = objectName,
                PromptText = block,
                PromptImageUrl =
                    promptImageUrls.TryGetValue(
                        current.Number,
                        out var promptImageUrl)
                        ? promptImageUrl
                        : null,
                ReferenceText = groupContext?.ReferenceText,
                CourseType = courseType,
                PreparationTimeSeconds =
                    GetToeicSpeakingPreparationTime(
                        current.Number),
                SpeakingTimeSeconds =
                    GetToeicSpeakingResponseTime(
                        current.Number),
                IsActive = true
            });
        }

        return tasks;
    }

    private static Dictionary<int, ToeicSpeakingGroupContext>
        CreateToeicSpeakingGroupContexts(
            string normalized,
            IReadOnlyCollection<ToeicSpeakingQuestionMarker>
                questionMarkers,
            IReadOnlyCollection<Match> groupMatches)
    {
        var result =
            new Dictionary<int, ToeicSpeakingGroupContext>();

        foreach (var groupMatch in groupMatches)
        {
            var startQuestion =
                int.Parse(groupMatch.Groups[1].Value);
            var endQuestion =
                int.Parse(groupMatch.Groups[2].Value);

            if (startQuestion < 1 ||
                endQuestion > 11 ||
                endQuestion < startQuestion)
            {
                continue;
            }

            var firstQuestionMarker = questionMarkers
                .Where(marker =>
                    marker.Number >= startQuestion &&
                    marker.Number <= endQuestion &&
                    marker.Match.Index > groupMatch.Index)
                .OrderBy(marker => marker.Match.Index)
                .FirstOrDefault();
            var contentEnd = firstQuestionMarker?.Match.Index ??
                normalized.Length;
            var heading =
                $"Questions {startQuestion}-{endQuestion}: " +
                groupMatch.Groups[3].Value.Trim();
            var bodyStart =
                groupMatch.Index + groupMatch.Length;
            var body = normalized
                .Substring(
                    bodyStart,
                    Math.Max(0, contentEnd - bodyStart))
                .Trim();
            var (instructionBody, referenceText) =
                SplitToeicSpeakingGroupBody(body);
            var instruction = string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        heading.Trim(),
                        instructionBody
                    }
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value)))
                .Trim();

            for (var questionNumber = startQuestion;
                 questionNumber <= endQuestion;
                 questionNumber++)
            {
                result[questionNumber] =
                    new ToeicSpeakingGroupContext(
                        instruction,
                        referenceText);
            }
        }

        return result;
    }

    private static (string Instruction, string? ReferenceText)
        SplitToeicSpeakingGroupBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return (string.Empty, null);
        }

        var referenceMatch = Regex.Match(
            body,
            @"(?i)\b(?:\(?Narrator\)?\s*:|Information\s*:|Schedule\s*:)");

        if (!referenceMatch.Success)
        {
            return (
                CleanToeicSpeakingBlock(body),
                null);
        }

        var instruction = CleanToeicSpeakingBlock(
            body[..referenceMatch.Index]);
        var referenceText = CleanToeicSpeakingBlock(
            body[referenceMatch.Index..]);

        return (
            instruction,
            string.IsNullOrWhiteSpace(referenceText)
                ? null
                : referenceText);
    }

    private static string CleanToeicSpeakingBlock(string block)
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

    private static string? GetToeicSpeakingInstruction(
        int questionNumber)
    {
        if (questionNumber <= 2)
        {
            return "Read aloud.";
        }

        if (questionNumber <= 4)
        {
            return "Describe a picture.";
        }

        if (questionNumber <= 7)
        {
            return "Respond to questions.";
        }

        if (questionNumber <= 10)
        {
            return "Respond using information provided.";
        }

        return "Express an opinion.";
    }

    private static int GetToeicSpeakingPreparationTime(
        int questionNumber)
    {
        if (questionNumber <= 4)
        {
            return 45;
        }

        if (questionNumber <= 7)
        {
            return 3;
        }

        if (questionNumber <= 10)
        {
            return 45;
        }

        return 45;
    }

    private static int GetToeicSpeakingResponseTime(
        int questionNumber)
    {
        if (questionNumber <= 2)
        {
            return 45;
        }

        if (questionNumber <= 4)
        {
            return 30;
        }

        if (questionNumber <= 6)
        {
            return 15;
        }

        if (questionNumber == 7)
        {
            return 30;
        }

        if (questionNumber <= 9)
        {
            return 15;
        }

        if (questionNumber == 10)
        {
            return 30;
        }

        return 60;
    }

    private sealed record ToeicSpeakingQuestionMarker(
        int Number,
        Match Match);

    private sealed record VstepSpeakingPartMarker(
        int Number,
        string Heading,
        Match Match);

    private sealed record ToeicSpeakingGroupContext(
        string Instruction,
        string? ReferenceText);
}
