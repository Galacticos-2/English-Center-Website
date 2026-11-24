using System.Text.RegularExpressions;
using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Application.Interfaces.Services;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Infrastructure.Services;

public class TestDocumentSplitService : ITestDocumentSplitService
{
    public Task<TestDocumentSplitResultDto> SplitAsync(
        string rawText,
        TestType testType)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new Exception("Test document has no readable text");
        }

        var normalizedText = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();

        normalizedText = NormalizeInlineSkillMarkers(
            normalizedText);

        var result = testType switch
        {
            TestType.ToeicTwoSkills =>
                SplitToeicTwoSkills(normalizedText),

            TestType.VstepFourSkills =>
                SplitFourSkills(normalizedText),

            TestType.ToeicFourSkills =>
                SplitToeicFourSkills(normalizedText),

            _ => throw new Exception("Invalid test type")
        };

        result = NormalizeSkillAnswerMapping(
    normalizedText,
    result);

        return Task.FromResult(result);
    }
    private TestDocumentSplitResultDto NormalizeSkillAnswerMapping(
    string fullText,
    TestDocumentSplitResultDto result)
    {
        result.ListeningText = NormalizeOneSkillText(
            fullText,
            result.ListeningText,
            "Listening",
            new[] { "Reading", "Writing", "Speaking" });

        result.ReadingText = NormalizeOneSkillText(
            fullText,
            result.ReadingText,
            "Reading",
            new[] { "Writing", "Speaking" });

        return result;
    }

    private string? NormalizeOneSkillText(
    string fullText,
    string? skillText,
    string skillName,
    string[] nextSkillNames)
    {
        if (string.IsNullOrWhiteSpace(skillText))
        {
            return skillText;
        }

        if (HasAnswerKeyInside(skillText))
        {
            return skillText;
        }

        var answerFromNamedSkillSection =
            ExtractNamedSkillAnswerSection(
                fullText,
                skillName);

        if (!string.IsNullOrWhiteSpace(
                answerFromNamedSkillSection))
        {
            var normalizedAnswer =
                NormalizeNamedSkillAnswerText(
                    answerFromNamedSkillSection,
                    skillName);

            return skillText.Trim()
                + "\n\nAnswer Key:\n"
                + normalizedAnswer;
        }

        var answerFromGlobalAnswerSection =
            ExtractAnswerFromGlobalAnswerSection(
                fullText,
                skillName,
                nextSkillNames);

        if (!string.IsNullOrWhiteSpace(answerFromGlobalAnswerSection))
        {
            var normalizedAnswer =
                NormalizeAnswerKeyText(answerFromGlobalAnswerSection);

            return skillText.Trim()
                + "\n\nAnswer Key:\n"
                + normalizedAnswer;
        }

        var answerFromInlineSection =
            ExtractInlineAnswerNearSkill(
                fullText,
                skillName,
                nextSkillNames);

        if (!string.IsNullOrWhiteSpace(answerFromInlineSection))
        {
            var normalizedAnswer =
                NormalizeAnswerKeyText(answerFromInlineSection);

            return skillText.Trim()
                + "\n\nAnswer Key:\n"
                + normalizedAnswer;
        }

        return skillText;
    }
    private string NormalizeAnswerKeyText(
        string answerText,
        int? minQuestionNumber = null,
        int? maxQuestionNumber = null)
    {
        if (string.IsNullOrWhiteSpace(answerText))
        {
            return "";
        }

        var matches = Regex.Matches(
                answerText,
                @"(?im)\b(\d+)\s*[\.\)]\s*([A-D])(?=\s|$|[A-Z][a-z])")
            .Cast<Match>()
            .Select(x => new
            {
                Number = int.Parse(x.Groups[1].Value),
                Answer = x.Groups[2].Value.ToUpper()
            })
            .Where(x =>
                (!minQuestionNumber.HasValue ||
                 x.Number >= minQuestionNumber.Value) &&
                (!maxQuestionNumber.HasValue ||
                 x.Number <= maxQuestionNumber.Value))
            .DistinctBy(x => x.Number)
            .OrderBy(x => x.Number)
            .ToList();

        if (!matches.Any())
        {
            return answerText.Trim();
        }

        return string.Join(
            "\n",
            matches.Select(x => $"{x.Number}. {x.Answer}"));
    }

    private string NormalizeNamedSkillAnswerText(
        string answerText,
        string skillName)
    {
        if (skillName.Equals(
                "Listening",
                StringComparison.OrdinalIgnoreCase))
        {
            var toeicAnswerMap =
                ExtractToeicListeningAnswerMap(
                    answerText);

            if (toeicAnswerMap.Count >= 80)
            {
                return string.Join(
                    "\n",
                    toeicAnswerMap
                        .OrderBy(x => x.Key)
                        .Select(x =>
                            $"{x.Key}. {x.Value}"));
            }

            var normalizedAnswer =
                NormalizeAnswerKeyText(
                    answerText,
                    1,
                    100);

            var normalizedAnswerCount = Regex.Matches(
                    normalizedAnswer,
                    @"(?m)^\s*\d{1,3}\s*[\.\)]\s*[A-D]\s*$",
                    RegexOptions.IgnoreCase)
                .Count;

            if (normalizedAnswerCount >= 80)
            {
                return normalizedAnswer;
            }

            return NormalizeAnswerKeyText(
                answerText,
                1,
                100);
        }

        return NormalizeAnswerKeyText(answerText);
    }

    private Dictionary<int, string>
        ExtractToeicListeningAnswerMap(
            string answerText)
    {
        var result =
            new Dictionary<int, string>();

        var partRanges = new[]
        {
            new { Part = 1, Start = 1, End = 6 },
            new { Part = 2, Start = 7, End = 31 },
            new { Part = 3, Start = 32, End = 70 },
            new { Part = 4, Start = 71, End = 100 }
        };

        foreach (var partRange in partRanges)
        {
            var partText = ExtractToeicListeningPartAnswerText(
                answerText,
                partRange.Part);

            partText = NormalizeToeicListeningAnswerQuestionMarkers(
                partText,
                partRange.Start,
                partRange.End);

            if (string.IsNullOrWhiteSpace(partText))
            {
                continue;
            }

            for (var groupStart = partRange.Start;
                 groupStart <= partRange.End;
                 groupStart += 5)
            {
                var groupEnd = Math.Min(
                    groupStart + 4,
                    partRange.End);

                AddToeicListeningAnswerGroup(
                    partText,
                    groupStart,
                    groupEnd,
                    result);
            }
        }

        return result;
    }

    private string ExtractToeicListeningPartAnswerText(
        string answerText,
        int partNumber)
    {
        var startIndex = 0;

        var partHeading = Regex.Match(
            answerText,
            $@"(?im)^\s*Part\s+{partNumber}\s*:?\s*$");

        if (partHeading.Success)
        {
            startIndex =
                partHeading.Index +
                partHeading.Length;
        }
        else if (partNumber != 1)
        {
            return "";
        }

        var transcriptHeading = Regex.Match(
            answerText.Substring(startIndex),
            @"(?im)^\s*Transcript(?:\s+part\s+\d+)?\s*:?\s*$");

        var endIndex = transcriptHeading.Success
            ? startIndex + transcriptHeading.Index
            : answerText.Length;

        return answerText
            .Substring(
                startIndex,
                endIndex - startIndex)
            .Trim();
    }

    private string NormalizeToeicListeningAnswerQuestionMarkers(
        string partText,
        int startQuestionNumber,
        int endQuestionNumber)
    {
        if (string.IsNullOrWhiteSpace(partText))
        {
            return partText;
        }

        for (var questionNumber = startQuestionNumber;
             questionNumber <= endQuestionNumber;
             questionNumber++)
        {
            var digits = questionNumber.ToString();

            if (digits.Length < 2)
            {
                continue;
            }

            var splitMarkerPattern =
                string.Join(
                    @"\s+",
                    digits.Select(digit =>
                        Regex.Escape(digit.ToString())));

            partText = Regex.Replace(
                partText,
                $@"(?<!\d){splitMarkerPattern}\s*([\.\)])",
                $"{digits}$1");
        }

        return partText;
    }

    private void AddToeicListeningAnswerGroup(
        string partText,
        int groupStart,
        int groupEnd,
        Dictionary<int, string> answerMap)
    {
        var startMatch = Regex.Match(
            partText,
            $@"(?<!\d){groupStart}\s*[\.\)]");

        if (!startMatch.Success)
        {
            return;
        }

        var nextGroupStart = groupEnd + 1;
        var nextMatch = Regex.Match(
            partText.Substring(
                startMatch.Index +
                startMatch.Length),
            $@"(?<!\d){nextGroupStart}\s*[\.\)]");

        var groupTextEnd = nextMatch.Success
            ? startMatch.Index +
              startMatch.Length +
              nextMatch.Index
            : partText.Length;

        var groupText = partText.Substring(
            startMatch.Index,
            groupTextEnd - startMatch.Index);

        var presentQuestionNumbers = Regex.Matches(
                groupText,
                @"(?<!\d)(\d{1,3})\s*[\.\)]")
            .Cast<Match>()
            .Select(match =>
                int.Parse(match.Groups[1].Value))
            .Where(questionNumber =>
                questionNumber >= groupStart &&
                questionNumber <= groupEnd)
            .Distinct()
            .OrderBy(questionNumber =>
                questionNumber)
            .ToList();

        var directAnswers = presentQuestionNumbers
            .Select(questionNumber =>
            {
                var match = Regex.Match(
                    groupText,
                    $@"(?<!\d){questionNumber}\s*[\.\)]\s*([A-D])(?=\s|$|[A-Z][a-z])",
                    RegexOptions.IgnoreCase);

                return new
                {
                    QuestionNumber = questionNumber,
                    Match = match
                };
            })
            .Where(x => x.Match.Success)
            .ToList();

        foreach (var directAnswer in directAnswers)
        {
            answerMap[directAnswer.QuestionNumber] =
                directAnswer.Match
                    .Groups[1]
                    .Value
                    .ToUpperInvariant();
        }

        if (directAnswers.Count ==
            presentQuestionNumbers.Count)
        {
            return;
        }

        var answerLabels = Regex.Matches(
                groupText,
                @"(?<![A-Za-z])([A-D])(?![A-Za-z])",
                RegexOptions.IgnoreCase)
            .Cast<Match>()
            .Select(match =>
                match.Groups[1]
                    .Value
                    .ToUpperInvariant())
            .ToList();

        if (answerLabels.Count <
            presentQuestionNumbers.Count)
        {
            return;
        }

        answerMap.TryAdd(
            presentQuestionNumbers[0],
            answerLabels[0]);

        if (presentQuestionNumbers.Count == 5)
        {
            answerMap.TryAdd(
                presentQuestionNumbers[4],
                answerLabels[1]);

            for (var index = 1; index <= 3; index++)
            {
                answerMap.TryAdd(
                    presentQuestionNumbers[index],
                    answerLabels[index + 1]);
            }

            return;
        }

        for (var index = 1;
             index < presentQuestionNumbers.Count;
             index++)
        {
            answerMap.TryAdd(
                presentQuestionNumbers[index],
                answerLabels[index]);
        }
    }
    private bool HasAnswerKeyInside(string text)
    {
        return Regex.IsMatch(
            text,
            @"(?im)^\s*(Answer\s*Key|Answers?|Đáp\s*án|Đáp\s*án\s*gợi\s*ý)\s*:?\s*$");
    }

    private string ExtractAnswerFromGlobalAnswerSection(
        string fullText,
        string skillName,
        string[] nextSkillNames)
    {
        var globalAnswerSection = ExtractGlobalAnswerSection(fullText);

        if (string.IsNullOrWhiteSpace(globalAnswerSection))
        {
            return "";
        }

        return ExtractSkillAnswerText(
            globalAnswerSection,
            skillName,
            nextSkillNames);
    }

    private string ExtractNamedSkillAnswerSection(
        string fullText,
        string skillName)
    {
        var answerHeadingPattern =
            $@"(?:Answer\s+for\s+(?:TOEIC\s+)?{Regex.Escape(skillName)}|(?:TOEIC\s+)?{Regex.Escape(skillName)}\s+Answers?)";

        var answerHeading = Regex.Match(
            fullText,
            $@"(?im)^\s*{answerHeadingPattern}\s*:?\s*(?:Part\s+\d+\s*:?)?\s*$");

        if (!answerHeading.Success)
        {
            answerHeading = Regex.Match(
                fullText,
                $@"(?im)^\s*{answerHeadingPattern}\s*:\s*(?=\S)");

            if (!answerHeading.Success)
            {
                return "";
            }
        }

        var contentStart =
            answerHeading.Index +
            answerHeading.Length;

        var nextAnswerHeading = Regex.Match(
            fullText.Substring(contentStart),
            @"(?im)^\s*(?:Answer\s+for\s+(?:TOEIC\s+)?(?:Listening|Reading|Writing|Speaking)|(?:TOEIC\s+)?(?:Listening|Reading|Writing|Speaking)\s+Answers?)\s*:?\s*(?:Part\s+\d+\s*:?)?\s*$");

        var contentEnd = nextAnswerHeading.Success
            ? contentStart + nextAnswerHeading.Index
            : fullText.Length;

        return fullText
            .Substring(
                contentStart,
                contentEnd - contentStart)
            .Trim();
    }

    private string ExtractGlobalAnswerSection(string fullText)
    {
        var match = Regex.Match(
            fullText,
            @"(?im)^\s*(Đáp\s*án\s*gợi\s*ý|Answer\s*Key|Answers)\s*:?\s*$");

        if (!match.Success)
        {
            return "";
        }

        return fullText
            .Substring(match.Index + match.Length)
            .Trim();
    }
    private string ExtractSkillAnswerText(
    string answerSection,
    string skillName,
    string[] nextSkillNames)
    {
        var lines = answerSection
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n');

        var isCollecting = false;
        var collectedLines = new List<string>();

        foreach (var line in lines)
        {
            if (!isCollecting)
            {
                if (!TryReadSkillAnswerLine(
                        line,
                        skillName,
                        out var firstLineContent))
                {
                    continue;
                }

                isCollecting = true;

                if (!string.IsNullOrWhiteSpace(firstLineContent))
                {
                    collectedLines.Add(firstLineContent.Trim());
                }

                continue;
            }

            if (nextSkillNames.Any(nextSkillName =>
                    IsSkillAnswerLine(line, nextSkillName)))
            {
                break;
            }

            collectedLines.Add(line);
        }

        return string.Join("\n", collectedLines).Trim();
    }

    private bool TryReadSkillAnswerLine(
        string line,
        string skillName,
        out string content)
    {
        content = "";

        var match = Regex.Match(
            line,
            $@"^\s*(?:TOEIC\s+)?{Regex.Escape(skillName)}(?:\s*\([^)]*\))?(?:\s+(?:TEST|SECTION))?(?<content>(?:\s+|\s*:).*)?$",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return false;
        }

        content = match.Groups["content"].Success
            ? match.Groups["content"].Value.Trim().TrimStart(':').Trim()
            : "";

        return true;
    }

    private bool IsSkillAnswerLine(
        string line,
        string skillName)
    {
        return Regex.IsMatch(
            line,
            $@"^\s*(?:TOEIC\s+)?{Regex.Escape(skillName)}(?:\s|\(|:|$)",
            RegexOptions.IgnoreCase);
    }

    private string ExtractInlineAnswerNearSkill(
        string fullText,
        string skillName,
        string[] nextSkillNames)
    {
        var skillBlock = ExtractSkillBlock(
            fullText,
            skillName,
            nextSkillNames);

        if (string.IsNullOrWhiteSpace(skillBlock))
        {
            return "";
        }

        var answerMatch = Regex.Match(
            skillBlock,
            @"(?is)(Answer\s*Key|Answers?|Đáp\s*án|Đáp\s*án\s*gợi\s*ý)\s*:?\s*(?<content>.*)$");

        if (!answerMatch.Success)
        {
            return "";
        }

        return answerMatch.Groups["content"].Value.Trim();
    }

    private string ExtractSkillBlock(
    string fullText,
    string skillName,
    string[] nextSkillNames)
    {
        var startMatch = Regex.Match(
            fullText,
            $@"(?im)^\s*(?:TOEIC\s+)?{Regex.Escape(skillName)}(?:\s*\([^)]*\))?(?:\s+(?:TEST|SECTION))?\s*:?\s*$");

        if (!startMatch.Success)
        {
            return "";
        }

        var startIndex = startMatch.Index;
        var searchStartIndex = startMatch.Index + startMatch.Length;
        var endIndex = GetContentEndBeforeAnswerSection(fullText);

        if (endIndex <= startIndex)
        {
            endIndex = fullText.Length;
        }

        foreach (var nextSkillName in nextSkillNames)
        {
            var nextMatch = Regex.Match(
    fullText.Substring(searchStartIndex),
    $@"(?im)^\s*(?:TOEIC\s+)?{Regex.Escape(nextSkillName)}(?:\s*\([^)]*\))?(?:\s+(?:TEST|SECTION))?\s*:?\s*$");

            if (nextMatch.Success)
            {
                var absoluteNextIndex =
                    searchStartIndex + nextMatch.Index;

                if (absoluteNextIndex > startIndex &&
                    absoluteNextIndex < endIndex)
                {
                    endIndex = absoluteNextIndex;
                }

                break;
            }
        }

        if (endIndex <= startIndex)
        {
            return "";
        }

        return fullText
            .Substring(startIndex, endIndex - startIndex)
            .Trim();
    }
    private TestDocumentSplitResultDto SplitFourSkills(string text)
    {
        var sections = FindSkillSections(text);

        return new TestDocumentSplitResultDto
        {
            ListeningText = sections.GetValueOrDefault("listening"),
            ReadingText = sections.GetValueOrDefault("reading"),
            WritingText = sections.GetValueOrDefault("writing"),
            SpeakingText = sections.GetValueOrDefault("speaking")
        };
    }

    private TestDocumentSplitResultDto SplitToeicTwoSkills(string text)
    {
        var sections = FindToeicSkillSections(text);
        var toeicListeningReading = SplitToeicByParts(text);
        var listeningText = sections.GetValueOrDefault("listening");
        var readingText = SelectMostCompleteToeicReadingText(
            sections.GetValueOrDefault("reading"),
            toeicListeningReading.ReadingText);

        if (!string.IsNullOrWhiteSpace(listeningText) &&
            !string.IsNullOrWhiteSpace(readingText))
        {
            return new TestDocumentSplitResultDto
            {
                ListeningText = listeningText,
                ReadingText = readingText
            };
        }

        return new TestDocumentSplitResultDto
        {
            ListeningText = listeningText
                ?? toeicListeningReading.ListeningText,
            ReadingText = readingText
        };
    }

    private TestDocumentSplitResultDto SplitToeicFourSkills(string text)
    {
        var sections = FindToeicSkillSections(text);
        var toeicListeningReading =
            SplitToeicByParts(text);
        var toeicSpeakingWriting =
            SplitToeicSpeakingWritingByTaskHeadings(text);

        return new TestDocumentSplitResultDto
        {
            ListeningText =
                sections.GetValueOrDefault("listening")
                ?? toeicListeningReading.ListeningText,

            ReadingText =
                SelectMostCompleteToeicReadingText(
                    sections.GetValueOrDefault("reading"),
                    toeicListeningReading.ReadingText),

            WritingText =
                sections.GetValueOrDefault("writing")
                ?? toeicSpeakingWriting.WritingText,

            SpeakingText =
                sections.GetValueOrDefault("speaking")
                ?? toeicSpeakingWriting.SpeakingText
        };
    }

    private static string? SelectMostCompleteToeicReadingText(
        params string?[] candidates)
    {
        return candidates
            .Where(candidate =>
                !string.IsNullOrWhiteSpace(candidate))
            .Select(candidate => candidate!.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(GetToeicReadingCoverageScore)
            .ThenByDescending(candidate => candidate.Length)
            .FirstOrDefault();
    }

    private static int GetToeicReadingCoverageScore(string text)
    {
        var partCount = Regex.Matches(
                text,
                @"(?im)^\s*PART\s*([5-7])\b")
            .Cast<Match>()
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .Count();

        var questionCount = Regex.Matches(
                text,
                @"(?m)^\s*(\d{1,3})\s*[\.\)]\s+")
            .Cast<Match>()
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .Count();

        return partCount * 1000 + questionCount;
    }

    private TestDocumentSplitResultDto SplitToeicSpeakingWritingByTaskHeadings(
        string text)
    {
        var contentText = text;

        var speakingStart = FindToeicSpeakingStart(contentText);
        var writingStart = FindToeicWritingStart(contentText);
        var listeningStart = FindToeicListeningStart(contentText);

        if (speakingStart < 0 &&
            writingStart < 0)
        {
            return new TestDocumentSplitResultDto();
        }

        string? speakingText = null;
        string? writingText = null;

        if (speakingStart >= 0)
        {
            var speakingEnd =
                FindNearestNextIndex(
                    speakingStart,
                    writingStart,
                    listeningStart,
                    contentText.Length);

            speakingText = contentText
                .Substring(
                    speakingStart,
                    speakingEnd - speakingStart)
                .Trim();
        }

        if (writingStart >= 0)
        {
            var writingEnd =
                FindNearestNextIndex(
                    writingStart,
                    speakingStart,
                    listeningStart,
                    contentText.Length);

            writingText = contentText
                .Substring(
                    writingStart,
                    writingEnd - writingStart)
                .Trim();
        }

        return new TestDocumentSplitResultDto
        {
            SpeakingText =
                string.IsNullOrWhiteSpace(speakingText)
                    ? null
                    : speakingText,

            WritingText =
                string.IsNullOrWhiteSpace(writingText)
                    ? null
                    : writingText
        };
    }

    private int FindToeicSpeakingStart(string text)
    {
        var match = Regex.Match(
            text,
            @"(?im)^\s*(?:(?:TOEIC\s+)?Speaking(?:\s+(?:Test|Section))?|Questions?\s+1\s*[-–—]\s*2\s*:?\s*Read\s+a\s+text\s+aloud|Read\s+a\s+text\s+aloud)\b.*$");

        return match.Success
            ? match.Index
            : -1;
    }

    private int FindToeicListeningStart(string text)
    {
        var match = Regex.Match(
            text,
            @"(?im)^\s*(?:(?:TOEIC\s+)?Listening(?:\s+(?:Practice\s+)?(?:Test|Section))?|(?:TOEIC\s+)?Listening\s+Practice\s+Test|Part\s*1\s*:?\s*Directions)\b.*$");

        return match.Success
            ? match.Index
            : -1;
    }

    private int FindToeicWritingStart(string text)
    {
        var match = Regex.Match(
            text,
            @"(?im)^\s*(?:(?:TOEIC\s+)?Writing(?:\s+(?:Test|Section))?|Questions?\s+1\s*[-–—]\s*5\s*:?\s*Write\s+a\s+sentence(?:\s+based\s+on\s+a\s+picture)?|Write\s+a\s+sentence\s+based\s+on\s+a\s+picture)\b.*$");

        return match.Success
            ? match.Index
            : -1;
    }

    private static int FindNearestNextIndex(
        int currentIndex,
        int firstCandidate,
        int secondCandidate,
        int fallbackIndex)
    {
        var candidates = new[]
            {
                firstCandidate,
                secondCandidate
            }
            .Where(index => index > currentIndex)
            .ToList();

        return candidates.Any()
            ? candidates.Min()
            : fallbackIndex;
    }

    private string NormalizeInlineSkillMarkers(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return Regex.Replace(
            text,
            @"(?im)(?<!^)(?<!\n)\s+(?<marker>(?:TOEIC\s+)?(?:LISTENING|READING|WRITING|SPEAKING)(?:\s*\([^)]*\))?(?:\s+(?:TEST|SECTION))?)(?=\s*(?:Time\s*:|Directions?\s*:|Part\s+\d+|Task\s+\d+|Questions?\s+\d+|Number\s+of\s+questions\s*:))",
            match => "\n" +
                     match.Groups["marker"].Value.Trim() +
                     "\n");
    }

    private Dictionary<string, string> FindSkillSections(string text)
    {
        var contentEnd = GetContentEndBeforeAnswerSection(text);

        var contentText = text.Substring(0, contentEnd);

        var markerPattern =
    @"(?im)^\s*(?:TOEIC\s+)?(LISTENING|READING|WRITING|SPEAKING)(?:\s*\([^)]*\))?(?:\s+(?:TEST|SECTION))?\s*:?\s*$";

        var matches = Regex.Matches(contentText, markerPattern)
            .Cast<Match>()
            .Select(x => new
            {
                Skill = NormalizeSkillName(x.Groups[1].Value),
                Index = x.Index,
                Length = x.Length
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Skill))
            .OrderBy(x => x.Index)
            .ToList();

        var result = new Dictionary<string, string>();

        for (var i = 0; i < matches.Count; i++)
        {
            var current = matches[i];

            var start = current.Index + current.Length;

            var end = i + 1 < matches.Count
                ? matches[i + 1].Index
                : contentText.Length;

            if (end <= start)
            {
                continue;
            }

            var sectionText = contentText
                .Substring(start, end - start)
                .Trim();

            if (!string.IsNullOrWhiteSpace(sectionText))
            {
                result[current.Skill] = sectionText;
            }
        }

        return result;
    }

    private Dictionary<string, string> FindToeicSkillSections(
        string text)
    {
        var markerPattern =
            @"(?im)^\s*(?:TOEIC\s+)?(LISTENING|READING|WRITING|SPEAKING)(?:\s*\([^)]*\))?(?:\s+(?:PRACTICE\s+)?(?:TEST|SECTION))?\s*:?\s*$";

        var matches = Regex.Matches(text, markerPattern)
            .Cast<Match>()
            .Select(match => new
            {
                Skill = NormalizeSkillName(
                    match.Groups[1].Value),
                match.Index,
                match.Length
            })
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Skill))
            .OrderBy(item => item.Index)
            .ToList();

        var result = new Dictionary<string, string>();

        for (var index = 0; index < matches.Count; index++)
        {
            var current = matches[index];
            var start = current.Index + current.Length;
            var end = index + 1 < matches.Count
                ? matches[index + 1].Index
                : text.Length;

            if (end <= start)
            {
                continue;
            }

            var sectionText = text
                .Substring(start, end - start)
                .Trim();

            if (!string.IsNullOrWhiteSpace(sectionText))
            {
                result[current.Skill] = sectionText;
            }
        }

        return result;
    }
    private int GetContentEndBeforeAnswerSection(string text)
    {
        var answerMatch = Regex.Match(
            text,
            AnswerSectionHeadingPattern);

        if (!answerMatch.Success)
        {
            return text.Length;
        }

        return answerMatch.Index;
    }

    private const string AnswerSectionHeadingPattern =
        @"(?im)^\s*(Đáp\s*án\s*gợi\s*ý|Answer\s*Key|Answers|Answer\s+for\s+(?:TOEIC\s+)?(?:Listening|Reading|Writing|Speaking)|(?:TOEIC\s+)?(?:Listening|Reading|Writing|Speaking)\s+Answers?)\s*:?\s*(?:Part\s+\d+\s*:?)?\s*$";

    private TestDocumentSplitResultDto SplitToeicByParts(string text)
    {
        var contentText = text;

        var partMatches = Regex.Matches(
                contentText,
                @"(?i)\bPART\s*([1-7])\b")
            .Cast<Match>()
            .OrderBy(x => x.Index)
            .ToList();

        if (!partMatches.Any())
        {
            return new TestDocumentSplitResultDto();
        }

        var listeningStart = partMatches
            .FirstOrDefault(x =>
                int.Parse(x.Groups[1].Value) >= 1 &&
                int.Parse(x.Groups[1].Value) <= 4);

        var readingStart = partMatches
            .FirstOrDefault(x =>
                int.Parse(x.Groups[1].Value) >= 5 &&
                int.Parse(x.Groups[1].Value) <= 7);

        string? listeningText = null;
        string? readingText = null;

        if (listeningStart != null)
        {
            var listeningEnd =
                readingStart != null &&
                readingStart.Index > listeningStart.Index
                    ? readingStart.Index
                    : contentText.Length;

            listeningText = contentText
                .Substring(
                    listeningStart.Index,
                    listeningEnd - listeningStart.Index)
                .Trim();
        }

        if (readingStart != null)
        {
            var readingEnd = FindToeicReadingEnd(
                contentText,
                readingStart.Index);

            readingText = contentText
                .Substring(
                    readingStart.Index,
                    readingEnd - readingStart.Index)
                .Trim();
        }

        return new TestDocumentSplitResultDto
        {
            ListeningText =
                string.IsNullOrWhiteSpace(listeningText)
                    ? null
                    : listeningText,

            ReadingText =
                string.IsNullOrWhiteSpace(readingText)
                    ? null
                    : readingText
        };
    }

    private int FindToeicReadingEnd(
        string contentText,
        int readingStartIndex)
    {
        var speakingStart =
            FindToeicSpeakingStart(contentText);
        var writingStart =
            FindToeicWritingStart(contentText);

        var nextSkillStarts = new[]
            {
                speakingStart,
                writingStart
            }
            .Where(x => x > readingStartIndex)
            .ToList();

        return nextSkillStarts.Any()
            ? nextSkillStarts.Min()
            : contentText.Length;
    }

    private string NormalizeSkillName(string value)
    {
        value = value.Trim().ToLower();

        if (value.Contains("listening"))
        {
            return "listening";
        }

        if (value.Contains("reading"))
        {
            return "reading";
        }

        if (value.Contains("writing"))
        {
            return "writing";
        }

        if (value.Contains("speaking"))
        {
            return "speaking";
        }

        return "";
    }
}
