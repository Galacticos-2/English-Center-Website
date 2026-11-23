using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishCenterSystem.Application.Features.Speaking.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
namespace EnglishCenterSystem.Infrastructure.Services;

public class GroqSpeakingAiParserService : ISpeakingAiParserService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqSpeakingAiParserService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<AiParsedSpeakingExerciseDto> ParseSpeakingTasksAsync(
        string rawText)
    {
        var deterministicResult = FallbackParse(rawText);

        if (HasExplicitSpeakingMarkers(rawText) &&
            deterministicResult.Tasks.Any())
        {
            return deterministicResult;
        }
        var apiKey = _configuration["Groq:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("Groq API key is missing");
        }

        var model =
            _configuration["Groq:Model"] ??
            "llama-3.3-70b-versatile";

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "You are an English speaking test parser. Return only valid JSON. No markdown."
                },
                new
                {
                    role = "user",
                    content = BuildPrompt(rawText)
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
            new AuthenticationHeaderValue("Bearer", apiKey);

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
            throw new Exception("AI speaking parser returned empty response");
        }

        var result =
            JsonSerializer.Deserialize<AiParsedSpeakingExerciseDto>(
                CleanJson(content),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result == null || !result.Tasks.Any())
        {
            return FallbackParse(rawText);
        }

        result.Tasks = result.Tasks
            .Where(x => !string.IsNullOrWhiteSpace(x.PromptText))
            .OrderBy(x => x.TaskNumber)
            .ToList();

        return result.Tasks.Any()
            ? result
            : FallbackParse(rawText);
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

                var response = await _httpClient.SendAsync(
                    request,
                    cts.Token);

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
    private static string BuildPrompt(string rawText)
    {
        return
            "Parse this English speaking test into separate speaking tasks.\n\n" +
            "Rules:\n" +
            "- If the test has Part 1, Part 2, Part 3, return 3 tasks.\n" +
            "- If the test has Task 1, Task 2, return 2 tasks.\n" +
            "- If there is only one speaking prompt, return 1 task.\n" +
            "- Do not create grammar questions.\n" +
            "- Do not create multiple choice questions.\n" +
            "- Keep full prompt/instruction for each speaking task.\n" +
            "- Return only valid JSON.\n\n" +
            "JSON format:\n" +
            "{\n" +
            "  \"tasks\": [\n" +
            "    {\n" +
            "      \"taskNumber\": 1,\n" +
            "      \"title\": \"Part 1 - Social Interaction\",\n" +
            "      \"instruction\": \"instruction if any\",\n" +
            "      \"promptText\": \"full speaking prompt\",\n" +
            "      \"rubric\": null,\n" +
            "      \"preparationTimeSeconds\": 60,\n" +
            "      \"speakingTimeSeconds\": 180\n" +
            "    }\n" +
            "  ]\n" +
            "}\n\n" +
            "Speaking test text:\n" +
            rawText;
    }

    private static string CleanJson(string content)
    {
        content = content
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        var firstBrace = content.IndexOf('{');
        var lastBrace = content.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return content.Substring(
                firstBrace,
                lastBrace - firstBrace + 1);
        }

        return content;
    }

    private static AiParsedSpeakingExerciseDto FallbackParse(
        string rawText)
    {
        var result = new AiParsedSpeakingExerciseDto();

        var normalized = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        var matches = System.Text.RegularExpressions.Regex.Matches(
    normalized,
    @"(?im)^\s*(part|task)\s+(\d+)\s*[:：]?\s*(.*)$");

        if (matches.Count == 0)
        {
            result.Tasks.Add(new AiParsedSpeakingTaskDto
            {
                TaskNumber = 1,
                Title = "Speaking Task 1",
                PromptText = normalized.Trim(),
                PreparationTimeSeconds = 60,
                SpeakingTimeSeconds = 180
            });

            return result;
        }

        for (var i = 0; i < matches.Count; i++)
        {
            var current = matches[i];
            var start = current.Index;
            var end = i + 1 < matches.Count
                ? matches[i + 1].Index
                : normalized.Length;

            var block = normalized
                .Substring(start, end - start)
                .Trim();

            var number = int.Parse(current.Groups[2].Value);

            var titleSuffix = current.Groups[3].Value.Trim();

            result.Tasks.Add(new AiParsedSpeakingTaskDto
            {
                TaskNumber = number,
                Title = string.IsNullOrWhiteSpace(titleSuffix)
    ? $"Part {number}"
    : $"Part {number} - {titleSuffix}",
                PromptText = block,
                PreparationTimeSeconds = 60,
                SpeakingTimeSeconds = number == 1 ? 180 : 240
            });
        }

        return result;
    }

    private static bool HasExplicitSpeakingMarkers(string rawText)
    {
        return Regex.IsMatch(
            rawText,
            @"(?im)^\s*(?:part|task)\s+\d+\s*[:：]?\s*");
    }
}
