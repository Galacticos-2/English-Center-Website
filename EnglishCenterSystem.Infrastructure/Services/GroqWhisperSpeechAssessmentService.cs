using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishCenterSystem.Application.Features.Speaking.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace EnglishCenterSystem.Infrastructure.Services;

public class GroqWhisperSpeechAssessmentService
    : ISpeechAssessmentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqWhisperSpeechAssessmentService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<SpeakingAssessmentResultDto> AssessAsync(
        Stream audioStream,
        string fileName,
        string? referenceText)
    {
        var apiKey = _configuration["Groq:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("Groq API key is missing");
        }

        if (audioStream.CanSeek)
        {
            audioStream.Position = 0;
        }

        byte[] audioBytes;

        using (var memoryStream = new MemoryStream())
        {
            await audioStream.CopyToAsync(memoryStream);
            audioBytes = memoryStream.ToArray();
        }

        var responseText = await SendTranscriptionWithRetryAsync(
            apiKey,
            audioBytes,
            fileName);

        using var document = JsonDocument.Parse(responseText);

        var root = document.RootElement;

        var transcript = root.TryGetProperty("text", out var textElement)
            ? textElement.GetString() ?? ""
            : "";

        var segments = ExtractSegments(root);

        return BuildAssessmentResult(
            transcript,
            segments,
            referenceText);
    }

    private async Task<string> SendTranscriptionWithRetryAsync(
        string apiKey,
        byte[] audioBytes,
        string fileName)
    {
        Exception? lastException = null;

        foreach (var model in GetWhisperModels())
        {
            try
            {
                return await SendTranscriptionForModelAsync(
                    apiKey,
                    audioBytes,
                    fileName,
                    model);
            }
            catch (Exception ex) when (ShouldTryFallbackModel(ex))
            {
                lastException = ex;
            }
        }

        throw new Exception(
            "Groq Whisper could not transcribe audio with any configured model. " +
            lastException?.Message);
    }

    private async Task<string> SendTranscriptionForModelAsync(
        string apiKey,
        byte[] audioBytes,
        string fileName,
        string model)
    {
        var maxAttempts =
            GetIntConfig("Groq:MaxRetryAttempts", 5);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await DelayBetweenRequestsAsync();

            using var request = BuildTranscriptionRequest(
                apiKey,
                audioBytes,
                fileName,
                model);

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
                        $"Groq Whisper model {model} failed with status " +
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
              new Exception($"Groq Whisper model {model} failed");
    }

    private HttpRequestMessage BuildTranscriptionRequest(
        string apiKey,
        byte[] audioBytes,
        string fileName,
        string model)
    {
        var form = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(audioBytes);
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(GetContentType(fileName));

        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(model), "model");
        form.Add(new StringContent("verbose_json"), "response_format");
        form.Add(new StringContent("en"), "language");
        form.Add(new StringContent("0"), "temperature");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.groq.com/openai/v1/audio/transcriptions");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        request.Content = form;

        return request;
    }

    private List<string> GetWhisperModels()
    {
        var models = new List<string>
        {
            _configuration["Groq:WhisperModel"] ??
            "whisper-large-v3-turbo"
        };

        var fallbackModels =
            _configuration
                .GetSection("Groq:WhisperFallbackModels")
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
            "audio_daily" =>
                $"GROQ_AUDIO_DAILY_LIMIT: Groq Whisper model {model} reached daily audio quota. " +
                "Please wait until Groq daily audio quota resets or switch API key/project/model. " +
                responseText,

            "daily" =>
                $"GROQ_DAILY_LIMIT: Groq Whisper model {model} reached daily quota. " +
                "Please wait until Groq daily quota resets or switch API key/project/model. " +
                responseText,

            "minute" =>
                $"GROQ_MINUTE_LIMIT: Groq Whisper model {model} reached short-term rate limit. " +
                $"Retry after about {waitSeconds}s. " +
                responseText,

            _ =>
                $"GROQ_RATE_LIMIT: Groq Whisper model {model} hit rate limit. " +
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

        if (text.Contains("audio seconds per day") ||
            text.Contains("asd"))
        {
            return "audio_daily";
        }

        if (text.Contains("tokens per day") ||
            text.Contains("requests per day") ||
            text.Contains("per day") ||
            text.Contains("daily") ||
            text.Contains("quota") ||
            text.Contains("tpd") ||
            text.Contains("rpd"))
        {
            return "daily";
        }

        if (text.Contains("tokens per minute") ||
            text.Contains("requests per minute") ||
            text.Contains("audio seconds per hour") ||
            text.Contains("per minute") ||
            text.Contains("per hour") ||
            text.Contains("tpm") ||
            text.Contains("rpm") ||
            text.Contains("ash") ||
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

    private static List<WhisperSegment> ExtractSegments(
        JsonElement root)
    {
        var result = new List<WhisperSegment>();

        if (!root.TryGetProperty("segments", out var segmentsElement) ||
            segmentsElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var segment in segmentsElement.EnumerateArray())
        {
            var text = segment.TryGetProperty("text", out var textElement)
                ? textElement.GetString() ?? ""
                : "";

            var start = segment.TryGetProperty("start", out var startElement)
                ? startElement.GetDouble()
                : 0;

            var end = segment.TryGetProperty("end", out var endElement)
                ? endElement.GetDouble()
                : start;

            if (!string.IsNullOrWhiteSpace(text))
            {
                result.Add(new WhisperSegment
                {
                    Text = text.Trim(),
                    Start = start,
                    End = end
                });
            }
        }

        return result;
    }

    private SpeakingAssessmentResultDto BuildAssessmentResult(
        string transcript,
        List<WhisperSegment> segments,
        string? referenceText)
    {
        var spokenWords = Tokenize(transcript);
        var referenceWords = Tokenize(referenceText ?? "");

        var durationSeconds = GetDurationSeconds(segments);
        var wordCount = spokenWords.Count;

        var wordsPerMinute = durationSeconds <= 0
            ? 0
            : Math.Round(wordCount / durationSeconds * 60, 2);

        var fillerWordCount = CountFillers(spokenWords);
        var repetitionCount = CountRepetitions(spokenWords);
        var pauses = CalculatePauses(segments);

        var completenessScore = CalculateCompletenessScore(
            spokenWords,
            referenceWords);

        var accuracyScore = CalculateAccuracyScore(
            spokenWords,
            referenceWords);

        var fluencyScore = CalculateFluencyScore(
            wordsPerMinute,
            fillerWordCount,
            repetitionCount,
            pauses.LongPauseCount);

        var prosodyScore = CalculateProsodyEstimate(
            wordsPerMinute,
            pauses.LongPauseCount,
            pauses.AveragePauseSeconds);

        var pronunciationScore = Math.Round(
            accuracyScore * 0.45 +
            fluencyScore * 0.35 +
            completenessScore * 0.20,
            2);

        var wordMetrics = BuildWordMetrics(
            spokenWords,
            referenceWords,
            segments);

        var analysisJson = JsonSerializer.Serialize(
            new
            {
                wordsPerMinute,
                wordCount,
                fillerWordCount,
                repetitionCount,
                pauses.LongPauseCount,
                pauses.AveragePauseSeconds,
                note = "This is an estimated speaking analysis from Groq Whisper transcript and timing. It is not phoneme-level acoustic scoring.",
                words = wordMetrics
            },
            new JsonSerializerOptions
            {
                WriteIndented = false
            });

        return new SpeakingAssessmentResultDto
        {
            Transcript = transcript,
            PronunciationScore = ToTenPointScore(pronunciationScore),
            AccuracyScore = ToTenPointScore(accuracyScore),
            FluencyScore = ToTenPointScore(fluencyScore),
            CompletenessScore = ToTenPointScore(completenessScore),
            ProsodyScore = ToTenPointScore(prosodyScore),
            WordsPerMinute = wordsPerMinute,
            WordCount = wordCount,
            FillerWordCount = fillerWordCount,
            RepetitionCount = repetitionCount,
            LongPauseCount = pauses.LongPauseCount,
            AveragePauseSeconds = pauses.AveragePauseSeconds,
            AnalysisJson = analysisJson
        };
    }

    private static double ToTenPointScore(double score)
    {
        return Math.Round(Math.Clamp(score, 0, 100) / 10, 1);
    }

    private static List<SpeakingAudioMetricDto> BuildWordMetrics(
        List<string> spokenWords,
        List<string> referenceWords,
        List<WhisperSegment> segments)
    {
        var result = new List<SpeakingAudioMetricDto>();

        var estimatedTimedWords =
            EstimateWordTimings(spokenWords, segments);

        for (var i = 0; i < estimatedTimedWords.Count; i++)
        {
            var spoken = estimatedTimedWords[i];

            var matchedReference = i < referenceWords.Count
                ? referenceWords[i]
                : null;

            var accuracy = matchedReference == null
                ? 60
                : CalculateWordSimilarity(
                    spoken.Word,
                    matchedReference);

            result.Add(new SpeakingAudioMetricDto
            {
                Word = spoken.Word,
                Start = spoken.Start,
                End = spoken.End,
                MatchedReferenceWord = matchedReference,
                AccuracyEstimate = accuracy,
                Issue = accuracy >= 80
                    ? null
                    : "Word differs from expected reference or may be unclear",
                Syllables = EstimateSyllables(spoken.Word),
                EstimatedPhonemes = EstimatePhonemes(spoken.Word)
            });
        }

        return result;
    }

    private static List<TimedWord> EstimateWordTimings(
        List<string> spokenWords,
        List<WhisperSegment> segments)
    {
        var result = new List<TimedWord>();

        if (!segments.Any())
        {
            for (var i = 0; i < spokenWords.Count; i++)
            {
                result.Add(new TimedWord
                {
                    Word = spokenWords[i],
                    Start = i,
                    End = i + 0.5
                });
            }

            return result;
        }

        foreach (var segment in segments)
        {
            var words = Tokenize(segment.Text);

            if (!words.Any())
            {
                continue;
            }

            var duration = Math.Max(
                0.1,
                segment.End - segment.Start);

            var step = duration / words.Count;

            for (var i = 0; i < words.Count; i++)
            {
                result.Add(new TimedWord
                {
                    Word = words[i],
                    Start = Math.Round(
                        segment.Start + step * i,
                        2),
                    End = Math.Round(
                        segment.Start + step * (i + 1),
                        2)
                });
            }
        }

        return result;
    }

    private static List<string> Tokenize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        return Regex.Matches(
                value.ToLower(),
                @"[a-z']+")
            .Cast<Match>()
            .Select(x => x.Value.Trim('\''))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static double GetDurationSeconds(
        List<WhisperSegment> segments)
    {
        if (!segments.Any())
        {
            return 0;
        }

        return Math.Max(
            0,
            segments.Max(x => x.End) - segments.Min(x => x.Start));
    }

    private static int CountFillers(List<string> words)
    {
        var fillers = new HashSet<string>
        {
            "um", "uh", "er", "ah", "like", "youknow"
        };

        return words.Count(x =>
            fillers.Contains(x.Replace(" ", "")));
    }

    private static int CountRepetitions(List<string> words)
    {
        var count = 0;

        for (var i = 1; i < words.Count; i++)
        {
            if (words[i] == words[i - 1])
            {
                count++;
            }
        }

        return count;
    }

    private static PauseMetric CalculatePauses(
        List<WhisperSegment> segments)
    {
        if (segments.Count <= 1)
        {
            return new PauseMetric();
        }

        var pauses = new List<double>();

        for (var i = 1; i < segments.Count; i++)
        {
            var pause = segments[i].Start -
                        segments[i - 1].End;

            if (pause > 0)
            {
                pauses.Add(pause);
            }
        }

        return new PauseMetric
        {
            LongPauseCount = pauses.Count(x => x >= 1.5),
            AveragePauseSeconds = pauses.Any()
                ? Math.Round(pauses.Average(), 2)
                : 0
        };
    }

    private static double CalculateCompletenessScore(
        List<string> spokenWords,
        List<string> referenceWords)
    {
        if (!referenceWords.Any())
        {
            return spokenWords.Any() ? 75 : 0;
        }

        var spokenSet = spokenWords.ToHashSet();

        var covered = referenceWords
            .Distinct()
            .Count(x => spokenSet.Contains(x));

        return Math.Round(
            (double)covered /
            referenceWords.Distinct().Count() * 100,
            2);
    }

    private static double CalculateAccuracyScore(
        List<string> spokenWords,
        List<string> referenceWords)
    {
        if (!referenceWords.Any())
        {
            return spokenWords.Any() ? 75 : 0;
        }

        var distance = LevenshteinDistance(
            spokenWords,
            referenceWords);

        var max = Math.Max(
            spokenWords.Count,
            referenceWords.Count);

        if (max == 0)
        {
            return 0;
        }

        return Math.Round(
            Math.Max(0, 100 - (double)distance / max * 100),
            2);
    }

    private static double CalculateFluencyScore(
        double wordsPerMinute,
        int fillerWordCount,
        int repetitionCount,
        int longPauseCount)
    {
        var score = 100.0;

        if (wordsPerMinute < 80)
        {
            score -= 25;
        }
        else if (wordsPerMinute < 110)
        {
            score -= 10;
        }
        else if (wordsPerMinute > 190)
        {
            score -= 10;
        }

        score -= fillerWordCount * 3;
        score -= repetitionCount * 3;
        score -= longPauseCount * 8;

        return Math.Round(
            Math.Clamp(score, 0, 100),
            2);
    }

    private static double CalculateProsodyEstimate(
        double wordsPerMinute,
        int longPauseCount,
        double averagePauseSeconds)
    {
        var score = 80.0;

        if (wordsPerMinute >= 110 &&
            wordsPerMinute <= 170)
        {
            score += 10;
        }

        score -= longPauseCount * 6;
        score -= averagePauseSeconds > 1
            ? 10
            : 0;

        return Math.Round(
            Math.Clamp(score, 0, 100),
            2);
    }

    private static double CalculateWordSimilarity(
        string source,
        string target)
    {
        var distance = LevenshteinDistance(
            source,
            target);

        var max = Math.Max(
            source.Length,
            target.Length);

        if (max == 0)
        {
            return 100;
        }

        return Math.Round(
            Math.Max(0, 100 - (double)distance / max * 100),
            2);
    }

    private static int LevenshteinDistance(
        List<string> source,
        List<string> target)
    {
        var dp = new int[source.Count + 1, target.Count + 1];

        for (var i = 0; i <= source.Count; i++)
        {
            dp[i, 0] = i;
        }

        for (var j = 0; j <= target.Count; j++)
        {
            dp[0, j] = j;
        }

        for (var i = 1; i <= source.Count; i++)
        {
            for (var j = 1; j <= target.Count; j++)
            {
                var cost = source[i - 1] == target[j - 1]
                    ? 0
                    : 1;

                dp[i, j] = Math.Min(
                    Math.Min(
                        dp[i - 1, j] + 1,
                        dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[source.Count, target.Count];
    }

    private static int LevenshteinDistance(
        string source,
        string target)
    {
        var dp = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; i++)
        {
            dp[i, 0] = i;
        }

        for (var j = 0; j <= target.Length; j++)
        {
            dp[0, j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1]
                    ? 0
                    : 1;

                dp[i, j] = Math.Min(
                    Math.Min(
                        dp[i - 1, j] + 1,
                        dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[source.Length, target.Length];
    }

    private static List<string> EstimateSyllables(string word)
    {
        var matches = Regex.Matches(
            word.ToLower(),
            @"[aeiouy]+");

        if (matches.Count == 0)
        {
            return new List<string> { word };
        }

        return matches
            .Cast<Match>()
            .Select(x => x.Value)
            .ToList();
    }

    private static List<string> EstimatePhonemes(string word)
    {
        return word
            .ToUpper()
            .Where(char.IsLetter)
            .Select(x => x.ToString())
            .ToList();
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();

        return extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".webm" => "audio/webm",
            _ => "application/octet-stream"
        };
    }

    private class WhisperSegment
    {
        public string Text { get; set; } = "";

        public double Start { get; set; }

        public double End { get; set; }
    }

    private class TimedWord
    {
        public string Word { get; set; } = "";

        public double Start { get; set; }

        public double End { get; set; }
    }

    private class PauseMetric
    {
        public int LongPauseCount { get; set; }

        public double AveragePauseSeconds { get; set; }
    }
}
