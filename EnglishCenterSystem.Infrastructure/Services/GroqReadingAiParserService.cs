using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishCenterSystem.Application.Features.Reading.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace EnglishCenterSystem.Infrastructure.Services;

public class GroqReadingAiParserService : IReadingAiParserService
{
    private const string ReadingAnswerHeadingPattern =
        @"(?:Answer\s+for\s+Reading|Answer\s*Key(?:\s+Reading)?|Answers?(?:\s+Reading)?|Đáp\s*án(?:\s+gợi\s*ý)?(?:\s+Reading)?)";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqReadingAiParserService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<AiParsedReadingExerciseDto> ParseReadingAsync(
        string rawText)
    {
        var documentAnswerMap =
            ExtractReadingAnswerMap(rawText);
        var chunks = SplitIntoReadingChunks(rawText);

        var finalResult = new AiParsedReadingExerciseDto();

        foreach (var chunk in chunks)
        {
            var chunkResult =
                TryParseChunkLocally(
                    chunk.Text,
                    chunk.PassageNumber) ??
                await ParseChunkAsync(
                    chunk.Text,
                    chunk.PassageNumber);

            finalResult.Passages.AddRange(chunkResult.Passages);
        }

        finalResult.Passages = finalResult.Passages
    .Where(x =>
        !string.IsNullOrWhiteSpace(x.PassageText) &&
        x.Questions.Any())
    .OrderBy(x => x.PassageNumber)
    .ToList();

        ApplyCorrectAnswersFromAnswerSection(
            finalResult,
            documentAnswerMap);

        if (!finalResult.Passages.Any())
        {
            throw new Exception("AI reading parser returned no passages");
        }

        return finalResult;
    }

    private static AiParsedReadingExerciseDto? TryParseChunkLocally(
        string chunkText,
        int passageNumber)
    {
        var answerMap = ExtractReadingAnswerMap(chunkText);
        var expectedNumbers = ExtractExpectedQuestionNumbers(chunkText);

        if (!expectedNumbers.Any() ||
            answerMap.Count == 0)
        {
            return null;
        }

        var questions = expectedNumbers
            .Select(number =>
                ExtractQuestionByNumber(
                    chunkText,
                    number,
                    answerMap))
            .Where(x => x != null)
            .Cast<AiParsedReadingQuestionDto>()
            .OrderBy(x => x.QuestionNumber)
            .ToList();

        var complete =
            questions.Count == expectedNumbers.Count &&
            questions.All(question =>
                question.Options.Count >= 2 &&
                question.Options.Count(option => option.IsCorrect) == 1);

        if (!complete)
        {
            return null;
        }

        var passageText = ExtractPassageTextForLocalParse(chunkText);

        if (string.IsNullOrWhiteSpace(passageText))
        {
            return null;
        }

        return new AiParsedReadingExerciseDto
        {
            Passages = new List<AiParsedReadingTaskDto>
            {
                new()
                {
                    PassageNumber = passageNumber,
                    Title = $"Passage {passageNumber}",
                    PassageText = passageText,
                    Questions = questions
                }
            }
        };
    }

    private static string ExtractPassageTextForLocalParse(string chunkText)
    {
        var normalizedText = chunkText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();

        var answerStart = GetAnswerStartIndex(normalizedText);

        if (answerStart < normalizedText.Length)
        {
            normalizedText = normalizedText[..answerStart].Trim();
        }

        var firstQuestionMatch = Regex.Match(
            normalizedText,
            @"(?m)^\s*\d+\s*[\.\)]\s+");

        var passageText = firstQuestionMatch.Success
            ? normalizedText[..firstQuestionMatch.Index]
            : normalizedText;

        passageText = Regex.Replace(
                passageText,
                @"(?im)^\s*(?:\d+(?:\.\d+)*\.\s*)?(?:reading\s+)?passage\s+\d+\s*:\s*questions\s+\d+\s*[-–—]\s*\d+.*$",
                "")
            .Trim();

        return passageText;
    }

