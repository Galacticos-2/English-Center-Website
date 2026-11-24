using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Listening.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Listening;
using EnglishCenterSystem.Domain.Enums;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EnglishCenterSystem.Application.Common.Enums;
using EnglishCenterSystem.Application.Common.Helpers;
namespace EnglishCenterSystem.Application.Features.Listening.Services;

public class ListeningCoreImportService
{
    private readonly IMinioService _minioService;
    private readonly IExerciseDocumentParserService _documentParserService;
    private readonly IListeningAiParserService _listeningAiParserService;
    private readonly ISpeechAssessmentService _speechAssessmentService;
    private readonly IQuestionExplanationService _questionExplanationService;
    private readonly IToeicPdfPromptImageExtractorService
        _toeicPdfPromptImageExtractorService;

    public ListeningCoreImportService(
        IMinioService minioService,
        IExerciseDocumentParserService documentParserService,
        IListeningAiParserService listeningAiParserService,
        ISpeechAssessmentService speechAssessmentService,
        IQuestionExplanationService questionExplanationService,
        IToeicPdfPromptImageExtractorService
            toeicPdfPromptImageExtractorService)
    {
        _minioService = minioService;
        _documentParserService = documentParserService;
        _listeningAiParserService = listeningAiParserService;
        _speechAssessmentService = speechAssessmentService;
        _questionExplanationService = questionExplanationService;
        _toeicPdfPromptImageExtractorService =
            toeicPdfPromptImageExtractorService;
    }

