using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Reading;
using EnglishCenterSystem.Domain.Enums;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;

namespace EnglishCenterSystem.Infrastructure.Services;

public class ToeicReadingImportService
    : IToeicReadingImportService
{
    private const int ToeicReadingQuestionStart = 101;
    private const int ToeicReadingQuestionEnd = 200;

    private readonly IPdfPageRenderService _pdfPageRenderService;
    private readonly IMinioService _minioService;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ToeicReadingImportService(
        IPdfPageRenderService pdfPageRenderService,
        IMinioService minioService,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _pdfPageRenderService = pdfPageRenderService;
        _minioService = minioService;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public Task ValidateReadingAnswerKeyAsync(
        byte[] pdfBytes)
    {
        if (pdfBytes.Length == 0)
        {
            throw new Exception(
                "Preflight failed: TOEIC PDF data is required");
        }

        var pageInfos = ReadPageInfos(pdfBytes);
        var answerMap = ExtractAnswerMapFromText(pageInfos);

        if (answerMap == null)
        {
            throw new Exception(
                "Preflight failed: TOEIC Reading answer key could not be read from PDF text. " +
                "AI image parsing is temporarily disabled for imports.");
        }

        ValidateToeicReadingAnswerMap(answerMap);

        return Task.CompletedTask;
    }

    public async Task<List<ReadingTask>> CreateReadingTasksAsync(
        int teacherUserId,
        string title,
        string? description,
        CourseType courseType,
        byte[] pdfBytes,
        string pdfFileName,
        string storagePrefix)
    {
        if (pdfBytes.Length == 0)
        {
            throw new Exception("TOEIC PDF data is required");
        }

        var pageInfos = ReadPageInfos(pdfBytes);
        var readingPages = DetectImageReadingPages(pageInfos);

        if (!readingPages.Any())
        {
            throw new Exception(
                "TOEIC Reading section could not be parsed from text and no image-only reading pages were found");
        }

        var answerMap = ExtractAnswerMapFromText(pageInfos);

        if (answerMap == null)
        {
            throw new Exception(
                "TOEIC Reading answer key could not be read from PDF text. " +
                "AI image parsing is temporarily disabled for imports.");
        }

        ValidateToeicReadingAnswerMap(answerMap);

        var readingTask = new ReadingTask
        {
            TeacherUserId = teacherUserId,
            Title = $"{title} - TOEIC Reading",
            Instruction = description,
            PassageText =
                "TOEIC Reading section is provided as images from the source PDF.",
            CourseType = courseType,
            IsActive = true,
            Questions = Enumerable
                .Range(
                    ToeicReadingQuestionStart,
                    ToeicReadingQuestionEnd - ToeicReadingQuestionStart + 1)
                .Select((number, index) =>
                    CreateToeicReadingQuestion(
                        number,
                        index + 1,
                        answerMap[number]))
                .ToList()
        };

        await AddReadingPageImagesAsync(
            pdfBytes,
            readingPages,
            readingTask,
            storagePrefix);

        return new List<ReadingTask>
        {
            readingTask
        };
    }

    private static List<PdfPageInfo> ReadPageInfos(
        byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(stream);

        return document
            .GetPages()
            .Select(page =>
            {
                var words = page.GetWords()
                    .Select(word => word.Text)
                    .Where(text =>
                        !string.IsNullOrWhiteSpace(text))
                    .ToList();

                var text = string.Join(" ", words);

                return new PdfPageInfo(
                    page.Number,
                    page.Width,
                    page.Height,
                    text,
                    words.Count);
            })
            .ToList();
    }

    private static List<PdfPageInfo> DetectImageReadingPages(
        IReadOnlyList<PdfPageInfo> pages)
    {
        var answerStartPage =
            pages
                .FirstOrDefault(page =>
                    page.Text.Contains(
                        "Answer for Listening",
                        StringComparison.OrdinalIgnoreCase) ||
                    page.Text.Contains(
                        "Transcript part",
                        StringComparison.OrdinalIgnoreCase))
                ?.PageNumber ?? pages.Count + 1;

        var candidatePages = pages
            .Where(page =>
                page.PageNumber < answerStartPage &&
                page.WordCount <= 2 &&
                page.PageNumber > 1)
            .OrderBy(page => page.PageNumber)
            .ToList();

        var bestRun = new List<PdfPageInfo>();
        var currentRun = new List<PdfPageInfo>();
        var previousPage = 0;

        foreach (var page in candidatePages)
        {
            if (previousPage == 0 ||
                page.PageNumber == previousPage + 1)
            {
                currentRun.Add(page);
            }
            else
            {
                if (currentRun.Count > bestRun.Count)
                {
                    bestRun = currentRun;
                }

                currentRun = new List<PdfPageInfo>
                {
                    page
                };
            }

            previousPage = page.PageNumber;
        }

        if (currentRun.Count > bestRun.Count)
        {
            bestRun = currentRun;
        }

        return bestRun.Count >= 2
            ? bestRun
            : new List<PdfPageInfo>();
    }

    private static Dictionary<int, string>? ExtractAnswerMapFromText(
        IReadOnlyList<PdfPageInfo> pages)
    {
        var readingAnswerStartPage = pages.FirstOrDefault(page =>
            page.Text.Contains(
                "Answer for Reading",
                StringComparison.OrdinalIgnoreCase));

        if (readingAnswerStartPage == null)
        {
            return null;
        }

        var answerPages = pages
            .Where(page =>
                page.PageNumber >=
                readingAnswerStartPage.PageNumber)
            .TakeWhile(page =>
                page.PageNumber ==
                readingAnswerStartPage.PageNumber ||
                !IsOtherAnswerSectionPage(page.Text))
            .ToList();

        var answerText = string.Join(
            "\n",
            answerPages.Select(page => page.Text));

        if (string.IsNullOrWhiteSpace(answerText))
        {
            return null;
        }

        var answerMap = ExtractAnswerPairs(answerText);

        return answerMap.Count > 0
            ? answerMap
            : null;
    }

    private static bool IsOtherAnswerSectionPage(string text)
    {
        return text.Contains(
                   "Answer for Listening",
                   StringComparison.OrdinalIgnoreCase) ||
               text.Contains(
                   "Answer for Speaking",
                   StringComparison.OrdinalIgnoreCase) ||
               text.Contains(
                   "Answer for Writing",
                   StringComparison.OrdinalIgnoreCase) ||
               text.Contains(
                   "Transcript part",
                   StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<int, string>>
        ExtractAnswerMapFromAnswerImagesAsync(
            byte[] pdfBytes,
            IReadOnlyList<PdfPageInfo> pages)
    {
        var imageOnlyAnswerPages = pages
            .Where(page =>
                page.WordCount <= 2 &&
                page.PageNumber >
                pages.Count - 3)
            .OrderBy(page => page.PageNumber)
            .ToList();

        if (!imageOnlyAnswerPages.Any())
        {
            throw new Exception(
                "TOEIC Reading answer key is image-only and no answer image page was found");
        }

        var imageDataUrls = new List<string>();

        foreach (var page in imageOnlyAnswerPages)
        {
            await using var imageStream =
                await _pdfPageRenderService.RenderAndCropPageAsync(
                    pdfBytes,
                    page.PageNumber,
                    page.Width,
                    page.Height,
                    0,
                    0,
                    page.Width,
                    page.Height,
                    dpi: 180);

            imageDataUrls.Add(
                "data:image/webp;base64," +
                Convert.ToBase64String(
                    imageStream.ToArray()));
        }

        var rawResponse =
            await ReadAnswerKeyWithVisionAsync(imageDataUrls);

        var answerMap = ExtractAnswerPairs(rawResponse);

        if (answerMap.Count == 0)
        {
            throw new Exception(
                "TOEIC Reading answer key could not be read from answer image");
        }

        return answerMap;
    }

    private async Task<string> ReadAnswerKeyWithVisionAsync(
        IReadOnlyList<string> imageDataUrls)
    {
        var apiKey = _configuration["Groq:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception(
                "Groq API key is missing for TOEIC Reading image answer import");
        }

        var model =
            _configuration["Groq:VisionModel"] ??
            _configuration["Groq:Model"] ??
            "meta-llama/llama-4-scout-17b-16e-instruct";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.groq.com/openai/v1/chat/completions");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                apiKey);

        var contentItems = new List<object>
        {
            new
            {
                type = "text",
                text =
                    "Read the TOEIC Reading answer key in these images. " +
                    "Return only pairs like 101.A 102.B ... 200.D. " +
                    "Do not explain."
            }
        };

        contentItems.AddRange(
            imageDataUrls.Select(url => new
            {
                type = "image_url",
                image_url = new
                {
                    url
                }
            }));

        var payload = new
        {
            model,
            temperature = 0,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = contentItems
                }
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response =
            await _httpClient.SendAsync(request);

        var responseText =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                "Groq vision could not read TOEIC Reading answer image. " +
                $"Status {(int)response.StatusCode}: {responseText}");
        }

        using var document =
            JsonDocument.Parse(responseText);

        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }

    private static Dictionary<int, string> ExtractAnswerPairs(
        string text)
    {
        return Regex.Matches(
                text,
                @"\b(1\d{2}|200)\s*[\.\):\-]?\s*([A-D])\b",
                RegexOptions.IgnoreCase)
            .Cast<Match>()
            .Select(match => new
            {
                Number = int.Parse(match.Groups[1].Value),
                Answer = match.Groups[2].Value.ToUpperInvariant()
            })
            .Where(x =>
                x.Number >= ToeicReadingQuestionStart &&
                x.Number <= ToeicReadingQuestionEnd)
            .GroupBy(x => x.Number)
            .ToDictionary(
                group => group.Key,
                group => group.First().Answer);
    }

    private static void ValidateToeicReadingAnswerMap(
        IReadOnlyDictionary<int, string> answerMap)
    {
        var missingNumbers = Enumerable
            .Range(
                ToeicReadingQuestionStart,
                ToeicReadingQuestionEnd - ToeicReadingQuestionStart + 1)
            .Where(number =>
                !answerMap.ContainsKey(number))
            .ToList();

        if (missingNumbers.Any())
        {
            throw new Exception(
                "TOEIC Reading answer key is missing questions: " +
                string.Join(", ", missingNumbers));
        }
    }

    private async Task AddReadingPageImagesAsync(
        byte[] pdfBytes,
        IReadOnlyList<PdfPageInfo> readingPages,
        ReadingTask readingTask,
        string storagePrefix)
    {
        var orderIndex = 1;

        foreach (var page in readingPages)
        {
            await using var imageStream =
                await _pdfPageRenderService.RenderAndCropPageAsync(
                    pdfBytes,
                    page.PageNumber,
                    page.Width,
                    page.Height,
                    0,
                    0,
                    page.Width,
                    page.Height,
                    dpi: 180);

            var objectName =
                $"{NormalizeStoragePrefix(storagePrefix)}/" +
                $"toeic-reading-page-{page.PageNumber}-" +
                $"{orderIndex}-{Guid.NewGuid():N}.webp";

            var imageUrl =
                await _minioService.UploadFileAsync(
                    imageStream,
                    objectName,
                    "image/webp");

            readingTask.PassageImages.Add(
                new ReadingPassageImage
                {
                    ImageUrl = imageUrl,
                    ObjectName = objectName,
                    PageNumber = page.PageNumber,
                    OrderIndex = orderIndex
                });

            orderIndex++;
        }
    }

    private static ReadingQuestion CreateToeicReadingQuestion(
        int toeicQuestionNumber,
        int orderIndex,
        string correctAnswer)
    {
        return new ReadingQuestion
        {
            Content =
                $"Question {toeicQuestionNumber} (see TOEIC Reading images)",
            OrderIndex = orderIndex,
            Point = 1,
            Options = new[]
                {
                    "A",
                    "B",
                    "C",
                    "D"
                }
                .Select((label, index) =>
                    new ReadingOption
                    {
                        Content = label,
                        IsCorrect =
                            string.Equals(
                                label,
                                correctAnswer,
                                StringComparison.OrdinalIgnoreCase),
                        OrderIndex = index + 1
                    })
                .ToList()
        };
    }

    private static string NormalizeStoragePrefix(
        string value)
    {
        return value
            .Replace("\\", "/")
            .Trim()
            .Trim('/');
    }

    private sealed record PdfPageInfo(
        int PageNumber,
        double Width,
        double Height,
        string Text,
        int WordCount);
}