    private async Task<AiParsedReadingExerciseDto> ParseChunkAsync(
        string chunkText,
        int passageNumber)
    {
        var apiKey = _configuration["Groq:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("Groq API key is missing");
        }

        var model =
            _configuration["Groq:Model"] ??
            "llama-3.3-70b-versatile";

        var prompt = BuildPrompt(
            chunkText,
            passageNumber);

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "You are an English reading test parser. Return only valid JSON. Do not include markdown."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.1,
            response_format = new
            {
                type = "json_object"
            }
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.groq.com/openai/v1/chat/completions");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                apiKey);

       

        var requestJson = JsonSerializer.Serialize(requestBody);

        request.Content = new StringContent(
            requestJson,
            Encoding.UTF8,
            "application/json");

        var response = await SendGroqRequestWithRetryAsync(
            request,
            requestJson);

        var responseText =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(responseText);
        }

        using var document = JsonDocument.Parse(responseText);

        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new Exception("AI reading parser returned empty response");
        }

        var json = CleanJson(content);

        try
        {
            var result =
                JsonSerializer.Deserialize<AiParsedReadingExerciseDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null)
            {
                throw new Exception("AI reading parser returned null result");
            }

            NormalizeAiResult(
    result,
    passageNumber);

            var answerMap =
                ExtractReadingAnswerMap(
                    chunkText);

            ApplyCorrectAnswersFromAnswerSection(
                result,
                answerMap);

            RepairMissingQuestionsFromChunk(
                result,
                chunkText,
                answerMap);

            ApplyCorrectAnswersFromAnswerSection(
                result,
                answerMap);

            foreach (var passage in result.Passages)
            {
                passage.PassageNumber =
                    passageNumber;
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "AI returned invalid reading JSON. Raw content: " +
                json +
                " | Error: " +
                ex.Message);
        }
    }
    private static void RepairMissingQuestionsFromChunk(
    AiParsedReadingExerciseDto result,
    string chunkText,
    Dictionary<int, string> answerMap)
    {
        var expectedNumbers =
            ExtractExpectedQuestionNumbers(chunkText);

        if (!expectedNumbers.Any())
        {
            return;
        }

        var passage = result.Passages.FirstOrDefault();

        if (passage == null)
        {
            return;
        }

        passage.Questions ??=
            new List<AiParsedReadingQuestionDto>();

        var existingNumbers = passage.Questions
            .Select(x => x.QuestionNumber)
            .ToHashSet();

        foreach (var questionNumber in expectedNumbers)
        {
            if (existingNumbers.Contains(questionNumber))
            {
                continue;
            }

            var fallbackQuestion =
                ExtractQuestionByNumber(
                    chunkText,
                    questionNumber,
                    answerMap);

            if (fallbackQuestion == null)
            {
                continue;
            }

            passage.Questions.Add(
                fallbackQuestion);

            existingNumbers.Add(
                questionNumber);
        }

        passage.Questions = passage.Questions
            .Where(x => x != null)
            .GroupBy(x => x.QuestionNumber)
            .Select(group =>
                group
                    .OrderByDescending(x =>
                        x.Options?.Count ?? 0)
                    .First())
            .OrderBy(x => x.QuestionNumber)
            .ToList();
    }

    private static List<int> ExtractExpectedQuestionNumbers(string text)
    {
        var rangeMatch = Regex.Match(
            text,
            @"(?im)\bquestions?\s+(\d+)\s*[-–—]\s*(\d+)\b");

        if (rangeMatch.Success &&
            int.TryParse(
                rangeMatch.Groups[1].Value,
                out var start) &&
            int.TryParse(
                rangeMatch.Groups[2].Value,
                out var end) &&
            start > 0 &&
            end >= start)
        {
            return Enumerable
                .Range(start, end - start + 1)
                .ToList();
        }

        var answerMap =
            ExtractReadingAnswerMap(text);

        if (answerMap.Count > 0)
        {
            return answerMap.Keys
                .OrderBy(x => x)
                .ToList();
        }

        return new List<int>();
    }

    private static AiParsedReadingQuestionDto? ExtractQuestionByNumber(
    string text,
    int questionNumber,
    Dictionary<int, string> answerMap)
    {
        var pattern =
            $@"(?ims)" +
            $@"^\s*{questionNumber}\s*[\.\)]\s*" +
            @"(?<content>.*?)" +
            @"(?=^\s*A\s*[\.\)]\s*)" +

            @"^\s*A\s*[\.\)]\s*" +
            @"(?<a>.*?)" +
            @"(?=^\s*B\s*[\.\)]\s*)" +

            @"^\s*B\s*[\.\)]\s*" +
            @"(?<b>.*?)" +
            @"(?=^\s*C\s*[\.\)]\s*)" +

            @"^\s*C\s*[\.\)]\s*" +
            @"(?<c>.*?)" +
            @"(?=^\s*D\s*[\.\)]\s*)" +

            @"^\s*D\s*[\.\)]\s*" +
            @"(?<d>.*?)" +
            @"(?=" +
                @"^\s*\d+\s*[\.\)]\s*" +
                @"|^\s*Passage\s+\d+" +
                @"|^\s*Writing\b" +
                @"|^\s*Speaking\b" +
                $@"|^\s*{ReadingAnswerHeadingPattern}\b" +
                @"|\z" +
            @")";

        var match = Regex.Match(
            text,
            pattern,
            RegexOptions.IgnoreCase |
            RegexOptions.Multiline |
            RegexOptions.Singleline);

        if (!match.Success)
        {
            match = MatchInlineQuestionByNumber(
                text,
                questionNumber);
        }

        if (!match.Success)
        {
            return null;
        }

        var content = CleanText(
            match.Groups["content"].Value);

        var optionA = CleanText(
            match.Groups["a"].Value);

        var optionB = CleanText(
            match.Groups["b"].Value);

        var optionC = CleanText(
            match.Groups["c"].Value);

        var optionD = CleanText(
            match.Groups["d"].Value);

        if (string.IsNullOrWhiteSpace(content) ||
            string.IsNullOrWhiteSpace(optionA) ||
            string.IsNullOrWhiteSpace(optionB) ||
            string.IsNullOrWhiteSpace(optionC) ||
            string.IsNullOrWhiteSpace(optionD))
        {
            return null;
        }

        var correctLabel =
            answerMap.TryGetValue(
                questionNumber,
                out var label)
                ? label.Trim().ToUpperInvariant()
                : "";

        return new AiParsedReadingQuestionDto
        {
            QuestionNumber = questionNumber,
            Content = content,
          
            Options = new List<AiParsedReadingOptionDto>
        {
            new()
            {
                Label = "A",
                Content = optionA,
                IsCorrect = correctLabel == "A"
            },
            new()
            {
                Label = "B",
                Content = optionB,
                IsCorrect = correctLabel == "B"
            },
            new()
            {
                Label = "C",
                Content = optionC,
                IsCorrect = correctLabel == "C"
            },
            new()
            {
                Label = "D",
                Content = optionD,
                IsCorrect = correctLabel == "D"
            }
        }
        };
    }

    private static Match MatchInlineQuestionByNumber(
        string text,
        int questionNumber)
    {
        var marker =
            @"(?=\s(?:A|B|C|D)\s*[\.\)]?\s+)";

        var pattern =
            $@"(?is)(?:^|[\r\n])\s*{questionNumber}\s*[\.\)]\s*" +
            $@"(?<content>.*?){marker}" +
            @"\s*A\s*[\.\)]?\s+(?<a>.*?)" +
            @"(?=\s*B\s*[\.\)]?\s+)" +
            @"\s*B\s*[\.\)]?\s+(?<b>.*?)" +
            @"(?=\s*C\s*[\.\)]?\s+)" +
            @"\s*C\s*[\.\)]?\s+(?<c>.*?)" +
            @"(?=\s*D\s*[\.\)]?\s+)" +
            @"\s*D\s*[\.\)]?\s+(?<d>.*?)" +
            @"(?=" +
                @"(?:[\r\n]\s*\d+\s*[\.\)]\s*)" +
                @"|(?:[\r\n]\s*Passage\s+\d+)" +
                @"|(?:[\r\n]\s*Writing\b)" +
                @"|(?:[\r\n]\s*Speaking\b)" +
                $@"|(?:[\r\n]\s*{ReadingAnswerHeadingPattern}\b)" +
                @"|\z" +
            @")";

        return Regex.Match(
            text,
            pattern,
            RegexOptions.IgnoreCase |
            RegexOptions.Singleline);
    }

    private static string CleanText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        return Regex.Replace(
                value
                    .Replace("\u00A0", " ")
                    .Replace("\u200B", "")
                    .Replace("\uFEFF", ""),
                @"\s+",
                " ")
            .Trim();
    }
    private static Dictionary<int, string> ExtractReadingAnswerMap(
    string chunkText)
    {
        var result = new Dictionary<int, string>();

        var answerSection = ExtractAnswerSection(chunkText);

        if (string.IsNullOrWhiteSpace(answerSection))
        {
            answerSection = chunkText;
        }

        var matches = Regex.Matches(
            answerSection,
            @"(?im)\b(\d+)\s*[\.\)]\s*([A-D])\b");

        foreach (Match match in matches)
        {
            var questionNumber = int.Parse(match.Groups[1].Value);

            var correctLabel = match.Groups[2]
                .Value
                .Trim()
                .ToUpper();

            result[questionNumber] = correctLabel;
        }

        return result;
    }
    private static void ApplyCorrectAnswersFromAnswerSection(
    AiParsedReadingExerciseDto result,
    Dictionary<int, string> answerMap)
    {
        var labels = new[] { "A", "B", "C", "D" };

        foreach (var passage in result.Passages)
        {
            foreach (var question in passage.Questions)
            {
                for (var i = 0;
                     i < question.Options.Count &&
                     i < labels.Length;
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
    }
    private static List<ReadingChunk> SplitIntoReadingChunks(
    string rawText)
    {
        var normalizedText = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();

        var answerText = ExtractAnswerSection(normalizedText);

        var passageMatches = Regex.Matches(
            normalizedText,
            @"(?im)^\s*(?:\d+(?:\.\d+)*\.\s*)?(?:reading\s+)?passage\s+(\d+)\s*:\s*questions\s+(\d+)\s*-\s*(\d+).*$");

        if (passageMatches.Count >= 1)
        {
            var chunks = new List<ReadingChunk>();

            var introText = normalizedText
                .Substring(0, passageMatches[0].Index)
                .Trim();

            for (var i = 0; i < passageMatches.Count; i++)
            {
                var current = passageMatches[i];

                var start = current.Index;

                var end = i + 1 < passageMatches.Count
                    ? passageMatches[i + 1].Index
                    : GetAnswerStartIndex(normalizedText);

                if (end <= start)
                {
                    end = normalizedText.Length;
                }

                var passageNumber =
                    int.Parse(current.Groups[1].Value);

                var startQuestionNumber =
                    int.Parse(current.Groups[2].Value);

                var endQuestionNumber =
                    int.Parse(current.Groups[3].Value);

                var text = normalizedText
                    .Substring(start, end - start)
                    .Trim();
                if (i == 0 &&
    !string.IsNullOrWhiteSpace(introText))
                {
                    text =
                        "General Directions:\n" +
                        introText +
                        "\n\n" +
                        text;
                }
                var answerForThisPassage =
                    ExtractAnswerLinesForRange(
                        answerText,
                        startQuestionNumber,
                        endQuestionNumber);

                if (!string.IsNullOrWhiteSpace(answerForThisPassage))
                {
                    text += "\n\nAnswer:\n" + answerForThisPassage;
                }

                chunks.Add(new ReadingChunk
                {
                    PassageNumber = passageNumber,
                    Text = text
                });
            }

            return chunks;
        }

        var toeicChunks = TrySplitToeicReadingChunks(
            normalizedText,
            answerText);

        if (toeicChunks.Any())
        {
            return toeicChunks;
        }

        var answerStartIndex =
            GetAnswerStartIndex(normalizedText);
        var questionText = normalizedText
            .Substring(0, answerStartIndex)
            .Trim();

        return SplitByLength(questionText, 7000);
    }

    private static List<ReadingChunk> TrySplitToeicReadingChunks(
        string normalizedText,
        string answerText)
    {
        if (!Regex.IsMatch(
                normalizedText,
                @"(?im)^\s*PART\s+[567]\b"))
        {
            return new List<ReadingChunk>();
        }

        var answerStartIndex = GetAnswerStartIndex(normalizedText);
        var questionText = normalizedText[..answerStartIndex].Trim();
        var rangeMatches = Regex.Matches(
                questionText,
                @"(?im)^\s*Questions?\s+(\d{1,3})\s*[-–—]\s*(\d{1,3})\s+refer\b.*$")
            .Cast<Match>()
            .ToList();
        var chunks = new List<ReadingChunk>();
        var passageNumber = 1;
        var firstRangeIndex = rangeMatches.Any()
            ? rangeMatches[0].Index
            : questionText.Length;
        var partFiveText = questionText[..firstRangeIndex].Trim();
        var partFiveMatches = Regex.Matches(
                partFiveText,
                @"(?m)^\s*(\d{1,3})\s*[\.\)]\s+")
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

        for (var startIndex = 0;
             startIndex < partFiveMatches.Count;
             startIndex += 5)
        {
            var group = partFiveMatches
                .Skip(startIndex)
                .Take(5)
                .ToList();
            var blockStart = group[0].Match.Index;
            var blockEndIndex = startIndex + group.Count;
            var blockEnd = blockEndIndex < partFiveMatches.Count
                ? partFiveMatches[blockEndIndex].Match.Index
                : partFiveText.Length;
            var text = partFiveText
                .Substring(blockStart, blockEnd - blockStart)
                .Trim();
            var answers = ExtractAnswerLinesForRange(
                answerText,
                group.First().Number,
                group.Last().Number);

            if (!string.IsNullOrWhiteSpace(answers))
            {
                text += "\n\nAnswer:\n" + answers;
            }

            chunks.Add(new ReadingChunk
            {
                PassageNumber = passageNumber++,
                Text = text
            });
        }

        for (var index = 0; index < rangeMatches.Count; index++)
        {
            var current = rangeMatches[index];
            var blockEnd = index + 1 < rangeMatches.Count
                ? rangeMatches[index + 1].Index
                : questionText.Length;
            var startQuestionNumber =
                int.Parse(current.Groups[1].Value);
            var endQuestionNumber =
                int.Parse(current.Groups[2].Value);
            var text = questionText
                .Substring(current.Index, blockEnd - current.Index)
                .Trim();
            var answers = ExtractAnswerLinesForRange(
                answerText,
                startQuestionNumber,
                endQuestionNumber);

            if (!string.IsNullOrWhiteSpace(answers))
            {
                text += "\n\nAnswer:\n" + answers;
            }

            chunks.Add(new ReadingChunk
            {
                PassageNumber = passageNumber++,
                Text = text
            });
        }

        return chunks;
    }
    private static int GetAnswerStartIndex(string text)
    {
        var match = Regex.Match(
            text,
            $@"(?im)^\s*{ReadingAnswerHeadingPattern}\s*:?\s*$");

        return match.Success
            ? match.Index
            : text.Length;
    }

    private static string ExtractAnswerSection(string text)
    {
        var match = Regex.Match(
            text,
            $@"(?ims)^\s*{ReadingAnswerHeadingPattern}\s*:?\s*(?<content>.*)$");

        return match.Success
            ? match.Groups["content"].Value.Trim()
            : "";
    }
    private async Task<HttpResponseMessage> SendGroqRequestWithRetryAsync(
    HttpRequestMessage request,
    string requestJson)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(90));

                Console.WriteLine($"Groq attempt {attempt}");

                var response = await _httpClient.SendAsync(
                    request,
                    cts.Token);

                Console.WriteLine(
                    $"Groq status {(int)response.StatusCode}");

                if ((int)response.StatusCode != 429)
                {
                    return response;
                }

                var responseText =
                    await response.Content.ReadAsStringAsync();
                if (
    responseText.Contains("tokens per day", StringComparison.OrdinalIgnoreCase) ||
    responseText.Contains("TPD", StringComparison.OrdinalIgnoreCase)
)
                {
                    throw new Exception(
                        "Groq daily token limit exceeded. Please try again later. " +
                        responseText);
                } 

                if (attempt == 3)
                {
                    throw new Exception(responseText);
                }

                var waitSeconds = ExtractGroqRetrySeconds(responseText);

                if (waitSeconds > 60)
                {
                    throw new Exception(
                        "Groq rate limit wait time is too long. Please try again later. " +
                        responseText);
                }

                waitSeconds = Math.Max(waitSeconds, 10);

                Console.WriteLine(
                    $"Groq rate limit. Attempt {attempt}/3. Waiting {waitSeconds}s");

                await Task.Delay(
                    TimeSpan.FromSeconds(waitSeconds));

                request = CloneHttpRequestMessage(
                    request,
                    requestJson);
            }
            catch (TaskCanceledException)
            {
                if (attempt == 3)
                {
                    throw new Exception(
                        "Groq request timed out after 90 seconds");
                }

                await Task.Delay(TimeSpan.FromSeconds(10));

                request = CloneHttpRequestMessage(
                    request,
                    requestJson);
            }
        }

        throw new Exception("Groq rate limit exceeded");
    }
    private static double ExtractGroqRetrySeconds(string responseText)
    {
        var minuteMatch = Regex.Match(
            responseText,
            @"try again in ([0-9.]+)m([0-9.]+)s",
            RegexOptions.IgnoreCase);

        if (minuteMatch.Success &&
            double.TryParse(minuteMatch.Groups[1].Value, out var minutes) &&
            double.TryParse(minuteMatch.Groups[2].Value, out var seconds))
        {
            return minutes * 60 + seconds;
        }

        var secondMatch = Regex.Match(
            responseText,
            @"try again in ([0-9.]+)s",
            RegexOptions.IgnoreCase);

        if (secondMatch.Success &&
            double.TryParse(secondMatch.Groups[1].Value, out var secondOnly))
        {
            return secondOnly;
        }

        return 30;
    }
    private static HttpRequestMessage CloneHttpRequestMessage(
    HttpRequestMessage oldRequest,
    string requestJson)
    {
        var newRequest = new HttpRequestMessage(
            oldRequest.Method,
            oldRequest.RequestUri);

        foreach (var header in oldRequest.Headers)
        {
            newRequest.Headers.TryAddWithoutValidation(
                header.Key,
                header.Value);
        }

        newRequest.Content = new StringContent(
            requestJson,
            Encoding.UTF8,
            "application/json");

        return newRequest;
    }
    private static string ExtractAnswerLinesForRange(
    string answerText,
    int startQuestionNumber,
    int endQuestionNumber)
    {
        if (string.IsNullOrWhiteSpace(answerText))
        {
            return "";
        }

        var matches = Regex.Matches(
            answerText,
            @"(?im)\b(\d+)\s*[\.\)]\s*([A-D])\b");

        var lines = matches
            .Cast<Match>()
            .Select(x => new
            {
                Number = int.Parse(x.Groups[1].Value),
                Answer = x.Groups[2].Value.Trim().ToUpper()
            })
            .Where(x =>
                x.Number >= startQuestionNumber &&
                x.Number <= endQuestionNumber)
            .OrderBy(x => x.Number)
            .Select(x => $"{x.Number}. {x.Answer}")
            .ToList();

        return string.Join("\n", lines);
    }
    private static List<ReadingChunk> SplitByLength(
        string text,
        int maxLength)
    {
        var chunks = new List<ReadingChunk>();

        var passageNumber = 1;

        for (var i = 0; i < text.Length; i += maxLength)
        {
            chunks.Add(new ReadingChunk
            {
                PassageNumber = passageNumber,
                Text = text.Substring(
                    i,
                    Math.Min(maxLength, text.Length - i))
            });

            passageNumber++;
        }

        return chunks;
    }

    private static string BuildPrompt(
        string chunkText,
        int passageNumber)
    {
        return
            "Parse this English reading test chunk into structured JSON.\n\n" +
            "Return only valid JSON. No markdown. No explanation.\n\n" +
            "Important rules:\n" +
            $"- This chunk belongs to passageNumber {passageNumber}.\n" +
            "- Extract exactly one reading passage from this chunk if possible.\n" +
            "- Extract the passage title if available.\n" +
            "- Extract instruction if available.\n" +
          
"- If the chunk contains General Directions, put that text into instruction of the first passage.\n" +
            "- Extract passageText separately from questions.\n" +
            "- Preserve the original paragraph breaks in passageText.\n" +
            "- Use \\n\\n between paragraphs in passageText exactly where the source chunk has blank lines.\n" +
            "- Do not collapse passageText into a single paragraph.\n" +
            "- Do not add new paragraph breaks unless they already exist in the source chunk.\n" +
            "- Extract only answerable multiple choice questions.\n" +
            "- Preserve each original question number exactly; do not renumber questions.\n" +
            "- Extract options A, B, C, D if available.\n" +
            "- Use the Answer section in this chunk to mark isCorrect.\n" +
            "- Exactly one option must have isCorrect true if answer is known.\n" +
            "- Do not invent passage text.\n" +
            "- Do not invent questions.\n" +
            "- Do not invent answers.\n" +
            "- Keep question content clean. Do not include options inside content.\n" +
            "- Do not skip sentence insertion questions such as 'In which space...' if they have options A, B, C, D.\n" +
            "- Sentence insertion questions are valid multiple-choice questions.\n" +
"- For questions beginning with 'In which space', include the sentence shown between the question and options as part of the question content.\n" +
"- Even if markers A, B, C, D are not visible inside the passage, still extract the question and its four options.\n" +
"- Never omit a numbered question only because its answer options are A, B, C, D themselves.\n" +
"- Do not skip reference, vocabulary, inference, tone, organization, or sentence insertion questions.\n" +
"- Extract every numbered question in the expected range of this passage.\n" +
"- Only ignore a question if it truly has no A-D options in the source text.\n\n" +
            "JSON format:\n" +
            "{\n" +
            "  \"passages\": [\n" +
            "    {\n" +
            "      \"passageNumber\": 1,\n" +
            "      \"title\": \"Passage 1\",\n" +
            "      \"instruction\": \"Read the passage and choose the best answer.\",\n" +
            "      \"passageText\": \"full passage text\",\n" +
            "      \"questions\": [\n" +
            "        {\n" +
            "          \"questionNumber\": 1,\n" +
            "          \"content\": \"question content\",\n" +
     
            "          \"options\": [\n" +
            "            {\n" +
            "              \"label\": \"A\",\n" +
            "              \"content\": \"option content\",\n" +
            "              \"isCorrect\": true\n" +
            "            }\n" +
            "          ]\n" +
            "        }\n" +
            "      ]\n" +
            "    }\n" +
            "  ]\n" +
            "}\n\n" +
            "Reading chunk:\n" +
            chunkText;
    }

    private static string CleanJson(string content)
    {
        content = content
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        var firstBrace = content.IndexOf('{');

        if (firstBrace < 0)
        {
            return content;
        }

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = firstBrace; i < content.Length; i++)
        {
            var current = content[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (current == '\\')
            {
                escaped = true;
                continue;
            }

            if (current == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (current == '{')
            {
                depth++;
            }

            if (current == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return content.Substring(
                        firstBrace,
                        i - firstBrace + 1);
                }
            }
        }

        return content;
    }

    private static void NormalizeAiResult(
    AiParsedReadingExerciseDto result,
    int fallbackPassageNumber)
    {
        var labels = new[] { "A", "B", "C", "D" };

        result.Passages = result.Passages
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.PassageText) &&
                x.Questions.Any())
            .ToList();

        foreach (var passage in result.Passages)
        {
            if (passage.PassageNumber <= 0)
            {
                passage.PassageNumber =
                    fallbackPassageNumber;
            }

            if (string.IsNullOrWhiteSpace(passage.Title))
            {
                passage.Title =
                    $"Passage {passage.PassageNumber}";
            }

            passage.Questions = passage.Questions
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Content) &&
                    x.Options.Count >= 2)
                .OrderBy(x => x.QuestionNumber)
                .ToList();

            foreach (var question in passage.Questions)
            {
               

                question.Options = question.Options
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Content))
                    .Take(4)
                    .ToList();

                for (var i = 0;
                     i < question.Options.Count &&
                     i < labels.Length;
                     i++)
                {
                    question.Options[i].Label =
                        labels[i];
                }

                if (question.Options.Count(x => x.IsCorrect) != 1)
                {
                    question.Options.ForEach(x =>
                        x.IsCorrect = false);
                }
            }
        }
    }
 
    private class ReadingChunk
    {
        public int PassageNumber { get; set; }

        public string Text { get; set; } = null!;
    }
}