    public async Task<List<ListeningTask>>
    CreateListeningTasksAsync(
        int teacherUserId,
        ImportStorageModule storageModule,
        string storageFolderName,
        string title,
        string? description,
        string? transcript,
        CourseType courseType,
        List<ListeningAudioUploadItem> audioFiles,
        Stream questionStream,
        string questionFileName,
        string questionCountsJson,
        bool useToeicListeningRules = false,
        byte[]? promptImageDocumentBytes = null,
        string? promptImageDocumentFileName = null,
        string? normalizedAnswerSourceText = null)
    {
        if (audioFiles == null ||
            audioFiles.Count == 0)
        {
            throw new Exception(
                "At least one audio file is required");
        }

        if (string.IsNullOrWhiteSpace(
            storageFolderName))
        {
            throw new Exception(
                "Storage folder name is required");
        }

        var questionCounts =
            System.Text.Json.JsonSerializer
                .Deserialize<List<int>>(
                    questionCountsJson)
            ?? new List<int>();

        if (questionCounts.Count !=
            audioFiles.Count)
        {
            throw new Exception(
                "Question counts must match the number of audio files");
        }

        if (questionCounts.Any(x => x <= 0))
        {
            throw new Exception(
                "Question count for each audio must be greater than 0");
        }

        var questionExtension =
            Path.GetExtension(
                questionFileName)
            .ToLowerInvariant();

        if (questionExtension != ".pdf" &&
            questionExtension != ".docx" &&
            questionExtension != ".txt")
        {
            throw new Exception(
                "Only PDF, Word or TXT question files are allowed");
        }

        var moduleFolder =
            StoragePathHelper.GetModuleFolder(
                storageModule);

        var resourceFolder =
            StoragePathHelper.BuildResourceFolder(
                storageFolderName,
                "listening-set");

        var baseFolder =
            $"{moduleFolder}/listening/" +
            $"{resourceFolder}";

        byte[] questionFileBytes;

        using (var tempQuestionStream =
            new MemoryStream())
        {
            await questionStream.CopyToAsync(
                tempQuestionStream);

            questionFileBytes =
                tempQuestionStream.ToArray();
        }

        var safeQuestionFileName =
            StoragePathHelper.SanitizeFileName(
                questionFileName);

        var questionObjectName =
            $"{baseFolder}/source/" +
            $"{Guid.NewGuid():N}_" +
            $"{safeQuestionFileName}";

        var contentType = questionExtension switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        await using var uploadQuestionStream =
            new MemoryStream(questionFileBytes);

        var questionFileUrl = await _minioService.UploadFileAsync(
            uploadQuestionStream,
            questionObjectName,
            contentType);

        await using var extractQuestionStream =
            new MemoryStream(questionFileBytes);

        var extractedText =
            await _documentParserService.ExtractTextAsync(
                extractQuestionStream,
                questionFileName);
        var isToeicListening =
            useToeicListeningRules ||
            courseType == CourseType.TOEIC;
        var listeningParts =
            ExtractListeningParts(extractedText, isToeicListening);
        var listeningLittleParts =
            ExtractListeningLittleParts(extractedText);
        var expectedQuestionCount = questionCounts.Sum();

        var answerMap = ExtractListeningAnswerMap(
            string.IsNullOrWhiteSpace(normalizedAnswerSourceText)
                ? extractedText
                : normalizedAnswerSourceText);
        var promptImages = isToeicListening
            ? await ExtractToeicListeningPromptImagesAsync(
                promptImageDocumentBytes ??
                    questionFileBytes,
                Path.GetExtension(
                    promptImageDocumentFileName ??
                    questionFileName)
                    .ToLowerInvariant(),
                teacherUserId,
                baseFolder)
            : new Dictionary<int, ListeningPromptImageAsset>();

        if (isToeicListening)
        {
            ValidateToeicListeningAnswerMap(
                answerMap,
                expectedQuestionCount);
        }

        var questionBlocks = SplitListeningQuestionBlocks(
            extractedText,
            answerMap,
            expectedQuestionCount,
            isToeicListening);

        var parsedQuestions = new List<AiParsedListeningQuestionDto>();
        var failedBlocks = new List<ListeningQuestionBlockDto>();

        foreach (var block in questionBlocks)
        {
            var parsed = ParseOneListeningQuestionBlockV2(
                block.QuestionNumber,
                block.RawText,
                isToeicListening);

            if (parsed == null)
            {
                if (
                    isToeicListening &&
                    TryCreateToeicListeningPlaceholder(
                        block.QuestionNumber,
                        answerMap,
                        out var placeholderQuestion))
                {
                    parsedQuestions.Add(placeholderQuestion);
                }
                else
                {
                    failedBlocks.Add(block);
                }

                continue;
            }

            ApplyCorrectAnswersFromAnswerSection(
                new List<AiParsedListeningQuestionDto> { parsed },
                answerMap);

            if (IsValidListeningQuestion(
                    parsed,
                    isToeicListening))
            {
                parsedQuestions.Add(parsed);
            }
            else if (
                isToeicListening &&
                TryCreateToeicListeningPlaceholder(
                    block.QuestionNumber,
                    answerMap,
                    out var placeholderQuestion))
            {
                parsedQuestions.Add(placeholderQuestion);
            }
            else
            {
                failedBlocks.Add(block);
            }
        }

        if (isToeicListening)
        {
            AddMissingToeicListeningPartOneAndTwoPlaceholders(
                parsedQuestions,
                answerMap,
                expectedQuestionCount);

            failedBlocks = failedBlocks
                .Where(block => block.QuestionNumber > 31)
                .ToList();
        }

        if (failedBlocks.Any())
        {
            throw new Exception(
                "Listening local parser could not parse questions: " +
                string.Join(", ", failedBlocks
                    .Select(block => block.QuestionNumber)
                    .OrderBy(number => number)) +
                ". Required format: numbered questions, A-D options, " +
                "and a complete answer key. AI parsing is temporarily disabled for imports.");
        }
       
        if (parsedQuestions.Count < expectedQuestionCount)
        {
            var missingNumbers = Enumerable
                .Range(1, expectedQuestionCount)
                .Except(parsedQuestions.Select(x => x.QuestionNumber))
                .ToList();

            throw new Exception(
                $"Parser only parsed {parsedQuestions.Count}/{expectedQuestionCount}. Missing questions: {string.Join(",", missingNumbers)}");
        }

        if (isToeicListening)
        {
            parsedQuestions = parsedQuestions
                .Where(x =>
                    x.QuestionNumber >= 1 &&
                    x.QuestionNumber <= expectedQuestionCount)
                .OrderBy(x => x.QuestionNumber)
                .ToList();
        }

        var parsedNumberSet = parsedQuestions
            .Select(x => x.QuestionNumber)
            .ToHashSet();

        var finalMissingNumbers = Enumerable
            .Range(1, expectedQuestionCount)
            .Where(x => !parsedNumberSet.Contains(x))
            .ToList();

        if (finalMissingNumbers.Any())
        {
            throw new Exception(
                "Listening parser missed questions after validation: " +
                string.Join(",", finalMissingNumbers));
        }

        var listeningTasks = new List<ListeningTask>();
        var questionCursor = 0;

        for (var audioIndex = 0; audioIndex < audioFiles.Count; audioIndex++)
        {
            var audioFile =
    audioFiles[audioIndex];

            if (audioFile.Stream == null)
            {
                throw new Exception(
                    $"Audio {audioIndex + 1} has no file content");
            }

            var audioExtension =
                Path.GetExtension(
                    audioFile.FileName)
                .ToLowerInvariant();

            if (audioExtension != ".mp3" &&
                audioExtension != ".wav" &&
                audioExtension != ".m4a")
            {
                throw new Exception(
                    "Only MP3, WAV or M4A audio files are allowed");
            }

            var safeAudioFileName =
                StoragePathHelper.SanitizeFileName(
                    audioFile.FileName);

            var audioObjectName =
                $"{baseFolder}/audio/" +
                $"{Guid.NewGuid():N}_" +
                $"{safeAudioFileName}";

            byte[] audioFileBytes;

            using (var tempAudioStream = new MemoryStream())
            {
                await audioFile.Stream.CopyToAsync(tempAudioStream);
                audioFileBytes = tempAudioStream.ToArray();
            }

            await using var uploadAudioStream =
                new MemoryStream(audioFileBytes);

            var audioUrl = await _minioService.UploadFileAsync(
                uploadAudioStream,
                audioObjectName,
                audioFile.ContentType);

            var countForThisAudio = questionCounts[audioIndex];

            var questionsForThisAudio = parsedQuestions
                .Skip(questionCursor)
                .Take(countForThisAudio)
                .ToList();

            var taskTranscript =
                string.IsNullOrWhiteSpace(transcript)
                    ? null
                    : transcript.Trim();

            if (string.IsNullOrWhiteSpace(taskTranscript) &&
                !isToeicListening)
            {
                await using var transcribeStream =
                    new MemoryStream(audioFileBytes);

                var speechResult =
                    await _speechAssessmentService.AssessAsync(
                        transcribeStream,
                        audioFile.FileName,
                        null);

                taskTranscript =
                    string.IsNullOrWhiteSpace(
                        speechResult.Transcript)
                        ? null
                        : speechResult.Transcript.Trim();
            }

            var firstQuestionNumber =
    questionsForThisAudio.FirstOrDefault()?.QuestionNumber;

            var lastQuestionNumber =
                questionsForThisAudio.LastOrDefault()?.QuestionNumber;

            var matchingPart = listeningParts
                .FirstOrDefault(part =>
                    firstQuestionNumber.HasValue &&
                    lastQuestionNumber.HasValue &&
                    firstQuestionNumber.Value >= part.StartQuestionNumber &&
                    lastQuestionNumber.Value <= part.EndQuestionNumber);
            var matchingLittlePart = listeningLittleParts
    .FirstOrDefault(littlePart =>
        firstQuestionNumber.HasValue &&
        lastQuestionNumber.HasValue &&
        firstQuestionNumber.Value >=
            littlePart.StartQuestionNumber &&
        lastQuestionNumber.Value <=
            littlePart.EndQuestionNumber);
            questionCursor += countForThisAudio;

            var listeningTask = new ListeningTask
            {
                TeacherUserId = teacherUserId,
                Title = $"{title} - Audio {audioIndex + 1}",
                Instruction = description,
                PartTitle = matchingPart?.PartTitle,
                Direction = matchingPart?.Direction,
                LittlePart =
    matchingLittlePart?.LittlePart,
                AudioUrl = audioUrl,
                AudioObjectName = audioObjectName,
                Transcript = taskTranscript,
                CourseType = courseType,
                IsActive = true,
                SourceQuestionFileUrl = questionFileUrl,
                SourceQuestionFileObjectName = questionObjectName,
                Questions = questionsForThisAudio
                    .Select((questionDto, index) =>
                        new ListeningQuestion
                        {
                            Content = questionDto.Content.Trim(),
                            PromptImageUrl =
                                promptImages.TryGetValue(
                                    questionDto.QuestionNumber,
                                    out var promptImage)
                                    ? promptImage.Url
                                    : null,
                            PromptImageObjectName =
                                promptImages.TryGetValue(
                                    questionDto.QuestionNumber,
                                    out promptImage)
                                    ? promptImage.ObjectName
                                    : null,
                            Explanation =
                                string.IsNullOrWhiteSpace(
                                    questionDto.Explanation)
                                    ? null
                                    : questionDto.Explanation.Trim(),
                            OrderIndex = index + 1,
                            Point = 1,
                            Options = questionDto.Options
                                .Where(option =>
                                    !string.IsNullOrWhiteSpace(option.Content))
                                .Select((option, optionIndex) =>
                                    new ListeningOption
                                    {
                                        Content = option.Content.Trim(),
                                        IsCorrect = option.IsCorrect,
                                        OrderIndex = optionIndex + 1
                                    })
                                .ToList()
                        })
                    .ToList()
            };

            var invalidQuestion = listeningTask.Questions
                .FirstOrDefault(q =>
                    q.Options.Count < 2 ||
                    q.Options.Count(o => o.IsCorrect) != 1);

            if (invalidQuestion != null)
            {
                throw new Exception(
                    $"Audio {audioIndex + 1} has invalid question: {invalidQuestion.Content}. Options: {invalidQuestion.Options.Count}, Correct: {invalidQuestion.Options.Count(o => o.IsCorrect)}");
            }

            listeningTasks.Add(listeningTask);
        }

        return listeningTasks;
    }

