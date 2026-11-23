using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishCenterSystem.Application.Features.Listening.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace EnglishCenterSystem.Infrastructure.Services;

public class GroqListeningAiParserService : IListeningAiParserService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqListeningAiParserService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<AiParsedListeningExerciseDto> ParseListeningQuestionsAsync(
        string rawText,
        int expectedQuestionCount)
    {
        var chunks = SplitIntoListeningQuestionChunks(
            rawText,
            expectedQuestionCount,
            5);

        var finalResult = new AiParsedListeningExerciseDto();

        foreach (var chunk in chunks)
        {
            var chunkResult = await ParseChunkAsync(
                chunk.Text,
                chunk.StartQuestionNumber,
                chunk.EndQuestionNumber);

            finalResult.Questions.AddRange(
                chunkResult.Questions);
        }

        finalResult.Questions = finalResult.Questions
            .Where(x => !string.IsNullOrWhiteSpace(x.Content))
            .GroupBy(x => x.QuestionNumber)
            .Select(x => x.First())
            .OrderBy(x => x.QuestionNumber)
            .ToList();

        if (!finalResult.Questions.Any())
        {
            throw new Exception("AI listening parser returned no questions");
        }

        return finalResult;
    }

    private async Task<AiParsedListeningExerciseDto> ParseChunkAsync(
        string chunkText,
        int startQuestionNumber,
        int endQuestionNumber)
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
            startQuestionNumber,
            endQuestionNumber);

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "You are an English listening test parser. Return only valid JSON. Do not include markdown."
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
        var requestJson = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.groq.com/openai/v1/chat/completions");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                apiKey);

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
            throw new Exception("AI listening parser returned empty response");
        }

        var json = CleanJson(content);

        try
        {
            var result =
                JsonSerializer.Deserialize<AiParsedListeningExerciseDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null)
            {
                throw new Exception("AI listening parser returned null result");
            }

            var answerMap = ExtractAnswerMap(chunkText);

            NormalizeAiResult(
                result,
                answerMap,
                startQuestionNumber,
                endQuestionNumber);

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "AI returned invalid listening JSON. Raw content: " +
                json +
                " | Error: " +
                ex.Message);
        }
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
    private static List<ListeningChunk> SplitIntoListeningQuestionChunks(
        string rawText,
        int expectedQuestionCount,
        int questionsPerChunk)
    {
        var normalizedText = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        var answerMatch = Regex.Match(
    normalizedText,
    @"(?im)^\s*(Answer\s*Key|Answers?|Đáp\s*án|Đáp\s*án\s*gợi\s*ý)\s*:?\s*$");

        if (!answerMatch.Success)
        {
            return SplitByLength(normalizedText, 3500);
        }

        var answerIndex = answerMatch.Index;

        var questionText =
            normalizedText.Substring(0, answerIndex);

        var answerText =
            normalizedText.Substring(answerIndex);

        var answerMatches = Regex.Matches(
            answerText,
            @"(?im)\b(\d+)\s*[\.\)]\s*([A-D])\b");

        var answerMap = answerMatches
            .Cast<Match>()
            .Select(x => new
            {
                Number = int.Parse(x.Groups[1].Value),
                Answer = x.Groups[2].Value.Trim()
            })
            .Where(x =>
                x.Number >= 1 &&
                x.Number <= expectedQuestionCount)
            .OrderBy(x => x.Number)
            .ToDictionary(
                x => x.Number,
                x => x.Answer);

        if (!answerMap.Any())
        {
            return SplitByLength(normalizedText, 3500);
        }

        var questionBlocks = new List<QuestionBlock>();

        foreach (var number in answerMap.Keys.OrderBy(x => x))
        {
            var currentMatch = Regex.Match(
                questionText,
                $@"(?m)^\s*{number}\s*[\.\)]\s+");

            if (!currentMatch.Success)
            {
                continue;
            }

            var nextNumber = answerMap.Keys
                .Where(x => x > number)
                .OrderBy(x => x)
                .FirstOrDefault();

            var endIndex = questionText.Length;

            if (nextNumber > 0)
            {
                var nextMatch = Regex.Match(
                    questionText.Substring(currentMatch.Index + 1),
                    $@"(?m)^\s*{nextNumber}\.\s+");

                if (nextMatch.Success)
                {
                    endIndex =
                        currentMatch.Index + 1 + nextMatch.Index;
                }
            }

            var blockText = questionText
                .Substring(
                    currentMatch.Index,
                    endIndex - currentMatch.Index)
                .Trim();

            if (!string.IsNullOrWhiteSpace(blockText))
            {
                questionBlocks.Add(new QuestionBlock
                {
                    QuestionNumber = number,
                    Text = blockText,
                    Answer = answerMap[number]
                });
            }
        }

        var chunks = new List<ListeningChunk>();

        for (var i = 0; i < questionBlocks.Count; i += questionsPerChunk)
        {
            var selectedBlocks = questionBlocks
                .Skip(i)
                .Take(questionsPerChunk)
                .ToList();

            var chunkQuestions = string.Join(
                "\n\n",
                selectedBlocks.Select(x => x.Text));

            var chunkAnswers =
                "Answer:\n" +
                string.Join(
                    "\n",
                    selectedBlocks.Select(x =>
                        $"{x.QuestionNumber}. {x.Answer}"));

            chunks.Add(new ListeningChunk
            {
                StartQuestionNumber =
                    selectedBlocks.Min(x => x.QuestionNumber),

                EndQuestionNumber =
                    selectedBlocks.Max(x => x.QuestionNumber),

                Text =
                    chunkQuestions +
                    "\n\n" +
                    chunkAnswers
            });
        }

        return chunks;
    }

    private static List<ListeningChunk> SplitByLength(
        string text,
        int maxLength)
    {
        var chunks = new List<ListeningChunk>();

        for (var i = 0; i < text.Length; i += maxLength)
        {
            chunks.Add(new ListeningChunk
            {
                StartQuestionNumber = 1,
                EndQuestionNumber = 999,
                Text = text.Substring(
                    i,
                    Math.Min(maxLength, text.Length - i))
            });
        }

        return chunks;
    }

    private static string BuildPrompt(
        string chunkText,
        int startQuestionNumber,
        int endQuestionNumber)
    {
        return
            "Parse this English listening test chunk into structured JSON.\n\n" +
            "Return only valid JSON. Do not include markdown or text outside the JSON object.\n\n" +
            "Rules:\n" +
            $"- Extract only questions from {startQuestionNumber} to {endQuestionNumber}.\n" +
            "- Do not include directions, part titles, conversation titles, or talk titles as questions.\n" +
            "- Extract questionNumber exactly from the text.\n" +
            "- Extract options A, B, C, D if available.\n" +
           
            "- Use the Answer section in this chunk to mark isCorrect.\n" +
            "- Exactly one option must have isCorrect true if the answer is known.\n" +
            "- Do not invent questions.\n" +
            "- Do not invent answers.\n" +
            "- Keep question content clean. Do not include options inside content.\n\n" +
            "JSON format:\n" +
            "{\n" +
            "  \"questions\": [\n" +
            "    {\n" +
            "      \"questionNumber\": 1,\n" +
            "      \"content\": \"question content\",\n" +
         
            "      \"options\": [\n" +
            "        {\n" +
            "          \"label\": \"A\",\n" +
            "          \"content\": \"option content\",\n" +
            "          \"isCorrect\": true\n" +
            "        }\n" +
            "      ]\n" +
            "    }\n" +
            "  ]\n" +
            "}\n\n" +
            "Listening chunk:\n" +
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
    public async Task<AiParsedListeningExerciseDto> ParseListeningQuestionBlocksAsync(
    List<ListeningQuestionBlockDto> failedBlocks)
    {
        var finalResult = new AiParsedListeningExerciseDto();

        if (failedBlocks == null || !failedBlocks.Any())
        {
            return finalResult;
        }

        foreach (var block in failedBlocks)
        {
            var chunkText =
                $"Question {block.QuestionNumber}:\n" +
                block.RawText +
                "\n\nAnswer:\n" +
                $"{block.QuestionNumber}. {block.CorrectAnswer}";

            var chunkResult = await ParseChunkAsync(
                chunkText,
                block.QuestionNumber,
                block.QuestionNumber);

            finalResult.Questions.AddRange(chunkResult.Questions);
        }

        finalResult.Questions = finalResult.Questions
            .Where(x => !string.IsNullOrWhiteSpace(x.Content))
            .GroupBy(x => x.QuestionNumber)
            .Select(x => x.First())
            .OrderBy(x => x.QuestionNumber)
            .ToList();

        return finalResult;
    }

    public async Task<AiParsedListeningExerciseDto> ParseListeningQuestionBlockGroupsAsync(
        List<List<ListeningQuestionBlockDto>> blockGroups)
    {
        var finalResult = new AiParsedListeningExerciseDto();

        if (blockGroups == null || !blockGroups.Any())
        {
            return finalResult;
        }

        foreach (var blockGroup in blockGroups)
        {
            var validBlocks = blockGroup
                .Where(x => x != null)
                .OrderBy(x => x.QuestionNumber)
                .ToList();

            if (!validBlocks.Any())
            {
                continue;
            }

            var startQuestionNumber =
                validBlocks.Min(x => x.QuestionNumber);

            var endQuestionNumber =
                validBlocks.Max(x => x.QuestionNumber);

            var chunkQuestions = string.Join(
                "\n\n",
                validBlocks.Select(block =>
                    $"Question {block.QuestionNumber}:\n" +
                    block.RawText));

            var chunkAnswers =
                "Answer:\n" +
                string.Join(
                    "\n",
                    validBlocks
                        .Where(block =>
                            !string.IsNullOrWhiteSpace(
                                block.CorrectAnswer))
                        .Select(block =>
                            $"{block.QuestionNumber}. {block.CorrectAnswer}"));

            var chunkText =
                chunkQuestions +
                "\n\n" +
                chunkAnswers;

            try
            {
                var chunkResult = await ParseChunkAsync(
                    chunkText,
                    startQuestionNumber,
                    endQuestionNumber);

                finalResult.Questions.AddRange(
                    chunkResult.Questions);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"AI returned invalid listening JSON for questions {startQuestionNumber}-{endQuestionNumber}. " +
                    ex.Message);
            }
        }

        finalResult.Questions = finalResult.Questions
            .Where(x => !string.IsNullOrWhiteSpace(x.Content))
            .GroupBy(x => x.QuestionNumber)
            .Select(x => x.First())
            .OrderBy(x => x.QuestionNumber)
            .ToList();

        return finalResult;
    }
    private static Dictionary<int, string> ExtractAnswerMap(
    string chunkText)
    {
        var matches = Regex.Matches(
            chunkText,
            @"(?im)\b(\d+)\s*[\.\)]\s*([A-D])\b");

        return matches
            .Cast<Match>()
            .ToDictionary(
                x => int.Parse(x.Groups[1].Value),
                x => x.Groups[2].Value.Trim());
    }

    private static void NormalizeAiResult(
    AiParsedListeningExerciseDto result,
    Dictionary<int, string> answerMap,
    int startQuestionNumber,
    int endQuestionNumber)
    {
        result.Questions = result.Questions
            .Where(x =>
                x.QuestionNumber >= startQuestionNumber &&
                x.QuestionNumber <= endQuestionNumber &&
                !string.IsNullOrWhiteSpace(x.Content))
            .OrderBy(x => x.QuestionNumber)
            .ToList();

        var labels = new[] { "A", "B", "C", "D" };

        foreach (var question in result.Questions)
        {
            
            question.Options = question.Options
                .Where(x => !string.IsNullOrWhiteSpace(x.Content))
                .Take(4)
                .ToList();

            for (var i = 0; i < question.Options.Count; i++)
            {
                question.Options[i].Label = labels[i];
                question.Options[i].IsCorrect = false;
            }

            if (answerMap.TryGetValue(
                question.QuestionNumber,
                out var correctLabel))
            {
                correctLabel = NormalizeOptionLabel(correctLabel);

                var correctIndex =
                    Array.IndexOf(labels, correctLabel);

                if (correctIndex >= 0 &&
                    correctIndex < question.Options.Count)
                {
                    question.Options[correctIndex].IsCorrect = true;
                }
            }
        }
    }
   
    private static string NormalizeOptionLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return "";
        }

        label = label.Trim().ToUpper();

        var match = Regex.Match(label, @"[A-D]");

        return match.Success
            ? match.Value
            : "";
    }
    private class ListeningChunk
    {
        public int StartQuestionNumber { get; set; }

        public int EndQuestionNumber { get; set; }

        public string Text { get; set; } = null!;
    }

    private class QuestionBlock
    {
        public int QuestionNumber { get; set; }

        public string Text { get; set; } = null!;

        public string Answer { get; set; } = null!;
    }
}
