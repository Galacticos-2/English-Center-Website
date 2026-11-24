using EnglishCenterSystem.Application.Interfaces.Services;

using System.Text.RegularExpressions;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace EnglishCenterSystem.Infrastructure.Services;

public class ToeicPdfPromptImageExtractorService
    : IToeicPdfPromptImageExtractorService
{
    private readonly IPdfPageRenderService _pdfPageRenderService;

    public ToeicPdfPromptImageExtractorService(
        IPdfPageRenderService pdfPageRenderService)
    {
        _pdfPageRenderService = pdfPageRenderService;
    }

    public async Task<IReadOnlyDictionary<int, ToeicPdfPromptImage>>
        ExtractWritingPromptImagesAsync(byte[] pdfBytes)
    {
        var result =
            new Dictionary<int, ToeicPdfPromptImage>();
        var nextQuestionNumber = 1;
        var inWritingSection = false;

        using var document = PdfDocument.Open(pdfBytes);

        foreach (var page in document.GetPages())
        {
            var pageText = GetPageText(page);

            if (IsWritingSectionStart(pageText))
            {
                inWritingSection = true;
            }

            if (!inWritingSection ||
                nextQuestionNumber > 5)
            {
                continue;
            }

            foreach (var image in GetPromptImageCandidates(page))
            {
                if (nextQuestionNumber > 5)
                {
                    break;
                }

                result[nextQuestionNumber] =
                    await RenderImageAsync(
                        pdfBytes,
                        page,
                        image);

                nextQuestionNumber++;
            }
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<int, ToeicPdfPromptImage>>
        ExtractListeningPromptImagesAsync(byte[] pdfBytes)
    {
        var result =
            new Dictionary<int, ToeicPdfPromptImage>();
        var currentPart = 0;
        var nextPartOneQuestion = 1;

        using var document = PdfDocument.Open(pdfBytes);

        foreach (var page in document.GetPages())
        {
            var pageText = GetPageText(page);
            var detectedPart = DetectListeningPart(pageText);

            if (detectedPart > 0)
            {
                currentPart = detectedPart;
            }
            else if (IsListeningSectionEnd(pageText))
            {
                currentPart = 0;
                continue;
            }

            if (currentPart == 0)
            {
                continue;
            }

            var images = GetPromptImageCandidates(page);

            if (nextPartOneQuestion <= 6 &&
                currentPart is 1 or 2)
            {
                foreach (var image in images)
                {
                    if (nextPartOneQuestion > 6)
                    {
                        break;
                    }

                    result[nextPartOneQuestion] =
                        await RenderImageAsync(
                            pdfBytes,
                            page,
                            image);

                    nextPartOneQuestion++;
                }

                continue;
            }

            if (currentPart is not (3 or 4) ||
                images.Count == 0)
            {
                continue;
            }

            var questionRanges =
                FindQuestionRanges(
                    page,
                    currentPart);

            foreach (var image in images)
            {
                var questionRange =
                    FindNearestQuestionRange(
                        image,
                        questionRanges);

                if (questionRange == null)
                {
                    continue;
                }

                var renderedImage =
                    await RenderImageAsync(
                        pdfBytes,
                        page,
                        image);

                for (var questionNumber =
                         questionRange.StartQuestionNumber;
                     questionNumber <=
                         questionRange.EndQuestionNumber;
                     questionNumber++)
                {
                    result[questionNumber] = renderedImage;
                }
            }
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<int, ToeicPdfPromptImage>>
        ExtractSpeakingPromptImagesAsync(byte[] pdfBytes)
    {
        var result =
            new Dictionary<int, ToeicPdfPromptImage>();
        var inDescribePictureSection = false;
        var nextQuestionNumber = 3;

        using var document = PdfDocument.Open(pdfBytes);

        foreach (var page in document.GetPages())
        {
            var pageText = GetPageText(page);

            if (IsSpeakingDescribePictureStart(pageText))
            {
                inDescribePictureSection = true;
            }

            if (!inDescribePictureSection ||
                nextQuestionNumber > 4)
            {
                continue;
            }

            foreach (var image in GetPromptImageCandidates(page))
            {
                if (nextQuestionNumber > 4)
                {
                    break;
                }

                result[nextQuestionNumber] =
                    await RenderImageAsync(
                        pdfBytes,
                        page,
                        image);

                nextQuestionNumber++;
            }
        }

        return result;
    }

    private async Task<ToeicPdfPromptImage> RenderImageAsync(
        byte[] pdfBytes,
        Page page,
        IPdfImage image)
    {
        const double padding = 3;

        var left = Math.Max(
            0,
            image.BoundingBox.Left - padding);
        var bottom = Math.Max(
            0,
            image.BoundingBox.Bottom - padding);
        var right = Math.Min(
            page.Width,
            image.BoundingBox.Right + padding);
        var top = Math.Min(
            page.Height,
            image.BoundingBox.Top + padding);

        await using var imageStream =
            await _pdfPageRenderService
                .RenderAndCropPageAsync(
                    pdfBytes,
                    page.Number,
                    page.Width,
                    page.Height,
                    left,
                    bottom,
                    right,
                    top,
                    dpi: 180);

        return new ToeicPdfPromptImage(
            imageStream.ToArray(),
            "image/webp",
            ".webp");
    }

    private static List<IPdfImage>
        GetPromptImageCandidates(Page page)
    {
        var minimumWidth = page.Width * 0.20;
        var minimumHeight = page.Height * 0.08;
        var pageArea = page.Width * page.Height;

        return page
            .GetImages()
            .Where(image =>
            {
                var width = image.BoundingBox.Width;
                var height = image.BoundingBox.Height;
                var areaRatio =
                    width * height / pageArea;

                return width >= minimumWidth &&
                       height >= minimumHeight &&
                       areaRatio >= 0.025 &&
                       areaRatio <= 0.82;
            })
            .OrderByDescending(image => image.BoundingBox.Top)
            .ThenBy(image => image.BoundingBox.Left)
            .ToList();
    }

    private static string GetPageText(Page page)
    {
        return string.Join(
            " ",
            page.GetWords().Select(word => word.Text));
    }

    private static bool IsWritingSectionStart(string pageText)
    {
        return Regex.IsMatch(
                   pageText,
                   @"(?i)\bWRITING\s+TEST\b") ||
               Regex.IsMatch(
                   pageText,
                   @"(?i)\bQuestions?\s*1\s*[-–]\s*5\b.*?\bpicture\b") ||
               Regex.IsMatch(
                   pageText,
                   @"(?i)\bQuestion\s*1\s*:\s*write\s+a\s+sentence\b");
    }

    private static bool IsSpeakingDescribePictureStart(
        string pageText)
    {
        return Regex.IsMatch(
                   pageText,
                   @"(?i)\bQuestions?\s*3\s*[-–—]\s*4\b.*?\bDescribe\s+(?:a\s+)?picture\b") ||
               Regex.IsMatch(
                   pageText,
                   @"(?i)\bQuestion\s*3\s*:\s*Describe\s+(?:a\s+)?picture\b");
    }

    private static bool IsListeningSectionEnd(string pageText)
    {
        return Regex.IsMatch(
                   pageText,
                   @"(?i)\b(?:SPEAKING|WRITING)\s+TEST\b") ||
               Regex.IsMatch(
                   pageText,
                   @"(?i)\bAnswer\s+for\s+(?:TOEIC\s+)?Listening\b");
    }

    private static int DetectListeningPart(string pageText)
    {
        var match = Regex.Match(
            pageText,
            @"(?i)\bPART\s*([1-4])\b");

        return match.Success
            ? int.Parse(match.Groups[1].Value)
            : 0;
    }

    private static List<QuestionRangePosition>
        FindQuestionRanges(
            Page page,
            int listeningPart)
    {
        var result =
            new List<QuestionRangePosition>();

        foreach (var line in BuildTextLines(page))
        {
            var rangeMatch = Regex.Match(
                line.Text,
                @"(?i)\bQuestions?\s*(\d{1,3})\s*[-–]\s*(\d{1,3})\b");

            if (rangeMatch.Success)
            {
                var start =
                    int.Parse(rangeMatch.Groups[1].Value);
                var end =
                    int.Parse(rangeMatch.Groups[2].Value);

                if (IsRangeInListeningPart(
                        start,
                        end,
                        listeningPart))
                {
                    result.Add(
                        new QuestionRangePosition(
                            start,
                            end,
                            line.Bottom,
                            line.Top));
                }

                continue;
            }

            var questionMatch = Regex.Match(
                line.Text,
                @"^\s*(\d{1,3})\s*[\.\)]");

            if (!questionMatch.Success)
            {
                continue;
            }

            var questionNumber =
                int.Parse(questionMatch.Groups[1].Value);
            var partStart =
                listeningPart == 3 ? 32 : 71;
            var groupStart =
                partStart +
                Math.Max(
                    0,
                    questionNumber - partStart) / 3 * 3;
            var groupEnd =
                Math.Min(
                    listeningPart == 3 ? 70 : 100,
                    groupStart + 2);

            if (IsRangeInListeningPart(
                    groupStart,
                    groupEnd,
                    listeningPart))
            {
                result.Add(
                    new QuestionRangePosition(
                        groupStart,
                        groupEnd,
                        line.Bottom,
                        line.Top));
            }
        }

        return result
            .GroupBy(item => new
            {
                item.StartQuestionNumber,
                item.EndQuestionNumber
            })
            .Select(group => group.First())
            .ToList();
    }

    private static QuestionRangePosition?
        FindNearestQuestionRange(
            IPdfImage image,
            IReadOnlyCollection<QuestionRangePosition> ranges)
    {
        if (ranges.Count == 0)
        {
            return null;
        }

        var imageCenter =
            (image.BoundingBox.Bottom +
             image.BoundingBox.Top) / 2;

        return ranges
            .OrderBy(range =>
                Math.Abs(
                    (range.Bottom + range.Top) / 2 -
                    imageCenter))
            .First();
    }

    private static bool IsRangeInListeningPart(
        int start,
        int end,
        int listeningPart)
    {
        return listeningPart switch
        {
            3 => start >= 32 && end <= 70,
            4 => start >= 71 && end <= 100,
            _ => false
        };
    }

    private static List<PdfTextLine> BuildTextLines(Page page)
    {
        const double lineTolerance = 3;
        var lines = new List<List<Word>>();

        foreach (var word in page
            .GetWords()
            .OrderByDescending(word =>
                word.BoundingBox.Bottom)
            .ThenBy(word =>
                word.BoundingBox.Left))
        {
            var line = lines.FirstOrDefault(existing =>
                Math.Abs(
                    existing[0].BoundingBox.Bottom -
                    word.BoundingBox.Bottom) <=
                lineTolerance);

            if (line == null)
            {
                line = new List<Word>();
                lines.Add(line);
            }

            line.Add(word);
        }

        return lines
            .Select(words => new PdfTextLine(
                string.Join(
                    " ",
                    words
                        .OrderBy(word =>
                            word.BoundingBox.Left)
                        .Select(word => word.Text)),
                words.Min(word =>
                    word.BoundingBox.Bottom),
                words.Max(word =>
                    word.BoundingBox.Top)))
            .OrderByDescending(line => line.Top)
            .ToList();
    }

    private sealed record PdfTextLine(
        string Text,
        double Bottom,
        double Top);

    private sealed record QuestionRangePosition(
        int StartQuestionNumber,
        int EndQuestionNumber,
        double Bottom,
        double Top);
}