    private async Task<Dictionary<int, ListeningPromptImageAsset>>
        ExtractToeicListeningPromptImagesAsync(
            byte[] documentBytes,
            string extension,
            int teacherUserId,
            string baseFolder)
    {
        var result =
            new Dictionary<int, ListeningPromptImageAsset>();

        if (string.Equals(
                extension,
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            var pdfImages =
                await _toeicPdfPromptImageExtractorService
                    .ExtractListeningPromptImagesAsync(
                        documentBytes);

            foreach (var item in pdfImages)
            {
                await using var imageStream =
                    new MemoryStream(item.Value.Content);
                var objectName =
                    $"{baseFolder}/images/" +
                    $"{teacherUserId}_{Guid.NewGuid():N}" +
                    $"{item.Value.FileExtension}";
                var imageUrl =
                    await _minioService.UploadFileAsync(
                        imageStream,
                        objectName,
                        item.Value.ContentType);

                result[item.Key] =
                    new ListeningPromptImageAsset(
                        imageUrl,
                        objectName);
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
            LoadToeicListeningImageQuestionMap(
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
                $"{baseFolder}/images/" +
                $"{teacherUserId}_{Guid.NewGuid():N}_" +
                $"{fileName}";
            var imageUrl =
                await _minioService.UploadFileAsync(
                    imageMemoryStream,
                    objectName,
                    GetImageContentType(fileName));
            var asset =
                new ListeningPromptImageAsset(
                    imageUrl,
                    objectName);

            foreach (var questionNumber in
                imageMapping.Value)
            {
                result[questionNumber] = asset;
            }
        }

        return result;
    }

    private static Dictionary<string, string>
        LoadDocxImageRelationshipMap(
            ZipArchiveEntry relationshipEntry)
    {
        using var relationshipStream =
            relationshipEntry.Open();
        var document =
            XDocument.Load(relationshipStream);

        XNamespace relationshipNamespace =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        return document
            .Descendants(
                relationshipNamespace +
                "Relationship")
            .Where(element =>
                ((string?)element.Attribute("Type") ??
                    string.Empty)
                .Contains(
                    "/image",
                    StringComparison.OrdinalIgnoreCase))
            .Select(element => new
            {
                Id = (string?)element.Attribute("Id"),
                Target =
                    (string?)element.Attribute("Target")
            })
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Id) &&
                !string.IsNullOrWhiteSpace(item.Target))
            .ToDictionary(
                item => item.Id!,
                item => NormalizeDocxRelationshipTarget(
                    item.Target!));
    }

    private static Dictionary<string, List<int>>
        LoadToeicListeningImageQuestionMap(
            ZipArchiveEntry documentEntry)
    {
        using var documentStream =
            documentEntry.Open();
        var document = XDocument.Load(documentStream);

        XNamespace wordNamespace =
            "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XNamespace drawingNamespace =
            "http://schemas.openxmlformats.org/drawingml/2006/main";
        XNamespace relationshipNamespace =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var result =
            new Dictionary<string, List<int>>();
        var currentPart = 0;
        var nextPartOneQuestion = 1;
        var questionAnchors =
            new List<DocxListeningQuestionAnchor>();
        var groupedImages =
            new List<DocxListeningImagePosition>();
        var paragraphIndex = 0;

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
                    @"(?i)\b(?:READING|SPEAKING|WRITING)\s+TEST\b") ||
                Regex.IsMatch(
                    text,
                    ListeningAnswerHeadingPattern))
            {
                currentPart = 0;
            }

