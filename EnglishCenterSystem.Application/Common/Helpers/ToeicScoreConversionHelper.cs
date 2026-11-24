using EnglishCenterSystem.Domain.Entities.Competitions;
using EnglishCenterSystem.Domain.Entities.Tests;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Common.Helpers;

public static class ToeicScoreConversionHelper
{
    private static readonly int[] ReadingScoreTable =
    {
        5, 5, 5, 10, 15, 20, 25, 30, 35, 40,
        45, 50, 55, 60, 65, 70, 75, 80, 85, 90,
        95, 100, 105, 110, 115, 120, 125, 130, 135, 140,
        145, 150, 155, 160, 165, 170, 175, 180, 185, 190,
        195, 200, 205, 210, 215, 220, 225, 230, 235, 240,
        245, 250, 255, 260, 265, 270, 275, 280, 285, 290,
        295, 300, 305, 310, 315, 320, 325, 330, 335, 340,
        345, 350, 355, 360, 365, 370, 375, 380, 385, 390,
        395, 400, 405, 410, 415, 420, 425, 430, 435, 440,
        445, 450, 455, 460, 465, 470, 475, 480, 485, 490,
        495
    };

    private static readonly int[] ListeningScoreTable =
    {
        5, 15, 20, 25, 30, 35, 40, 45, 50, 55,
        60, 65, 70, 75, 80, 85, 90, 95, 100, 105,
        110, 115, 120, 125, 130, 135, 140, 145, 150, 155,
        160, 165, 170, 175, 180, 185, 190, 195, 200, 205,
        210, 215, 220, 225, 230, 235, 240, 245, 250, 255,
        260, 265, 270, 275, 280, 285, 290, 295, 300, 305,
        310, 315, 320, 325, 330, 335, 340, 345, 350, 355,
        360, 365, 370, 375, 380, 385, 395, 400, 405, 410,
        415, 420, 425, 430, 435, 440, 445, 450, 455, 460,
        465, 470, 475, 480, 485, 490, 495, 495, 495, 495,
        495
    };

    private static readonly int[] SpeakingScoreTable =
    {
        0, 10, 10, 20, 20, 30, 30, 40, 40, 50,
        50, 60, 60, 70, 80, 80, 90, 90, 100, 100,
        110, 110, 120, 120, 130, 140, 140, 150, 150, 160,
        160, 170, 170, 180, 180, 190, 190, 200
    };

    private static readonly int[] WritingScoreTable =
    {
        0, 10, 10, 20, 30, 40, 40, 50, 60, 60,
        70, 80, 90, 90, 100, 110, 110, 120, 130, 140,
        140, 150, 160, 160, 170, 180, 190, 190, 200
    };

    public static bool IsToeic(TestType testType)
    {
        return testType is TestType.ToeicTwoSkills or TestType.ToeicFourSkills;
    }

    public static ToeicScoreBreakdown CalculateTestScore(
        Test test,
        IEnumerable<TestAnswer> answers)
    {
        var answerList = answers.ToList();
        var internalScore = Math.Round(answerList.Sum(x => x.PointEarned), 2);

        if (!IsToeic(test.TestType))
        {
            return ToeicScoreBreakdown.InternalOnly(internalScore);
        }

        var listeningCorrect = answerList.Count(x =>
            x.ListeningQuestionId != null &&
            x.IsCorrect);

        var readingCorrect = answerList.Count(x =>
            x.ReadingQuestionId != null &&
            x.IsCorrect);

        var listeningScore = ConvertListeningScore(listeningCorrect);
        var readingScore = ConvertReadingScore(readingCorrect);

        int? speakingScore = null;
        int? writingScore = null;

        if (test.TestType == TestType.ToeicFourSkills)
        {
            var speakingRawScore = CalculateTestSpeakingRawScore(
                test,
                answerList);

            var writingRawScore = CalculateTestWritingRawScore(
                test,
                answerList);

            speakingScore = ConvertSpeakingScore(speakingRawScore);
            writingScore = ConvertWritingScore(writingRawScore);
        }

        return ToeicScoreBreakdown.Toeic(
            internalScore,
            listeningScore,
            readingScore,
            speakingScore,
            writingScore);
    }

    public static ToeicScoreBreakdown CalculateCompetitionScore(
        CompetitionTest competitionTest,
        IEnumerable<CompetitionTestAnswer> answers)
    {
        var answerList = answers.ToList();
        var internalScore = Math.Round(answerList.Sum(x => x.PointEarned), 2);

        if (!IsToeic(competitionTest.TestType))
        {
            return ToeicScoreBreakdown.InternalOnly(internalScore);
        }

        var listeningCorrect = answerList.Count(x =>
            x.ListeningQuestionId != null &&
            x.IsCorrect);

        var readingCorrect = answerList.Count(x =>
            x.ReadingQuestionId != null &&
            x.IsCorrect);

        var listeningScore = ConvertListeningScore(listeningCorrect);
        var readingScore = ConvertReadingScore(readingCorrect);

        int? speakingScore = null;
        int? writingScore = null;

        if (competitionTest.TestType == TestType.ToeicFourSkills)
        {
            var speakingRawScore = CalculateCompetitionSpeakingRawScore(
                competitionTest,
                answerList);

            var writingRawScore = CalculateCompetitionWritingRawScore(
                competitionTest,
                answerList);

            speakingScore = ConvertSpeakingScore(speakingRawScore);
            writingScore = ConvertWritingScore(writingRawScore);
        }

        return ToeicScoreBreakdown.Toeic(
            internalScore,
            listeningScore,
            readingScore,
            speakingScore,
            writingScore);
    }

    public static int ConvertListeningScore(int rawScore)
    {
        return ListeningScoreTable[Clamp(rawScore, 0, 100)];
    }

