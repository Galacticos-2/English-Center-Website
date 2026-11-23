using System.Net.Http.Headers;
using System.Net;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishCenterSystem.Application.Features.Exercises.DTOs;
using EnglishCenterSystem.Application.Features.Listening.DTOs;
using EnglishCenterSystem.Application.Features.Reading.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Domain.Entities.Listening;
using EnglishCenterSystem.Domain.Entities.Reading;
using Microsoft.Extensions.Configuration;

namespace EnglishCenterSystem.Infrastructure.Services;

public class GroqQuestionExplanationService
    : IQuestionExplanationService
{
    private static readonly ConcurrentDictionary<string, ExplanationResponse>
        ExplanationCache = new();

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqQuestionExplanationService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task AddReadingExplanationsAsync(
        AiParsedReadingTaskDto passage)
    {
        var questions = passage.Questions
            .Where(x => string.IsNullOrWhiteSpace(x.Explanation))
            .ToList();

        if (!ShouldGenerate() ||
            string.IsNullOrWhiteSpace(passage.PassageText) ||
            !questions.Any())
        {
            return;
        }

        var prompt = BuildReadingPrompt(
            new AiParsedReadingTaskDto
            {
                PassageNumber = passage.PassageNumber,
                Title = passage.Title,
                Instruction = passage.Instruction,
                PassageText = passage.PassageText,
                Questions = questions
            });

        await ApplyExplanationsAsync(
            prompt,
            questions,
            x => x.QuestionNumber,
            x => x.Explanation,
            (x, value) => x.Explanation = value);
    }

    public async Task AddListeningExplanationsAsync(
        string? transcript,
        List<AiParsedListeningQuestionDto> questions)
    {
        questions = questions?
            .Where(x => string.IsNullOrWhiteSpace(x.Explanation))
            .ToList() ?? new List<AiParsedListeningQuestionDto>();

        if (!ShouldGenerate() ||
            questions == null ||
            !questions.Any())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(transcript))
        {
            throw new Exception(
                "Listening explanation requires transcript, but transcript is empty");
        }

        foreach (var chunk in questions
            .OrderBy(x => x.QuestionNumber)
            .Chunk(8))
        {
            var chunkQuestions = chunk.ToList();

            var prompt = BuildListeningPrompt(
                transcript,
                chunkQuestions);

            await ApplyExplanationsAsync(
                prompt,
                chunkQuestions,
                x => x.QuestionNumber,
                x => x.Explanation,
                (x, value) => x.Explanation = value);
        }
    }

    public async Task AddExerciseExplanationsAsync(
        List<AiParsedQuestionDto> questions)
    {
        questions = questions?
            .Where(x => string.IsNullOrWhiteSpace(x.Explanation))
            .ToList() ?? new List<AiParsedQuestionDto>();

        if (!ShouldGenerate() ||
            questions == null ||
            !questions.Any())
        {
            return;
        }

        var numberedQuestions = questions
            .Select((question, index) => new NumberedExerciseQuestion(
                index + 1,
                question))
            .ToList();

        foreach (var chunk in numberedQuestions.Chunk(8))
        {
            var chunkQuestions = chunk.ToList();

            var prompt = BuildExercisePrompt(chunkQuestions);

            await ApplyExplanationsAsync(
                prompt,
                chunkQuestions,
                x => x.Number,
                x => x.Question.Explanation,
                (x, value) => x.Question.Explanation = value);
        }
    }

    public async Task AddReadingExplanationsAsync(
        ReadingTask task)
    {
        var missingQuestions = task.Questions
            .Where(x => string.IsNullOrWhiteSpace(x.Explanation))
            .OrderBy(x => x.OrderIndex)
            .ToList();

        if (!missingQuestions.Any())
        {
            return;
        }

        var dto = new AiParsedReadingTaskDto
        {
            PassageNumber = 1,
            Title = task.Title,
            Instruction = task.Instruction,
            PassageText = task.PassageText,
            Questions = missingQuestions
                .Select(question => new AiParsedReadingQuestionDto
                {
                    QuestionNumber = question.OrderIndex,
                    Content = question.Content,
                    Options = question.Options
                        .OrderBy(option => option.OrderIndex)
                        .Select((option, index) =>
                            new AiParsedReadingOptionDto
                            {
                                Label = ((char)('A' + index)).ToString(),
                                Content = option.Content,
                                IsCorrect = option.IsCorrect
                            })
                        .ToList()
                })
                .ToList()
        };

        await AddReadingExplanationsAsync(dto);

        foreach (var question in missingQuestions)
        {
            question.Explanation = dto.Questions
                .First(x => x.QuestionNumber == question.OrderIndex)
                .Explanation;
        }
    }

    public async Task AddListeningExplanationsAsync(
        ListeningTask task)
    {
        var missingQuestions = task.Questions
            .Where(x => string.IsNullOrWhiteSpace(x.Explanation))
            .OrderBy(x => x.OrderIndex)
            .ToList();

        if (!missingQuestions.Any())
        {
            return;
        }

        var dtos = missingQuestions
            .Select(question => new AiParsedListeningQuestionDto
            {
                QuestionNumber = question.OrderIndex,
                Content = question.Content,
                Options = question.Options
                    .OrderBy(option => option.OrderIndex)
                    .Select((option, index) =>
                        new AiParsedListeningOptionDto
                        {
                            Label = ((char)('A' + index)).ToString(),
                            Content = option.Content,
                            IsCorrect = option.IsCorrect
                        })
                    .ToList()
            })
            .ToList();

        await AddListeningExplanationsAsync(task.Transcript, dtos);

        foreach (var question in missingQuestions)
        {
            question.Explanation = dtos
                .First(x => x.QuestionNumber == question.OrderIndex)
                .Explanation;
        }
    }

    public async Task AddExerciseExplanationsAsync(
        IEnumerable<Question> questions)
    {
        var missingQuestions = questions
            .Where(x => string.IsNullOrWhiteSpace(x.Explanation))
            .ToList();

        if (!missingQuestions.Any())
        {
            return;
        }

        var dtos = missingQuestions
            .Select(question => new AiParsedQuestionDto
            {
                Content = question.Content,
                CorrectAnswer = question.CorrectTextAnswer,
                Explanation = question.Explanation,
                QuestionType = question.QuestionType.ToString(),
                Options = question.Options
                    .OrderBy(option => option.OrderIndex)
                    .Select((option, index) => new AiParsedOptionDto
                    {
                        Label = ((char)('A' + index)).ToString(),
                        Content = option.Content,
                        IsCorrect = option.IsCorrect
                    })
                    .ToList()
            })
            .ToList();

        await AddExerciseExplanationsAsync(dtos);

        for (var index = 0; index < missingQuestions.Count; index++)
        {
            missingQuestions[index].Explanation = dtos[index].Explanation;
        }
    }

    private bool ShouldGenerate()
    {
        var configured =
            _configuration["Groq:GenerateExplanations"];

        return string.IsNullOrWhiteSpace(configured) ||
               bool.TryParse(configured, out var enabled) && enabled;
    }

    private async Task ApplyExplanationsAsync<TQuestion>(
        string prompt,
        List<TQuestion> questions,
        Func<TQuestion, int> getQuestionNumber,
        Func<TQuestion, string?> getExplanation,
        Action<TQuestion, string?> setExplanation)
    {
        var result = await GenerateAsync(prompt);
        var missingQuestionNumbers = new List<int>();
        var orderedQuestions = questions
            .OrderBy(getQuestionNumber)
            .ToList();

        for (var i = 0; i < orderedQuestions.Count; i++)
        {
            var question = orderedQuestions[i];
            var questionNumber = getQuestionNumber(question);

            if (!result.Explanations.TryGetValue(
                    questionNumber,
                    out var explanation))
            {
                var ordinalQuestionNumber = i + 1;

                if (ordinalQuestionNumber == questionNumber ||
                    !result.Explanations.TryGetValue(
                        ordinalQuestionNumber,
                        out explanation))
                {
                    missingQuestionNumbers.Add(questionNumber);
                    continue;
                }
            }

            explanation = NormalizeExplanation(explanation);

            if (IsMeaningfulExplanation(explanation))
            {
                setExplanation(question, explanation);
            }
            else
            {
                missingQuestionNumbers.Add(questionNumber);
            }
        }

        missingQuestionNumbers.AddRange(
            questions
                .Where(question =>
                    string.IsNullOrWhiteSpace(
                        getExplanation(question)))
                .Select(getQuestionNumber));

        missingQuestionNumbers = missingQuestionNumbers
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (missingQuestionNumbers.Any())
        {
            throw new Exception(
                "Groq could not generate required explanations for questions: " +
                string.Join(", ", missingQuestionNumbers));
        }
    }

    private async Task<ExplanationResponse> GenerateAsync(
        string prompt)
    {
        var cacheKey = CreateCacheKey(prompt);

        if (ExplanationCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var responseText = await SendChatCompletionWithRetryAsync(prompt);

        using var document = JsonDocument.Parse(responseText);

        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new Exception("Groq explanation response is empty");
        }

        var json = CleanJson(content);

        try
        {
            var parsed = JsonSerializer
                .Deserialize<ExplanationResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            parsed ??= new ExplanationResponse();
            ExplanationCache[cacheKey] = parsed;

            return parsed;
        }
        catch
        {
            throw new Exception(
                "Groq returned invalid explanation JSON. Raw content: " +
                json);
        }
    }

    private async Task<string> SendChatCompletionWithRetryAsync(
        string prompt)
    {
        var apiKey = _configuration["Groq:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("Groq API key is missing");
        }

        Exception? lastException = null;

        foreach (var model in GetChatModels())
        {
            try
            {
                return await SendChatCompletionForModelAsync(
                    apiKey,
                    model,
                    prompt);
            }
            catch (Exception ex) when (ShouldTryFallbackModel(ex))
            {
                lastException = ex;
            }
        }

        throw new Exception(
            "Groq could not generate explanations with any configured model. " +
            lastException?.Message);
    }

    private async Task<string> SendChatCompletionForModelAsync(
        string apiKey,
        string model,
        string prompt)
    {
        var maxAttempts =
            GetIntConfig("Groq:MaxRetryAttempts", 5);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await DelayBetweenRequestsAsync();

            using var request = BuildChatRequest(
                apiKey,
                model,
                prompt);

            try
            {
                using var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(
                        GetIntConfig("Groq:RequestTimeoutSeconds", 120)));

                using var response = await _httpClient.SendAsync(
                    request,
                    cts.Token);

                var responseText =
                    await response.Content.ReadAsStringAsync(cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    return responseText;
                }

                if (response.StatusCode !=
                    HttpStatusCode.TooManyRequests)
                {
                    throw new Exception(
                        $"Groq model {model} failed with status " +
                        $"{(int)response.StatusCode}: {responseText}");
                }

                var waitSeconds = GetRateLimitWaitSeconds(
                    response,
                    responseText);

                lastException = new Exception(
                    BuildRateLimitMessage(
                        model,
                        response,
                        responseText,
                        waitSeconds));

                if (waitSeconds >
                    GetIntConfig("Groq:MaxRateLimitWaitSeconds", 45))
                {
                    break;
                }

                if (attempt >= maxAttempts)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;

                if (attempt >= maxAttempts)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        throw lastException ??
              new Exception($"Groq model {model} failed");
    }

    private HttpRequestMessage BuildChatRequest(
        string apiKey,
        string model,
        string prompt)
    {
        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "You write concise English answer explanations for English learners. Return only valid JSON."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            ["temperature"] = 0.15
        };

        if (IsQwenModel(model))
        {
            requestBody["reasoning_effort"] = "none";
        }
        else
        {
            requestBody["response_format"] = new
            {
                type = "json_object"
            };
        }

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

        return request;
    }

    private static bool IsQwenModel(string model)
    {
        return model.StartsWith(
            "qwen/",
            StringComparison.OrdinalIgnoreCase);
    }

    private List<string> GetChatModels()
    {
        var models = new List<string>();

        var primary =
            _configuration["Groq:Model"] ??
            "llama-3.3-70b-versatile";

        models.Add(primary);

        var fallbackModels =
            _configuration
                .GetSection("Groq:FallbackModels")
                .GetChildren()
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>();

        models.AddRange(fallbackModels);

        return models
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ShouldTryFallbackModel(Exception ex)
    {
        var message = ex.Message;

        return message.Contains(
                   "rate limit",
                   StringComparison.OrdinalIgnoreCase) ||
               message.Contains(
                   "429",
                   StringComparison.OrdinalIgnoreCase) ||
               message.Contains(
                   "model",
                   StringComparison.OrdinalIgnoreCase);
    }

    private async Task DelayBetweenRequestsAsync()
    {
        var delayMs = GetIntConfig(
            "Groq:DelayBetweenRequestsMs",
            750);

        if (delayMs > 0)
        {
            await Task.Delay(delayMs);
        }
    }

    private int GetRateLimitWaitSeconds(
        HttpResponseMessage response,
        string responseText)
    {
        var waitSeconds =
            TryParseSeconds(
                response.Headers.RetryAfter?.Delta?.ToString()) ??
            TryGetHeaderSeconds(response, "x-ratelimit-reset-tokens") ??
            TryGetHeaderSeconds(response, "x-ratelimit-reset-requests") ??
            ExtractGroqRetrySeconds(responseText) ??
            30;

        var maxWaitSeconds =
            GetIntConfig("Groq:MaxRateLimitWaitSeconds", 45);

        return Math.Max(waitSeconds, 1);
    }

    private static string BuildRateLimitMessage(
        string model,
        HttpResponseMessage response,
        string responseText,
        int waitSeconds)
    {
        var limitType = DetectRateLimitType(
            response,
            responseText,
            waitSeconds);

        return limitType switch
        {
            "daily" =>
                $"GROQ_DAILY_LIMIT: Groq model {model} reached daily quota. " +
                "Please wait until Groq daily quota resets or switch API key/project/model. " +
                responseText,

            "minute" =>
                $"GROQ_MINUTE_LIMIT: Groq model {model} reached short-term rate limit. " +
                $"Retry after about {waitSeconds}s. " +
                responseText,

            _ =>
                $"GROQ_RATE_LIMIT: Groq model {model} hit rate limit. " +
                $"Retry after about {waitSeconds}s. " +
                responseText
        };
    }

    private static string DetectRateLimitType(
        HttpResponseMessage response,
        string responseText,
        int waitSeconds)
    {
        var text = responseText.ToLowerInvariant();

        if (text.Contains("tokens per day") ||
            text.Contains("requests per day") ||
            text.Contains("audio seconds per day") ||
            text.Contains("per day") ||
            text.Contains("daily") ||
            text.Contains("quota") ||
            text.Contains("tpd") ||
            text.Contains("rpd") ||
            text.Contains("asd"))
        {
            return "daily";
        }

        if (text.Contains("tokens per minute") ||
            text.Contains("requests per minute") ||
            text.Contains("per minute") ||
            text.Contains("tpm") ||
            text.Contains("rpm") ||
            response.Headers.Contains("x-ratelimit-reset-tokens") ||
            response.Headers.Contains("x-ratelimit-reset-requests") ||
            waitSeconds < 3600)
        {
            return "minute";
        }

        return "unknown";
    }

    private static int? TryGetHeaderSeconds(
        HttpResponseMessage response,
        string headerName)
    {
        return response.Headers.TryGetValues(
                   headerName,
                   out var values)
            ? TryParseSeconds(values.FirstOrDefault())
            : null;
    }

    private static int? TryParseSeconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();

        if (double.TryParse(value, out var rawSeconds))
        {
            return (int)Math.Ceiling(rawSeconds);
        }

        var hourMatch = Regex.Match(
            value,
            @"(?:(?<h>[0-9.]+)h)?(?:(?<m>[0-9.]+)m)?(?<s>[0-9.]+)s",
            RegexOptions.IgnoreCase);

        if (hourMatch.Success)
        {
            var hours = hourMatch.Groups["h"].Success
                ? double.Parse(hourMatch.Groups["h"].Value)
                : 0;

            var minutes = hourMatch.Groups["m"].Success
                ? double.Parse(hourMatch.Groups["m"].Value)
                : 0;

            var seconds = double.Parse(
                hourMatch.Groups["s"].Value);

            return (int)Math.Ceiling(
                hours * 3600 + minutes * 60 + seconds);
        }

        var minuteMatch = Regex.Match(
            value,
            @"(?:(?<m>[0-9.]+)m)?(?<s>[0-9.]+)s",
            RegexOptions.IgnoreCase);

        if (minuteMatch.Success)
        {
            var minutes = minuteMatch.Groups["m"].Success
                ? double.Parse(minuteMatch.Groups["m"].Value)
                : 0;

            var seconds = double.Parse(
                minuteMatch.Groups["s"].Value);

            return (int)Math.Ceiling(minutes * 60 + seconds);
        }

        return null;
    }

    private static int? ExtractGroqRetrySeconds(string responseText)
    {
        var match = Regex.Match(
            responseText,
            @"try again in (?:(?<h>[0-9.]+)h)?(?:(?<m>[0-9.]+)m)?(?<s>[0-9.]+)s",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return null;
        }

        var hours = match.Groups["h"].Success
            ? double.Parse(match.Groups["h"].Value)
            : 0;

        var minutes = match.Groups["m"].Success
            ? double.Parse(match.Groups["m"].Value)
            : 0;

        var seconds = double.Parse(match.Groups["s"].Value);

        return (int)Math.Ceiling(
            hours * 3600 + minutes * 60 + seconds);
    }

    private int GetIntConfig(string key, int fallback)
    {
        return int.TryParse(_configuration[key], out var value) &&
               value > 0
            ? value
            : fallback;
    }

    private static string BuildReadingPrompt(
        AiParsedReadingTaskDto passage)
    {
        return
            "Generate explanations for the correct answers of this reading passage.\n" +
            "Return English explanations. Each explanation must cite concrete evidence from the provided evidence lines, wrapping each exact excerpt from the passage in double quotation marks.\n" +
            "Do not say an answer is correct only because the answer key says so.\n" +
            "Do not invent evidence. If evidence is weak, explain using the closest provided evidence line.\n\n" +
            "JSON format:\n" +
            "{ \"explanations\": { \"1\": \"...\", \"2\": \"...\" } }\n\n" +
            "Questions with local evidence:\n" +
            string.Join(
                "\n\n",
                passage.Questions.Select(question =>
                    FormatReadingQuestionWithEvidence(
                        question,
                        passage.PassageText)));
    }

    private static string BuildListeningPrompt(
        string transcript,
        List<AiParsedListeningQuestionDto> questions)
    {
        return
            "Generate explanations for the correct answers of these listening questions.\n" +
            "Return English explanations. Each explanation must cite concrete evidence from the transcript, using phrases such as 'the transcript/audio states...' when possible.\n" +
            "Do not say an answer is correct only because the answer key says so.\n" +
            "Do not invent evidence or speaker names if they are not in the transcript.\n\n" +
            "JSON format:\n" +
            "{ \"explanations\": { \"1\": \"...\", \"2\": \"...\" } }\n\n" +
            "Questions with local transcript evidence:\n" +
            string.Join(
                "\n\n",
                questions.Select(question =>
                    FormatListeningQuestionWithEvidence(
                        question,
                        transcript)));
    }

    private static string BuildExercisePrompt(
        List<NumberedExerciseQuestion> questions)
    {
        return
            "Generate explanations for these English grammar/general exercise questions.\n" +
            "Return English explanations. Explain the grammar rule, vocabulary clue, or logic that proves the correct answer.\n" +
            "Do not say an answer is correct only because it is marked correct.\n" +
            "For text-answer questions, explain why the correct text answer fits.\n\n" +
            "JSON format:\n" +
            "{ \"explanations\": { \"1\": \"...\", \"2\": \"...\" } }\n\n" +
            "Questions:\n" +
            string.Join(
                "\n\n",
                questions.Select(FormatExerciseQuestion));
    }

    private static string FormatReadingQuestion(
        AiParsedReadingQuestionDto question)
    {
        return FormatObjectiveQuestion(
            question.QuestionNumber,
            question.Content,
            question.Options.Select(x =>
                new OptionLine(
                    x.Label,
                    x.Content,
                    x.IsCorrect)));
    }

    private static string FormatReadingQuestionWithEvidence(
        AiParsedReadingQuestionDto question,
        string passageText)
    {
        return FormatReadingQuestion(question) +
               "\nEvidence:\n" +
               SelectEvidence(
                   passageText,
                   question.Content,
                   question.Options.Select(x => x.Content).ToList(),
                   1600);
    }

    private static string FormatListeningQuestion(
        AiParsedListeningQuestionDto question)
    {
        return FormatObjectiveQuestion(
            question.QuestionNumber,
            question.Content,
            question.Options.Select(x =>
                new OptionLine(
                    x.Label,
                    x.Content,
                    x.IsCorrect)));
    }

    private static string FormatListeningQuestionWithEvidence(
        AiParsedListeningQuestionDto question,
        string transcript)
    {
        return FormatListeningQuestion(question) +
               "\nTranscript evidence:\n" +
               SelectEvidence(
                   transcript,
                   question.Content,
                   question.Options.Select(x => x.Content).ToList(),
                   1200);
    }

    private static string FormatExerciseQuestion(
        NumberedExerciseQuestion item)
    {
        var question = item.Question;

        var optionLines =
            question.Options?.Any() == true
                ? string.Join(
                    "\n",
                    question.Options.Select(x =>
                        $"{x.Label}. {x.Content}" +
                        $"{(x.IsCorrect ? " [correct]" : "")}"))
                : $"Correct text answer: {question.CorrectAnswer}";

        return
            $"{item.Number}. {question.Content}\n" +
            $"Type: {question.QuestionType}\n" +
            optionLines;
    }

    private static string FormatObjectiveQuestion(
        int questionNumber,
        string content,
        IEnumerable<OptionLine> options)
    {
        return
            $"{questionNumber}. {content}\n" +
            string.Join(
                "\n",
                options.Select(x =>
                    $"{x.Label}. {x.Content}" +
                    $"{(x.IsCorrect ? " [correct]" : "")}"));
    }

    private static string SelectEvidence(
        string sourceText,
        string question,
        List<string> options,
        int maxChars)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return "";
        }

        var keywords = ExtractKeywords(
                question + " " + string.Join(" ", options))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var units = SplitEvidenceUnits(sourceText);

        if (!units.Any())
        {
            return Truncate(CleanEvidenceText(sourceText), maxChars);
        }

        var scored = units
            .Select((text, index) => new
            {
                Text = CleanEvidenceText(text),
                Index = index,
                Score = ScoreEvidence(text, keywords)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Index)
            .Take(4)
            .OrderBy(x => x.Index)
            .Select(x => x.Text)
            .ToList();

        if (!scored.Any() ||
            scored.All(x => ScoreEvidence(x, keywords) <= 0))
        {
            scored = units
                .Take(4)
                .Select(CleanEvidenceText)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        return Truncate(
            string.Join("\n", scored.Select(x => "- " + x)),
            maxChars);
    }

    private static List<string> SplitEvidenceUnits(string sourceText)
    {
        var normalized = sourceText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();

        var paragraphs = Regex.Split(normalized, @"\n\s*\n")
            .Select(x => x.Trim())
            .Where(x => x.Length > 0 && x.Length <= 900)
            .ToList();

        if (paragraphs.Count >= 3)
        {
            return paragraphs;
        }

        return Regex.Split(normalized, @"(?<=[\.\?\!])\s+")
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();
    }

    private static int ScoreEvidence(
        string text,
        HashSet<string> keywords)
    {
        if (!keywords.Any())
        {
            return 0;
        }

        var evidenceWords = ExtractKeywords(text)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return keywords.Count(evidenceWords.Contains);
    }

    private static IEnumerable<string> ExtractKeywords(string text)
    {
        var stopWords = new HashSet<string>(
            new[]
            {
                "the", "a", "an", "and", "or", "but", "to", "of", "in",
                "on", "at", "for", "with", "is", "are", "was", "were",
                "what", "why", "how", "which", "who", "does", "do", "did",
                "his", "her", "their", "this", "that", "these", "those",
                "he", "she", "they", "it", "as", "by", "from", "about"
            },
            StringComparer.OrdinalIgnoreCase);

        return Regex.Matches(
                text ?? "",
                @"[A-Za-z][A-Za-z'\-]{2,}")
            .Select(x => x.Value.Trim('\'', '-').ToLowerInvariant())
            .Where(x => x.Length >= 3 && !stopWords.Contains(x));
    }

    private static string CleanEvidenceText(string value)
    {
        return Regex.Replace(
                value
                    .Replace("\r", " ")
                    .Replace("\n", " "),
                @"\s+",
                " ")
            .Trim();
    }

    private static string Truncate(string value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length <= maxChars)
        {
            return value;
        }

        return value[..maxChars].Trim() + "...";
    }

    private static string? NormalizeExplanation(string? explanation)
    {
        if (string.IsNullOrWhiteSpace(explanation))
        {
            return null;
        }

        explanation = Regex.Replace(
                explanation
                    .Replace("\r", " ")
                    .Replace("\n", " "),
                @"\s+",
                " ")
            .Trim();

        return explanation.Length > 2000
            ? explanation[..2000]
            : explanation;
    }

    private static bool IsMeaningfulExplanation(string? explanation)
    {
        if (string.IsNullOrWhiteSpace(explanation) ||
            explanation.Length < 30)
        {
            return false;
        }

        var lower = explanation.ToLowerInvariant();

        var weakPatterns = new[]
        {
            "vì đáp án đúng",
            "because it is correct",
            "answer key",
            "đáp án là đáp án"
        };

        return !weakPatterns.Any(pattern =>
            lower == pattern ||
            lower.StartsWith(pattern + "."));
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

    private static string CreateCacheKey(string prompt)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(prompt));

        return Convert.ToHexString(bytes);
    }

    private class ExplanationResponse
    {
        public Dictionary<int, string> Explanations { get; set; }
            = new();
    }

    private record NumberedExerciseQuestion(
        int Number,
        AiParsedQuestionDto Question);

    private record OptionLine(
        string Label,
        string Content,
        bool IsCorrect);
}