            var partMatch = Regex.Match(
                text,
                @"(?i)\bPART\s*([1-4])\b");

            if (partMatch.Success)
            {
                currentPart =
                    int.Parse(partMatch.Groups[1].Value);
            }

            var rangeMatch = Regex.Match(
                text,
                @"(?i)\bQuestions?\s*(\d{1,3})\s*[-–]\s*(\d{1,3})\b");

            if (rangeMatch.Success &&
                currentPart is 3 or 4)
            {
                var rangeStart =
                    int.Parse(rangeMatch.Groups[1].Value);
                var rangeEnd =
                    int.Parse(rangeMatch.Groups[2].Value);

                if (IsQuestionRangeInListeningPart(
                        rangeStart,
                        rangeEnd,
                        currentPart))
                {
                    questionAnchors.Add(
                        new DocxListeningQuestionAnchor(
                            paragraphIndex,
                            currentPart,
                            rangeStart,
                            rangeEnd));
                }
            }
            else if (currentPart is 3 or 4)
            {
                var questionMatch = Regex.Match(
                    text,
                    @"^\s*(\d{1,3})\s*[\.\)]");

                if (questionMatch.Success)
                {
                    var questionNumber =
                        int.Parse(
                            questionMatch.Groups[1].Value);
                    var partStart =
                        currentPart == 3 ? 32 : 71;
                    var groupOffset =
                        Math.Max(
                            0,
                            questionNumber - partStart);

                    var groupStart =
                        partStart +
                        (groupOffset / 3 * 3);
                    var groupEnd = Math.Min(
                        currentPart == 3 ? 70 : 100,
                        groupStart + 2);

                    if (IsQuestionRangeInListeningPart(
                            groupStart,
                            groupEnd,
                            currentPart))
                    {
                        questionAnchors.Add(
                            new DocxListeningQuestionAnchor(
                                paragraphIndex,
                                currentPart,
                                groupStart,
                                groupEnd));
                    }
                }
            }

