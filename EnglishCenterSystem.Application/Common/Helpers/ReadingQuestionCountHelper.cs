using System.Text.RegularExpressions;

namespace EnglishCenterSystem.Application.Common.Helpers;

public static class ReadingQuestionCountHelper
{
    public static int? DetectExpectedQuestionCount(
        string? readingText)
    {
        if (string.IsNullOrWhiteSpace(readingText))
        {
            return null;
        }

        var questionNumbers =
            new HashSet<int>();

        var rangeMatches = Regex.Matches(
            readingText,
            @"(?im)\bquestions?\s+(\d+)\s*[-–—]\s*(\d+)\b");

        foreach (Match match in rangeMatches)
        {
            if (!int.TryParse(
                    match.Groups[1].Value,
                    out var startNumber) ||
                !int.TryParse(
                    match.Groups[2].Value,
                    out var endNumber))
            {
                continue;
            }

            if (startNumber <= 0 ||
                endNumber < startNumber)
            {
                continue;
            }

            for (var number = startNumber;
                 number <= endNumber;
                 number++)
            {
                questionNumbers.Add(number);
            }
        }

        if (questionNumbers.Count > 0)
        {
            return questionNumbers.Count;
        }

        var answerMatches = Regex.Matches(
                readingText,
                @"(?im)\b(\d+)\s*[\.\)]\s*([A-D])\b")
            .Cast<Match>()
            .Select(match =>
                int.TryParse(
                    match.Groups[1].Value,
                    out var number)
                    ? number
                    : 0)
            .Where(number => number > 0)
            .Distinct()
            .OrderBy(number => number)
            .ToList();

        if (answerMatches.Count > 0)
        {
            return answerMatches.Count;
        }

        return null;
    }
}
