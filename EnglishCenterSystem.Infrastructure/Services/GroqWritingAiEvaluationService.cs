using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishCenterSystem.Application.Features.Writing.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Writing;
using Microsoft.Extensions.Configuration;

namespace EnglishCenterSystem.Infrastructure.Services;

public class GroqWritingAiEvaluationService
    : IWritingAiEvaluationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqWritingAiEvaluationService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<WritingAiScoreDto> EvaluateAsync(
        WritingTask writingTask,
        string essayText)
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

        var prompt = BuildPrompt(
            writingTask,
            essayText);

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "You are an English writing examiner. Return only valid JSON. Do not include markdown."
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

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);

        var responseText =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(responseText);
        }

        using var document =
            JsonDocument.Parse(responseText);

        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new Exception("AI evaluation returned empty response");
        }

        var json = CleanJson(content);

        var result =
            JsonSerializer.Deserialize<WritingAiScoreDto>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result == null)
        {
            throw new Exception("AI evaluation returned invalid JSON");
        }

        result.OverallBand =
            Clamp(result.OverallBand, 0, 10);

        result.TaskAchievement =
            Clamp(result.TaskAchievement, 0, 2.5);

        result.CoherenceAndCohesion =
            Clamp(result.CoherenceAndCohesion, 0, 2.5);

        result.LexicalResource =
            Clamp(result.LexicalResource, 0, 2.5);

        result.GrammaticalRangeAndAccuracy =
            Clamp(result.GrammaticalRangeAndAccuracy, 0, 2.5);

        if (string.IsNullOrWhiteSpace(result.Feedback))
        {
            result.Feedback = "No feedback returned.";
        }

        return result;
    }

    private static string BuildPrompt(
        WritingTask writingTask,
        string essayText)
    {
        return
            "Grade this English writing answer on a 10-point scale.\n\n" +
            "Rubric:\n" +
            "- TaskAchievement: 0 to 2.5\n" +
            "- CoherenceAndCohesion: 0 to 2.5\n" +
            "- LexicalResource: 0 to 2.5\n" +
            "- GrammaticalRangeAndAccuracy: 0 to 2.5\n" +
            "- OverallBand must be the sum of the four criteria, max 10.\n\n" +
            "Strict rules:\n" +
            "- If the essay is empty, score 0.\n" +
            "- If the essay has fewer than 30 words, maximum OverallBand is 3.\n" +
            "- If the essay has fewer than the required minimum words, penalize TaskAchievement.\n" +
            "- If the answer is off-topic, maximum OverallBand is 4.\n" +
            "- If the answer only copies the prompt, maximum OverallBand is 2.\n\n" +
            "Return only valid JSON in this format:\n" +
            "{\n" +
            "  \"overallBand\": 7.5,\n" +
            "  \"taskAchievement\": 2.0,\n" +
            "  \"coherenceAndCohesion\": 2.0,\n" +
            "  \"lexicalResource\": 1.8,\n" +
            "  \"grammaticalRangeAndAccuracy\": 1.7,\n" +
            "  \"feedback\": \"Detailed feedback here.\",\n" +
            "  \"improvementSuggestions\": [\"Suggestion 1\", \"Suggestion 2\"]\n" +
            "}\n\n" +
            "Writing task:\n" +
            $"Title: {writingTask.Title}\n" +
            $"Task type: {writingTask.TaskType}\n" +
            $"Topic/Situation: {writingTask.ReferenceText}\n" +
            $"Source text: {writingTask.SourceText}\n" +
            $"Prompt: {writingTask.PromptText}\n" +
            $"Requirements: {writingTask.Requirements}\n" +
            $"Minimum words: {writingTask.MinimumWords}\n" +
            $"Scoring criteria: {writingTask.ScoringCriteria}\n\n" +
            "Student answer:\n" +
            essayText;
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

    private static double Clamp(
        double value,
        double min,
        double max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return Math.Round(value, 2);
    }
}