            var imageIds = paragraph
                .Descendants(drawingNamespace + "blip")
                .Select(element =>
                    (string?)element.Attribute(
                        relationshipNamespace + "embed"))
                .Where(id =>
                    !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

            foreach (var imageId in imageIds)
            {
                if (currentPart == 1 &&
                    nextPartOneQuestion <= 6)
                {
                    result[imageId] =
                        new List<int>
                        {
                            nextPartOneQuestion
                        };
                    nextPartOneQuestion++;
                }
                else if (currentPart is 3 or 4)
                {
                    groupedImages.Add(
                        new DocxListeningImagePosition(
                            imageId,
                            paragraphIndex,
                            currentPart));
                }
            }

            paragraphIndex++;
        }

        foreach (var image in groupedImages)
        {
            var nearestAnchor = questionAnchors
                .Where(anchor =>
                    anchor.PartNumber ==
                    image.PartNumber)
                .OrderBy(anchor =>
                    Math.Abs(
                        anchor.ParagraphIndex -
                        image.ParagraphIndex))
                .ThenBy(anchor =>
                    anchor.ParagraphIndex >=
                    image.ParagraphIndex
                        ? 0
                        : 1)
                .FirstOrDefault();

            if (nearestAnchor == null)
            {
                continue;
            }

            result[image.RelationshipId] =
                Enumerable.Range(
                        nearestAnchor.StartQuestionNumber,
                        nearestAnchor.EndQuestionNumber -
                        nearestAnchor.StartQuestionNumber + 1)
                    .ToList();
        }

