using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishCenterSystem.Application.Features.Speaking.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace EnglishCenterSystem.Infrastructure.Services;

public class GroqSpeakingAiEvaluationService
    : ISpeakingAiEvaluationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqSpeakingAiEvaluationService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<SpeakingAiEvaluationDto> EvaluateAsync(
        string promptText,
        string? referenceText,
        string transcript)
    {
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
                        "You are an English speaking examiner. Return only valid JSON. No markdown."
                },
                new
                {
                    role = "user",
                    content = BuildPrompt(
                        promptText,
                        referenceText,
                        transcript)
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
            throw new Exception("AI speaking evaluation returned empty response");
        }

        var result =
            JsonSerializer.Deserialize<SpeakingAiEvaluationDto>(
                CleanJson(content),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result == null)
        {
            throw new Exception("AI speaking evaluation returned null result");
        }

        result.GrammarScore = NormalizeScore(result.GrammarScore);
        result.VocabularyScore = NormalizeScore(result.VocabularyScore);
        result.ContentRelevanceScore =
            NormalizeScore(result.ContentRelevanceScore);
        result.CoherenceScore = NormalizeScore(result.CoherenceScore);
        result.OverallScore = NormalizeScore(result.OverallScore);

        return result;
    }

    private static string BuildPrompt(
    string promptText,
    string? referenceText,
    string transcript)
    {
        return
            "Evaluate this English speaking answer.\n\n" +

            "Important context:\n" +
            "- The audio was transcribed automatically.\n" +
            "- Pronunciation and speech timing are evaluated separately.\n" +
            "- Focus on grammar, vocabulary, task achievement, relevance, coherence, and feedback.\n\n" +

            "Invalid response rules:\n" +
            "- If the response is irrelevant, off-topic, meaningless, or does not answer the prompt, it is invalid.\n" +
            "- If the response contains fewer than five meaningful words, it is invalid.\n" +
            "- If the response only contains a greeting, thanks, refusal, filler phrase, or unrelated sentence, it is invalid.\n" +
            "- An invalid response must receive zero for every criterion below.\n" +
            "- Pronunciation quality or fluency alone must never produce a passing score when the response is invalid.\n\n" +
            "- Write feedback in concise English.\n\n" +

            "For an invalid response, return exactly:\n" +
            "{\n" +
            "  \"grammarScore\": 0,\n" +
            "  \"vocabularyScore\": 0,\n" +
            "  \"contentRelevanceScore\": 0,\n" +
            "  \"coherenceScore\": 0,\n" +
            "  \"overallScore\": 0,\n" +
            "  \"feedback\": \"Explain clearly in English why the response is invalid.\"\n" +
            "}\n\n" +

            "For a valid response:\n" +
            "- Score each criterion from 0 to 10, using one decimal place.\n" +
            "- Evaluate whether the student actually answers the task.\n" +
            "- Do not give a positive relevance score merely because the transcript is grammatically understandable.\n" +
            "- overallScore must reflect task achievement, relevance, grammar, vocabulary, and coherence.\n\n" +

            "Return only valid JSON with this format:\n" +
            "{\n" +
            "  \"grammarScore\": 0,\n" +
            "  \"vocabularyScore\": 0,\n" +
            "  \"contentRelevanceScore\": 0,\n" +
            "  \"coherenceScore\": 0,\n" +
            "  \"overallScore\": 0,\n" +
            "  \"feedback\": \"English feedback\"\n" +
            "}\n\n" +

            "Prompt from PDF:\n" +
            promptText +

            "\n\nReference text if available:\n" +
            (referenceText ?? "") +

            "\n\nStudent transcript:\n" +
            transcript;
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

    private static double NormalizeScore(double value)
    {
        var normalizedValue = value > 10
            ? value / 10
            : value;

        return Math.Round(Math.Clamp(normalizedValue, 0, 10), 1);
    }
}
