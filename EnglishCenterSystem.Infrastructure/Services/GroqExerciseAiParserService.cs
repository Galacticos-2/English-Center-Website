using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace EnglishCenterSystem.Infrastructure.Services;

public class GroqExerciseAiParserService
    : IExerciseAiParserService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqExerciseAiParserService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<AiParsedExerciseDto> ParseQuestionsAsync(
        string rawText)
    {
        var chunks = SplitIntoQuestionChunks(rawText, 5);

        var finalResult = new AiParsedExerciseDto();

        foreach (var chunk in chunks)
        {
            var chunkResult = await ParseChunkAsync(chunk);

            finalResult.Questions.AddRange(
                chunkResult.Questions);
        }

        if (!finalResult.Questions.Any())
        {
            throw new Exception("AI parser returned no questions");
        }

        return finalResult;
    }

    private async Task<AiParsedExerciseDto> ParseChunkAsync(
        string chunkText)
    {
        var apiKey = _configuration["Groq:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("Groq API key is missing");
        }

        var model =
            _configuration["Groq:Model"] ??
            "llama-3.3-70b-versatile";

        var prompt = BuildPrompt(chunkText);

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "You are an exam question parser. Return only valid JSON. Do not include markdown."
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

        try
        {
            var result =
                JsonSerializer.Deserialize<AiParsedExerciseDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null)
            {
                throw new Exception("AI parser returned null result");
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "AI returned invalid JSON. Raw content: " +
                json +
                " | Error: " +
                ex.Message);
        }
    }

    private static List<string> SplitIntoQuestionChunks(
        string rawText,
        int questionsPerChunk)
    {
        var normalizedText = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        var matches = Regex.Matches(
            normalizedText,
            @"(?im)(?=^\s*(?:Question\s*)?\d+\s*[\.\):])");

        if (matches.Count == 0)
        {
            return SplitByLength(normalizedText, 3500);
        }

        var questionBlocks = new List<string>();

        for (var i = 0; i < matches.Count; i++)
        {
            var startIndex = matches[i].Index;

            var endIndex = i + 1 < matches.Count
                ? matches[i + 1].Index
                : normalizedText.Length;

            var block = normalizedText
                .Substring(startIndex, endIndex - startIndex)
                .Trim();

            if (!string.IsNullOrWhiteSpace(block))
            {
                questionBlocks.Add(block);
            }
        }

        var chunks = new List<string>();

        for (var i = 0; i < questionBlocks.Count; i += questionsPerChunk)
        {
            var chunk = string.Join(
                "\n\n",
                questionBlocks
                    .Skip(i)
                    .Take(questionsPerChunk));

            chunks.Add(chunk);
        }

        return chunks;
    }

    private static List<string> SplitByLength(
        string text,
        int maxLength)
    {
        var chunks = new List<string>();

        for (var i = 0; i < text.Length; i += maxLength)
        {
            chunks.Add(
                text.Substring(
                    i,
                    Math.Min(maxLength, text.Length - i)));
        }

        return chunks;
    }

    private static string BuildPrompt(string rawText)
    {
        return
            "Parse this exam text into structured JSON.\n\n" +
            "Return only valid JSON. No markdown. No explanation.\n\n" +
            "Rules:\n" +
            "- Extract every question in this text.\n" +
            "- Preserve the original number in questionNumber.\n" +
            "- Extract options A, B, C, D if available.\n" +
            "- Detect the correct answer from Answer, Ans, Key, đáp án, or similar.\n" +
            "- Exactly one option should have isCorrect true if the answer is known.\n" +
            "- If the correct answer is unknown, set all isCorrect values to false.\n" +
            "- Do not invent questions.\n" +
            "- Do not invent answers.\n" +
            "- Keep question content clean. Do not include options inside content.\n" +
"- If a question has A/B/C/D options, use questionType SingleChoice.\n" +
"- If a question has True/False options, use questionType TrueFalse.\n" +
"- If a question has no options and requires filling text, use questionType TextAnswer.\n" +
"- For TextAnswer, put the answer into correctAnswer and keep options empty.\n\n" +
            "JSON format:\n" +
            "{\n" +
            "  \"questions\": [\n" +
            "    {\n" +
            "      \"questionNumber\": 1,\n" +
            "      \"content\": \"question content\",\n" +
            "      \"questionType\": \"SingleChoice\",\n" +
"      \"explanation\": null,\n" +
"      \"correctAnswer\": null,\n" +
"      \"options\": [\n" +
            "        {\n" +
            "          \"label\": \"A\",\n" +
            "          \"content\": \"option content\",\n" +
            "          \"isCorrect\": false\n" +
            "        }\n" +
            "      ]\n" +
            "    }\n" +
            "  ]\n" +
            "}\n\n" +
            "Exam text:\n" +
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
