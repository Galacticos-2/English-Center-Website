using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;

using System.Text.Json;
using EnglishCenterSystem.Application.Features.Writing.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace EnglishCenterSystem.Infrastructure.Services;

public class GroqWritingAiParserService : IWritingAiParserService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqWritingAiParserService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<AiParsedWritingTaskDto> ParseWritingTaskAsync(
        string rawText)
    {
        var apiKey = _configuration["Groq:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("Groq API key is missing");
        }

        var model =
            _configuration["Groq:Model"] ??
            _configuration["Groq:model"] ??
            "llama-3.3-70b-versatile";

        var prompt = BuildPrompt(rawText);

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "You are a writing exam parser. Return only valid JSON. Do not include markdown."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.1
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
            throw new Exception("AI parser returned empty response");
        }

        var json = CleanJson(content);

        var result = JsonSerializer.Deserialize<AiParsedWritingTaskDto>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result == null ||
    result.Tasks == null ||
    !result.Tasks.Any())
        {
            throw new Exception("AI parser returned no writing tasks");
        }

        foreach (var task in result.Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Title))
            {
                task.Title = $"Writing Task {task.OrderIndex}";
            }

            if (string.IsNullOrWhiteSpace(task.PromptText))
            {
                task.PromptText = rawText;
            }

            if (task.MinimumWords.GetValueOrDefault() <= 0)
            {
                task.MinimumWords = task.TaskType.Equals(
                    "Essay",
                    StringComparison.OrdinalIgnoreCase)
                    ? 250
                    : 120;
            }
        }

        return result;
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
            "Parse this English Writing exam document into structured JSON.\n\n" +
            "Return only valid JSON. No markdown. No explanation.\n\n" +
            "The document may contain 1 or more writing tasks.\n\n" +
            "Task type rules:\n" +
            "- If the task asks the student to write a reply, email, letter, message, or response to a person, use taskType EmailReply.\n" +
            "- If the task asks the student to write an essay discussing a problem, opinion, issue, trend, advantages, disadvantages, or effects, use taskType Essay.\n\n" +
            "Extraction rules:\n" +
            "- Extract every writing task separately.\n" +
            "- Preserve the original Question, Task, or Part number in orderIndex.\n" +
            "- Do not merge Task 1 and Task 2.\n" +
            "- Keep the original situation/context in topicText.\n" +
            "- Keep email/article/quote/chart/input material in sourceText.\n" +
            "- Keep the direct command such as 'Write a reply...' or 'Write an essay...' in promptText.\n" +
            "- Keep bullet requirements in requirements.\n" +
            "- Extract minimum word count as minimumWords.\n" +
            "- Extract scoring criteria into scoringCriteria.\n" +
            "- Do not invent content.\n" +
            "- Preserve important details and names.\n\n" +
            "Return JSON in exactly this format:\n" +
            "{\n" +
            "  \"tasks\": [\n" +
            "    {\n" +
            "      \"title\": \"Task 1 - Camping Trip Reply\",\n" +
            "      \"taskType\": \"EmailReply\",\n" +
            "      \"orderIndex\": 1,\n" +
            "      \"instruction\": \"You should write at least 120 words.\",\n" +
            "      \"topicText\": \"You live in Hanoi. Recently, you went on a camping trip...\",\n" +
            "      \"sourceText\": \"I can't stop thinking about the amazing time... Alex\",\n" +
            "      \"promptText\": \"Write a reply to Alex.\",\n" +
            "      \"requirements\": \"- tell him that you enjoyed the trip and why\\n- describe a challenge you faced\\n- suggest a location and time for your next adventure together\",\n" +
            "      \"minimumWords\": 120,\n" +
            "      \"scoringCriteria\": \"Task Fulfillment, Organization, Vocabulary, Grammar\",\n" +
            "      \"referenceText\": null\n" +
            "    }\n" +
            "  ]\n" +
            "}\n\n" +
            "Document text:\n" +
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

        if (firstBrace >= 0 &&
            lastBrace > firstBrace)
        {
            return content.Substring(
                firstBrace,
                lastBrace - firstBrace + 1);
        }

        return content;
    }
}