        return result;
    }

    private static bool IsQuestionRangeInListeningPart(
        int startQuestionNumber,
        int endQuestionNumber,
        int partNumber)
    {
        return partNumber switch
        {
            3 => startQuestionNumber >= 32 &&
                 endQuestionNumber <= 70,
            4 => startQuestionNumber >= 71 &&
                 endQuestionNumber <= 100,
            _ => false
        };
    }

    private static string
        NormalizeDocxRelationshipTarget(
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
        return Path.GetExtension(fileName)
            .ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }

    private List<ListeningLittlePartMetadata>
    ExtractListeningLittleParts(string rawText)
    {
        var result =
            new List<ListeningLittlePartMetadata>();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return result;
        }

        var normalizedText = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\f", "\n");

        var answerMatch = Regex.Match(
            normalizedText,
            ListeningAnswerHeadingPattern);

        var questionText = answerMatch.Success
            ? normalizedText.Substring(
                0,
                answerMatch.Index)
            : normalizedText;

        var matches = Regex.Matches(
                questionText,
                @"(?im)^\s*((?:Conversation|Talk)\s+\d+\s*:\s*Questions?\s+(?<start>\d+)\s*[-–]\s*(?<end>\d+))\s*$")
            .Cast<Match>()
            .OrderBy(x => x.Index)
            .ToList();

        foreach (var match in matches)
        {
            result.Add(
                new ListeningLittlePartMetadata
                {
                    LittlePart =
                        match.Groups[1].Value.Trim(),

                    StartQuestionNumber =
                        int.Parse(
                            match.Groups["start"].Value),

                    EndQuestionNumber =
                        int.Parse(
                            match.Groups["end"].Value)
                });
        }

        return result;
    }
    private List<ListeningPartMetadata> ExtractListeningParts(
        string rawText,
        bool isToeicListening)
    {
        var result = new List<ListeningPartMetadata>();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return result;
        }

        var normalizedText = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\f", "\n");

        var answerMatch = Regex.Match(
            normalizedText,
            ListeningAnswerHeadingPattern);

        var questionText = answerMatch.Success
            ? normalizedText.Substring(0, answerMatch.Index)
            : normalizedText;

        var partPattern = isToeicListening
            ? @"(?im)^\s*(Part\s+(?<partNumber>\d+)(?:\s*:\s*Questions?\s+(?<start>\d+)\s*[-–]\s*(?<end>\d+)|\s*:))\s*$"
            : @"(?im)^\s*(Part\s+(?<partNumber>\d+)\s*:\s*Questions?\s+(?<start>\d+)\s*[-–]\s*(?<end>\d+))\s*$";

        var partMatches = Regex.Matches(
                questionText,
                partPattern)
            .Cast<Match>()
            .OrderBy(x => x.Index)
            .ToList();

        for (var i = 0; i < partMatches.Count; i++)
        {
            var current = partMatches[i];

            var blockStart = current.Index + current.Length;

            var blockEnd = i + 1 < partMatches.Count
                ? partMatches[i + 1].Index
                : questionText.Length;

            var partBody = questionText
                .Substring(blockStart, blockEnd - blockStart)
                .Trim();

            var direction = ExtractPartDirection(partBody);

            var partNumber = int.Parse(
                current.Groups["partNumber"].Value);
            var hasQuestionRange =
                current.Groups["start"].Success &&
                current.Groups["end"].Success;
            var (startQuestionNumber, endQuestionNumber) =
                hasQuestionRange
                    ? (
                        int.Parse(current.Groups["start"].Value),
                        int.Parse(current.Groups["end"].Value))
                    : GetToeicListeningPartQuestionRange(partNumber);

            result.Add(new ListeningPartMetadata
            {
                PartTitle = $"Part {partNumber}",
                PartNumber = partNumber,
                StartQuestionNumber = startQuestionNumber,
                EndQuestionNumber = endQuestionNumber,
                Direction = direction
            });
        }

        return result;
    }

    private static (int StartQuestionNumber, int EndQuestionNumber)
        GetToeicListeningPartQuestionRange(int partNumber)
    {
        return partNumber switch
        {
            1 => (1, 6),
            2 => (7, 31),
            3 => (32, 70),
            4 => (71, 100),
            _ => (0, 0)
        };
    }

    private string? ExtractPartDirection(string partBody)
    {
        if (string.IsNullOrWhiteSpace(partBody))
        {
            return null;
        }

        var directionMatch = Regex.Match(
            partBody,
            @"(?ims)^\s*Directions?\s*:\s*(?<direction>.*?)(?=^\s*(?:Conversation|Talk)\s+\d+\s*:\s*Questions?\s+\d+\s*[-–]\s*\d+\s*$|^\s*\d+\s*[\.\)]\s+|\z)");

        if (!directionMatch.Success)
        {
            return null;
        }

        return CleanListeningText(
            directionMatch.Groups["direction"].Value);
    }

    private string TrimListeningStructuralTail(
    string blockText)
    {
        if (string.IsNullOrWhiteSpace(blockText))
        {
            return blockText;
        }

        var structuralMatch = Regex.Match(
            blockText,
            @"(?im)^\s*(?:" +
            @"Part\s+\d+\s*:\s*Questions?\s+\d+\s*[-–]\s*\d+" +
            @"|" +
            @"Conversation\s+\d+\s*:\s*Questions?\s+\d+\s*[-–]\s*\d+" +
            @"|" +
            @"Talk\s+\d+\s*:\s*Questions?\s+\d+\s*[-–]\s*\d+" +
            @")\s*$");

        if (!structuralMatch.Success)
        {
            return blockText.Trim();
        }

        return blockText
            .Substring(0, structuralMatch.Index)
            .Trim();
    }

    private class ListeningPartMetadata
    {
        public int PartNumber { get; set; }

        public int StartQuestionNumber { get; set; }

        public int EndQuestionNumber { get; set; }

        public string? PartTitle { get; set; }

        public string? Direction { get; set; }
    }
    private class ListeningLittlePartMetadata
    {
        public int StartQuestionNumber { get; set; }

        public int EndQuestionNumber { get; set; }

        public string LittlePart { get; set; } = null!;
    }
    private List<ListeningQuestionBlockDto> SplitListeningQuestionBlocks(
    string rawText,
    Dictionary<int, string> answerMap,
    int expectedQuestionCount,
    bool useToeicListeningRules = false)
    {
        var result = new List<ListeningQuestionBlockDto>();

        var normalizedText = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\f", "\n");

        var answerMatch = System.Text.RegularExpressions.Regex.Match(
            normalizedText,
            ListeningAnswerHeadingPattern);

        var questionText = answerMatch.Success
            ? normalizedText.Substring(0, answerMatch.Index)
            : normalizedText;

        if (!useToeicListeningRules)
        {
            questionText =
                System.Text.RegularExpressions.Regex.Replace(
                    questionText,
                    @"(?<=[A-Za-z\)])(?=\d+\s*[\.\)])",
                    "\n");
        }

        var questionPattern = useToeicListeningRules
            ? @"(?im)(?<!\d)(\d{1,3})\s*[\.\)]\s+(?=[A-Z])"
            : @"(?m)^\s*(\d+)\s*[\.\)]\s+";

        var questionMatches = System.Text.RegularExpressions.Regex.Matches(
                questionText,
                questionPattern)
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(x => new
            {
                Number = int.Parse(x.Groups[1].Value),
                Index = x.Index
            })
            .Where(x =>
                x.Number >= 1 &&
                x.Number <= expectedQuestionCount)
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

            var blockText = questionText
                .Substring(current.Index, endIndex - current.Index)
                .Trim();
            blockText = TrimListeningStructuralTail(blockText);
            result.Add(new ListeningQuestionBlockDto
            {
                QuestionNumber = current.Number,
                RawText = blockText,
                CorrectAnswer = answerMap.TryGetValue(
                    current.Number,
                    out var answer)
                    ? answer
                    : null
            });
        }

        return result;
    }
    private AiParsedListeningQuestionDto? ParseOneListeningQuestionBlockV2(
        int questionNumber,
        string block,
        bool useToeicListeningRules = false)
    {
        block = block
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("", " ")
            .Replace("•", " ")
            .Trim();

        var headerMatch = System.Text.RegularExpressions.Regex.Match(
            block,
            $@"(?is)^\s*{questionNumber}\s*[\.\)]\s*(.+)$");

        if (!headerMatch.Success)
        {
            return null;
        }

        var body = headerMatch.Groups[1].Value.Trim();

        var optionMarkerMatches =
            System.Text.RegularExpressions.Regex.Matches(
                body,
                @"(?im)(?<![A-Z.])([A-D])\.\s*(?=\S)");

        var optionMarkers = optionMarkerMatches
            .Cast<System.Text.RegularExpressions.Match>()
            .Where(x => x.Groups[1].Success)
            .ToList();

        if (optionMarkers.Count < 2)
        {
            return null;
        }

        var firstOption = optionMarkers
            .FirstOrDefault(x =>
                x.Groups[1].Value.Equals(
                    "A",
                    StringComparison.OrdinalIgnoreCase));

        if (firstOption == null)
        {
            return null;
        }

        var content = CleanListeningText(
            body.Substring(0, firstOption.Index));

        if (useToeicListeningRules &&
            questionNumber >= 32)
        {
            content = ExtractToeicListeningQuestionStem(
                content);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var options = new List<AiParsedListeningOptionDto>();

        for (var i = 0; i < optionMarkers.Count; i++)
        {
            var current = optionMarkers[i];

            var nextIndex = i + 1 < optionMarkers.Count
                ? optionMarkers[i + 1].Index
                : body.Length;

            var label = current.Groups[1].Value.Trim().ToUpper();

            if (!new[] { "A", "B", "C", "D" }.Contains(label))
            {
                continue;
            }

            var optionStart = current.Index + current.Length;

            var optionText = body.Substring(
                optionStart,
                nextIndex - optionStart);

            var cleanedOption = CleanListeningText(optionText);

            if (!string.IsNullOrWhiteSpace(cleanedOption))
            {
                options.Add(new AiParsedListeningOptionDto
                {
                    Label = label,
                    Content = cleanedOption,
                    IsCorrect = false
                });
            }
        }

        options = options
            .GroupBy(x => x.Label)
            .Select(x => x.First())
            .OrderBy(x => x.Label)
            .Take(4)
            .ToList();

        if (options.Count < 2)
        {
            return null;
        }

        return new AiParsedListeningQuestionDto
        {
            QuestionNumber = questionNumber,
            Content = content,
            Options = options
        };
    }

    private string ExtractToeicListeningQuestionStem(
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        content = CleanListeningText(content);

        var questionMarkIndex =
            content.LastIndexOf('?');

        if (questionMarkIndex < 0)
        {
            return content;
        }

        var beforeQuestionEnd =
            content.Substring(
                0,
                questionMarkIndex + 1);

        var sentenceStartMatches = Regex.Matches(
                beforeQuestionEnd,
                @"(?:^|[.!?]\s+)(What|Where|When|Why|Who|Whom|Whose|Which|How)\b",
                RegexOptions.IgnoreCase)
            .Cast<Match>()
            .ToList();

        if (sentenceStartMatches.Any())
        {
            var startIndex =
                sentenceStartMatches.Last().Groups[1].Index;

            return beforeQuestionEnd
                .Substring(startIndex)
                .Trim();
        }

        return beforeQuestionEnd.Trim();
    }

    private bool IsValidListeningQuestion(
        AiParsedListeningQuestionDto? question,
        bool useToeicListeningRules = false)
    {
        if (question == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(question.Content))
        {
            return false;
        }

        if (question.Options == null)
        {
            return false;
        }

        var validOptions = question.Options
            .Where(x => !string.IsNullOrWhiteSpace(x.Content))
            .ToList();

        var expectedOptionCount =
            useToeicListeningRules
                ? question.QuestionNumber switch
                {
                    <= 6 => 4,
                    <= 31 => 3,
                    _ => 4
                }
                : 2;

        return validOptions.Count >= expectedOptionCount &&
               validOptions.Count(x => x.IsCorrect) == 1;
    }

    private bool TryCreateToeicListeningPlaceholder(
        int questionNumber,
        IReadOnlyDictionary<int, string> answerMap,
        out AiParsedListeningQuestionDto question)
    {
        question = null!;

        if (questionNumber < 1 ||
            questionNumber > 31)
        {
            return false;
        }

        if (!answerMap.TryGetValue(
                questionNumber,
                out var correctLabel))
        {
            return false;
        }

        var labels = questionNumber <= 6
            ? new[] { "A", "B", "C", "D" }
            : new[] { "A", "B", "C" };

        if (!labels.Contains(correctLabel))
        {
            return false;
        }

        question = new AiParsedListeningQuestionDto
        {
            QuestionNumber = questionNumber,
            Content = questionNumber <= 6
                ? "See the picture and listen to the audio."
                : "Listen to the audio.",
            Options = labels
                .Select(label =>
                    new AiParsedListeningOptionDto
                    {
                        Label = label,
                        Content = label,
                        IsCorrect =
                            string.Equals(
                                label,
                                correctLabel,
                                StringComparison.OrdinalIgnoreCase)
                    })
                .ToList()
        };

        return true;
    }

    private void AddMissingToeicListeningPartOneAndTwoPlaceholders(
        List<AiParsedListeningQuestionDto> parsedQuestions,
        IReadOnlyDictionary<int, string> answerMap,
        int expectedQuestionCount)
    {
        var existingQuestionNumbers = parsedQuestions
            .Select(x => x.QuestionNumber)
            .ToHashSet();

        var lastPlaceholderQuestionNumber =
            Math.Min(31, expectedQuestionCount);

        for (var questionNumber = 1;
             questionNumber <= lastPlaceholderQuestionNumber;
             questionNumber++)
        {
            if (existingQuestionNumbers.Contains(questionNumber))
            {
                continue;
            }

            if (TryCreateToeicListeningPlaceholder(
                    questionNumber,
                    answerMap,
                    out var placeholderQuestion))
            {
                parsedQuestions.Add(placeholderQuestion);
                existingQuestionNumbers.Add(questionNumber);
            }
        }
    }

    private List<List<ListeningQuestionBlockDto>>
        BuildToeicListeningPartThreeAndFourGroups(
            List<ListeningQuestionBlockDto> questionBlocks,
            List<ListeningQuestionBlockDto> failedBlocks)
    {
        var failedGroupKeys = failedBlocks
            .Where(block =>
                block.QuestionNumber >= 32 &&
                block.QuestionNumber <= 100)
            .Select(block =>
                (block.QuestionNumber - 32) / 3)
            .ToHashSet();

        if (!failedGroupKeys.Any())
        {
            return new List<List<ListeningQuestionBlockDto>>();
        }

        return questionBlocks
            .Where(block =>
                block.QuestionNumber >= 32 &&
                block.QuestionNumber <= 100 &&
                failedGroupKeys.Contains(
                    (block.QuestionNumber - 32) / 3))
            .GroupBy(block =>
                (block.QuestionNumber - 32) / 3)
            .OrderBy(group => group.Key)
            .Select(group =>
                group
                    .OrderBy(block => block.QuestionNumber)
                    .ToList())
            .ToList();
    }

    private Dictionary<int, string> ExtractListeningAnswerMap(
        string rawText)
    {
        var result = new Dictionary<int, string>();
        var normalizedText = rawText
    .Replace("\r\n", "\n")
    .Replace("\r", "\n")
    .Replace("\f", "\n");
        

        var answerMatch =
    System.Text.RegularExpressions.Regex.Match(
        normalizedText,
        ListeningAnswerHeadingPattern);

        if (!answerMatch.Success)
        {
            return result;
        }

        var answerText = normalizedText.Substring(answerMatch.Index);

        var matches = System.Text.RegularExpressions.Regex.Matches(
            answerText,
            @"(?im)\b(\d+)\s*[\.\)]\s*([A-D])\b");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var questionNumber = int.Parse(match.Groups[1].Value);
            var correctLabel = match.Groups[2].Value.Trim().ToUpper();

            result[questionNumber] = correctLabel;
        }

        return result;
    }

    private void ApplyCorrectAnswersFromAnswerSection(
        List<AiParsedListeningQuestionDto> questions,
        Dictionary<int, string> answerMap)
    {
        var labels = new[] { "A", "B", "C", "D" };

        foreach (var question in questions)
        {
            for (var i = 0;
                i < question.Options.Count && i < labels.Length;
                i++)
            {
                question.Options[i].Label = labels[i];
                question.Options[i].IsCorrect = false;
            }

            if (!answerMap.TryGetValue(
                question.QuestionNumber,
                out var correctLabel))
            {
                continue;
            }

            var correctIndex = Array.IndexOf(
                labels,
                correctLabel);

            if (correctIndex >= 0 &&
                correctIndex < question.Options.Count)
            {
                question.Options[correctIndex].IsCorrect = true;
            }
        }
    }

    private void ValidateToeicListeningAnswerMap(
        IReadOnlyDictionary<int, string> answerMap,
        int expectedQuestionCount)
    {
        var missingAnswerNumbers = Enumerable
            .Range(1, expectedQuestionCount)
            .Where(questionNumber =>
                !answerMap.ContainsKey(questionNumber))
            .ToList();

        if (missingAnswerNumbers.Any())
        {
            throw new Exception(
                "TOEIC Listening answer key is missing questions: " +
                string.Join(",", missingAnswerNumbers));
        }
    }

    private string CleanListeningText(string value)
    {
        value = value
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ")
            .Replace("", " ")
            .Replace("•", " ");

        while (value.Contains("  "))
        {
            value = value.Replace("  ", " ");
        }

        return value.Trim();
    }

    private const string ListeningAnswerHeadingPattern =
        @"(?im)^\s*(Answer\s*Key|Answers?|Answer\s+for\s+(?:TOEIC\s+)?Listening|(?:TOEIC\s+)?Listening\s+Answers?|Đáp\s*án|Đáp\s*án\s*gợi\s*ý)\s*:?\s*(?:Part\s+\d+\s*:?)?\s*$";

    private sealed record ListeningPromptImageAsset(
        string Url,
        string ObjectName);

    private sealed record DocxListeningQuestionAnchor(
        int ParagraphIndex,
        int PartNumber,
        int StartQuestionNumber,
        int EndQuestionNumber);

    private sealed record DocxListeningImagePosition(
        string RelationshipId,
        int ParagraphIndex,
        int PartNumber);
}
