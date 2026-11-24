using System.Text.RegularExpressions;

using EnglishCenterSystem.Application
    .Features.Documents.DTOs;

using EnglishCenterSystem.Application
    .Interfaces.Services;

using EnglishCenterSystem.Domain
    .Entities.Reading;

namespace EnglishCenterSystem.Infrastructure.Services;

public class ReadingPassageSnapshotService
    : IReadingPassageSnapshotService
{
    private const double HorizontalMargin = 18;

    private const double TopMargin = 22;

    private const double BottomMargin = 22;

    private const int MinimumAnchorWordCount = 4;

    private const int PreferredAnchorWordCount = 10;

    private readonly
        IExerciseDocumentParserService
        _documentParserService;

    private readonly
        IPdfPageRenderService
        _pdfPageRenderService;

    private readonly
        IMinioService
        _minioService;

    public ReadingPassageSnapshotService(
        IExerciseDocumentParserService
            documentParserService,
        IPdfPageRenderService
            pdfPageRenderService,
        IMinioService
            minioService)
    {
        _documentParserService =
            documentParserService;

        _pdfPageRenderService =
            pdfPageRenderService;

        _minioService =
            minioService;
    }

    public async Task
        GeneratePassageImagesAsync(
            Stream pdfStream,
            string pdfFileName,
            IReadOnlyList<ReadingTask>
                readingTasks,
            string storagePrefix)
    {
        if (pdfStream == null)
        {
            throw new ArgumentNullException(
                nameof(pdfStream));
        }

        if (string.IsNullOrWhiteSpace(
                pdfFileName))
        {
            throw new Exception(
                "PDF file name is required");
        }

        var extension = Path
            .GetExtension(pdfFileName)
            .ToLowerInvariant();

        /*
         * DOCX và TXT vẫn fallback PassageText.
         */
        if (extension != ".pdf")
        {
            return;
        }

        if (readingTasks == null ||
            readingTasks.Count == 0)
        {
            return;
        }

        storagePrefix =
            NormalizeStoragePrefix(
                storagePrefix);

        byte[] pdfBytes;

        using (
            var memoryStream =
                new MemoryStream()
        )
        {
            if (pdfStream.CanSeek)
            {
                pdfStream.Position = 0;
            }

            await pdfStream.CopyToAsync(
                memoryStream);

            pdfBytes =
                memoryStream.ToArray();
        }

        await using var parserStream =
            new MemoryStream(pdfBytes);

        var parsedDocument =
            await _documentParserService
                .ExtractStructuredAsync(
                    parserStream,
                    pdfFileName);

        foreach (
            var taskItem in
            readingTasks
                .Select(
                    (
                        task,
                        index
                    ) =>
                        new
                        {
                            Task = task,
                            PassageIndex =
                                index + 1
                        })
        )
        {
            try
            {
                await GenerateForTaskAsync(
                    pdfBytes,
                    parsedDocument,
                    taskItem.Task,
                    taskItem.PassageIndex,
                    storagePrefix);
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
                    "READING PASSAGE SNAPSHOT ERROR");

                Console.WriteLine(
                    $"Passage index: " +
                    $"{taskItem.PassageIndex}");

                Console.WriteLine(
                    $"Passage title: " +
                    $"{taskItem.Task.Title}");

                Console.WriteLine(
                    exception.ToString());

                Console.WriteLine(
                    "========================================");
            }
        }
    }

    private async Task GenerateForTaskAsync(
        byte[] pdfBytes,
        ParsedDocumentDto parsedDocument,
        ReadingTask readingTask,
        int passageIndex,
        string storagePrefix)
    {
        if (string.IsNullOrWhiteSpace(
                readingTask.PassageText))
        {
            return;
        }

        var passageTokens =
            Tokenize(
                readingTask.PassageText);

        if (passageTokens.Count <
            MinimumAnchorWordCount)
        {
            return;
        }

        var startAnchor =
            passageTokens
                .Take(
                    Math.Min(
                        PreferredAnchorWordCount,
                        passageTokens.Count))
                .ToList();

        var endAnchor =
            passageTokens
                .Skip(
                    Math.Max(
                        0,
                        passageTokens.Count -
                        PreferredAnchorWordCount))
                .ToList();
        Console.WriteLine(
    "----------------------------------------");

        Console.WriteLine(
            $"SNAPSHOT TASK: {readingTask.Title}");

        Console.WriteLine(
            $"START ANCHOR: " +
            $"{string.Join(" ", startAnchor)}");

        Console.WriteLine(
            $"END ANCHOR: " +
            $"{string.Join(" ", endAnchor)}");
        var startMatch =
            FindAnchor(
                parsedDocument.Pages,
                startAnchor,
                searchFromEnd: false);

        var endMatch =
            FindAnchor(
                parsedDocument.Pages,
                endAnchor,
                searchFromEnd: true);
        Console.WriteLine(
    $"START FOUND: {startMatch != null}");

        Console.WriteLine(
            $"END FOUND: {endMatch != null}");

        if (startMatch != null)
        {
            Console.WriteLine(
                $"START PAGE: " +
                $"{startMatch.Page.PageNumber}");
        }

        if (endMatch != null)
        {
            Console.WriteLine(
                $"END PAGE: " +
                $"{endMatch.Page.PageNumber}");
        }
        if (startMatch == null)
        {
            throw new Exception(
                "Could not find passage start in PDF");
        }

        if (endMatch == null)
        {
            throw new Exception(
                "Could not find passage end in PDF");
        }

        if (endMatch.Page.PageNumber <
            startMatch.Page.PageNumber)
        {
            throw new Exception(
                "Passage end was found before passage start");
        }

        var passagePages =
            parsedDocument.Pages
                .Where(page =>
                    page.PageNumber >=
                        startMatch.Page.PageNumber &&
                    page.PageNumber <=
                        endMatch.Page.PageNumber)
                .OrderBy(page =>
                    page.PageNumber)
                .ToList();

        if (passagePages.Count == 0)
        {
            return;
        }

        readingTask.PassageImages.Clear();

        var imageOrderIndex = 1;

        foreach (var page in passagePages)
        {
            var cropBounds =
                CalculateCropBounds(
                    page,
                    startMatch,
                    endMatch);

            if (cropBounds == null)
            {
                continue;
            }

            await using var imageStream =
                await _pdfPageRenderService
                    .RenderAndCropPageAsync(
                        pdfBytes,
                        page.PageNumber,
                        page.Width,
                        page.Height,
                        cropBounds.Left,
                        cropBounds.Bottom,
                        cropBounds.Right,
                        cropBounds.Top,
                        dpi: 180);

            var objectName =
                BuildObjectName(
                    storagePrefix,
                    passageIndex,
                    page.PageNumber,
                    imageOrderIndex);

            var imageUrl =
                await _minioService
                    .UploadFileAsync(
                        imageStream,
                        objectName,
                        "image/webp");
            Console.WriteLine(
    $"PASSAGE IMAGE CREATED: " +
    $"{objectName}");
            readingTask.PassageImages.Add(
                new ReadingPassageImage
                {
                    ImageUrl =
                        imageUrl,

                    ObjectName =
                        objectName,

                    PageNumber =
                        page.PageNumber,

                    OrderIndex =
                        imageOrderIndex
                });

            imageOrderIndex++;
        }

        if (readingTask.PassageImages.Count == 0)
        {
            throw new Exception(
                "No passage image was generated");
        }
    }

    private static CropBounds?
        CalculateCropBounds(
            ParsedDocumentPageDto page,
            AnchorMatch startMatch,
            AnchorMatch endMatch)
    {
        if (page.Words.Count == 0)
        {
            return null;
        }

        var pageWords =
            page.Words
                .Where(word =>
                    !string.IsNullOrWhiteSpace(
                        word.Text))
                .ToList();

        if (pageWords.Count == 0)
        {
            return null;
        }

        var left =
            Math.Max(
                0,
                pageWords.Min(word =>
                    word.Left) -
                HorizontalMargin);

        var right =
            Math.Min(
                page.Width,
                pageWords.Max(word =>
                    word.Right) +
                HorizontalMargin);

        double top;

        double bottom;

        var isStartPage =
            page.PageNumber ==
            startMatch.Page.PageNumber;

        var isEndPage =
            page.PageNumber ==
            endMatch.Page.PageNumber;

        if (isStartPage)
        {
            top = Math.Min(
                page.Height,
                startMatch.MatchedWords
                    .Max(word => word.Top) +
                TopMargin);
        }
        else
        {
            top = Math.Min(
                page.Height,
                pageWords.Max(word =>
                    word.Top) +
                TopMargin);
        }

        if (isEndPage)
        {
            bottom = Math.Max(
                0,
                endMatch.MatchedWords
                    .Min(word => word.Bottom) -
                BottomMargin);
        }
        else
        {
            bottom = Math.Max(
                0,
                pageWords.Min(word =>
                    word.Bottom) -
                BottomMargin);
        }

        if (right <= left ||
            top <= bottom)
        {
            return null;
        }

        return new CropBounds
        {
            Left = left,
            Bottom = bottom,
            Right = right,
            Top = top
        };
    }

    private static AnchorMatch?
        FindAnchor(
            IReadOnlyList<
                ParsedDocumentPageDto> pages,
            IReadOnlyList<string> anchorTokens,
            bool searchFromEnd)
    {
        var orderedPages =
            searchFromEnd
                ? pages
                    .OrderByDescending(page =>
                        page.PageNumber)
                    .ToList()
                : pages
                    .OrderBy(page =>
                        page.PageNumber)
                    .ToList();

        var maximumTokenCount =
            Math.Min(
                PreferredAnchorWordCount,
                anchorTokens.Count);

        for (
            var tokenCount =
                maximumTokenCount;
            tokenCount >=
                MinimumAnchorWordCount;
            tokenCount--
        )
        {
            var currentAnchor =
                searchFromEnd
                    ? anchorTokens
                        .Skip(
                            Math.Max(
                                0,
                                anchorTokens.Count -
                                tokenCount))
                        .Take(tokenCount)
                        .ToList()
                    : anchorTokens
                        .Take(tokenCount)
                        .ToList();

            foreach (var page in orderedPages)
            {
                var pageTokenWords =
                    BuildPageTokenWords(page);

                var match =
                    FindTokenSequence(
                        pageTokenWords,
                        currentAnchor,
                        searchFromEnd);

                if (match != null)
                {
                    return new AnchorMatch
                    {
                        Page = page,

                        MatchedWords =
                            match
                    };
                }
            }
        }

        return null;
    }

    private static List<
        ParsedDocumentWordDto>?
        FindTokenSequence(
            IReadOnlyList<TokenWord>
                pageTokenWords,
            IReadOnlyList<string>
                anchorTokens,
            bool searchFromEnd)
    {
        if (anchorTokens.Count == 0 ||
            pageTokenWords.Count <
                anchorTokens.Count)
        {
            return null;
        }

        var startIndexes =
            Enumerable.Range(
                0,
                pageTokenWords.Count -
                anchorTokens.Count +
                1);

        if (searchFromEnd)
        {
            startIndexes =
                startIndexes.Reverse();
        }

        foreach (var startIndex in
            startIndexes)
        {
            var matched = true;

            for (
                var offset = 0;
                offset <
                    anchorTokens.Count;
                offset++
            )
            {
                if (!string.Equals(
                        pageTokenWords[
                            startIndex + offset]
                            .Token,
                        anchorTokens[offset],
                        StringComparison
                            .Ordinal))
                {
                    matched = false;
                    break;
                }
            }

            if (!matched)
            {
                continue;
            }

            return pageTokenWords
                .Skip(startIndex)
                .Take(anchorTokens.Count)
                .Select(item =>
                    item.Word)
                .Distinct()
                .ToList();
        }

        return null;
    }

    private static List<TokenWord>
    BuildPageTokenWords(
        ParsedDocumentPageDto page)
    {
        var result =
            new List<TokenWord>();

        if (page.Words == null ||
            page.Words.Count == 0)
        {
            return result;
        }

        const double lineTolerance = 3;

        var sourceWords = page.Words
            .Where(word =>
                !string.IsNullOrWhiteSpace(
                    word.Text))
            .OrderByDescending(word =>
                word.Bottom)
            .ThenBy(word =>
                word.Left)
            .ToList();

        var lines =
            new List<List<ParsedDocumentWordDto>>();

        foreach (var word in sourceWords)
        {
            var matchingLine =
                lines.FirstOrDefault(line =>
                    line.Count > 0 &&
                    Math.Abs(
                        line.Average(item =>
                            item.Bottom) -
                        word.Bottom)
                    <= lineTolerance);

            if (matchingLine == null)
            {
                matchingLine =
                    new List<
                        ParsedDocumentWordDto>();

                lines.Add(matchingLine);
            }

            matchingLine.Add(word);
        }

        var orderedLines = lines
            .OrderByDescending(line =>
                line.Average(word =>
                    word.Bottom))
            .ToList();

        foreach (var line in orderedLines)
        {
            foreach (
                var word in line
                    .OrderBy(word =>
                        word.Left)
            )
            {
                var tokens =
                    Tokenize(word.Text);

                foreach (var token in tokens)
                {
                    result.Add(
                        new TokenWord
                        {
                            Token = token,
                            Word = word
                        });
                }
            }
        }

        return result;
    }

    private static List<string>
        Tokenize(
            string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return new List<string>();
        }

        return Regex
            .Matches(
                value
                    .ToLowerInvariant()
                    .Replace('\u00A0', ' '),
                @"[\p{L}\p{N}]+")
            .Select(match =>
                match.Value)
            .Where(token =>
                !string.IsNullOrWhiteSpace(
                    token))
            .ToList();
    }

    private static string
        NormalizeStoragePrefix(
            string storagePrefix)
    {
        if (string.IsNullOrWhiteSpace(
                storagePrefix))
        {
            throw new Exception(
                "Reading passage storage prefix is required");
        }

        return storagePrefix
            .Replace("\\", "/")
            .Trim('/');
    }

    private static string BuildObjectName(
        string storagePrefix,
        int passageIndex,
        int pageNumber,
        int imageOrderIndex)
    {
        return
            $"{storagePrefix}/" +
            $"passage-{passageIndex}/" +
            $"page-{pageNumber}-" +
            $"{imageOrderIndex}-" +
            $"{Guid.NewGuid():N}.webp";
    }

    private sealed class TokenWord
    {
        public string Token { get; set; }
            = string.Empty;

        public ParsedDocumentWordDto Word
        {
            get;
            set;
        } = null!;
    }

    private sealed class AnchorMatch
    {
        public ParsedDocumentPageDto Page
        {
            get;
            set;
        } = null!;

        public List<ParsedDocumentWordDto>
            MatchedWords
        { get; set; }
                = new();
    }

    private sealed class CropBounds
    {
        public double Left { get; set; }

        public double Bottom { get; set; }

        public double Right { get; set; }

        public double Top { get; set; }
    }
}