    public static int ConvertReadingScore(int rawScore)
    {
        return ReadingScoreTable[Clamp(rawScore, 0, 100)];
    }

    public static int ConvertSpeakingScore(int rawScore)
    {
        return SpeakingScoreTable[Clamp(rawScore, 0, 37)];
    }

    public static int ConvertWritingScore(int rawScore)
    {
        return WritingScoreTable[Clamp(rawScore, 0, 28)];
    }

    private static int CalculateTestSpeakingRawScore(
        Test test,
        List<TestAnswer> answers)
    {
        var rawScore = test.SpeakingTasks
            .OrderBy(x => x.OrderIndex)
            .Select((link, index) =>
            {
                var answer = answers.FirstOrDefault(x =>
                    x.SpeakingTaskId == link.SpeakingTaskId);

                var scorePercent = answer?.OverallSpeakingScore;

                return ConvertPercentToRawScore(
                    scorePercent,
                    GetToeicSpeakingRawMax(index + 1));
            })
            .Sum();

        return ClampRawScore(rawScore, 37);
    }

    private static int CalculateTestWritingRawScore(
        Test test,
        List<TestAnswer> answers)
    {
        var rawScore = test.WritingTasks
            .OrderBy(x => x.OrderIndex)
            .Select((link, index) =>
            {
                var answer = answers.FirstOrDefault(x =>
                    x.WritingTaskId == link.WritingTaskId);

                var bandScore = answer?.WritingOverallBand ??
                                answer?.ManualScore;

                return ConvertBandToRawScore(
                    bandScore,
                    GetToeicWritingRawMax(index + 1));
            })
            .Sum();

        return ClampRawScore(rawScore, 28);
    }

    private static int CalculateCompetitionSpeakingRawScore(
        CompetitionTest competitionTest,
        List<CompetitionTestAnswer> answers)
    {
        var rawScore = competitionTest.SpeakingTasks
            .OrderBy(x => x.OrderIndex)
            .Select((link, index) =>
            {
                var answer = answers.FirstOrDefault(x =>
                    x.SpeakingTaskId == link.SpeakingTaskId);

                var scorePercent = answer?.OverallSpeakingScore;

                return ConvertPercentToRawScore(
                    scorePercent,
                    GetToeicSpeakingRawMax(index + 1));
            })
            .Sum();

        return ClampRawScore(rawScore, 37);
    }

    private static int CalculateCompetitionWritingRawScore(
        CompetitionTest competitionTest,
        List<CompetitionTestAnswer> answers)
    {
        var rawScore = competitionTest.WritingTasks
            .OrderBy(x => x.OrderIndex)
            .Select((link, index) =>
            {
                var answer = answers.FirstOrDefault(x =>
                    x.WritingTaskId == link.WritingTaskId);

                var bandScore = answer?.WritingOverallBand ??
                                answer?.ManualScore;

                return ConvertBandToRawScore(
                    bandScore,
                    GetToeicWritingRawMax(index + 1));
            })
            .Sum();

        return ClampRawScore(rawScore, 28);
    }

    private static int GetToeicSpeakingRawMax(int orderIndex)
    {
        return orderIndex == 11
            ? 5
            : 3;
    }

    private static int GetToeicWritingRawMax(int orderIndex)
    {
        if (orderIndex <= 5)
        {
            return 3;
        }

        if (orderIndex <= 7)
        {
            return 4;
        }

        return 5;
    }

    private static double ConvertPercentToRawScore(
        double? scorePercent,
        int rawMax)
    {
        if (!scorePercent.HasValue)
        {
            return 0;
        }

        return Clamp(scorePercent.Value, 0, 100) / 100 * rawMax;
    }

    private static double ConvertBandToRawScore(
        double? bandScore,
        int rawMax)
    {
        if (!bandScore.HasValue)
        {
            return 0;
        }

        return Clamp(bandScore.Value, 0, 10) / 10 * rawMax;
    }

    private static int ClampRawScore(
        double rawScore,
        int maxRawScore)
    {
        return Clamp(
            Convert.ToInt32(
                Math.Round(
                    rawScore,
                    MidpointRounding.AwayFromZero)),
            0,
            maxRawScore);
    }

    private static int Clamp(
        int value,
        int min,
        int max)
    {
        return Math.Min(
            Math.Max(value, min),
            max);
    }

    private static double Clamp(
        double value,
        double min,
        double max)
    {
        return Math.Min(
            Math.Max(value, min),
            max);
    }
}

public class ToeicScoreBreakdown
{
    public double InternalScore { get; init; }

    public double ScoreForRanking { get; init; }

    public int? ToeicListeningScore { get; init; }

    public int? ToeicReadingScore { get; init; }

    public int? ToeicSpeakingScore { get; init; }

    public int? ToeicWritingScore { get; init; }

    public int? ToeicTotalScore { get; init; }

    public static ToeicScoreBreakdown InternalOnly(
        double internalScore)
    {
        return new ToeicScoreBreakdown
        {
            InternalScore = internalScore,
            ScoreForRanking = internalScore
        };
    }

    public static ToeicScoreBreakdown Toeic(
        double internalScore,
        int listeningScore,
        int readingScore,
        int? speakingScore,
        int? writingScore)
    {
        var totalScore = listeningScore + readingScore +
            (speakingScore ?? 0) +
            (writingScore ?? 0);

        return new ToeicScoreBreakdown
        {
            InternalScore = internalScore,
            ScoreForRanking = totalScore,
            ToeicListeningScore = listeningScore,
            ToeicReadingScore = readingScore,
            ToeicSpeakingScore = speakingScore,
            ToeicWritingScore = writingScore,
            ToeicTotalScore = totalScore
        };
    }
}
