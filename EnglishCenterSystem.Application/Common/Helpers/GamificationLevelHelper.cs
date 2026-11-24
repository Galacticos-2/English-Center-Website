namespace EnglishCenterSystem.Application.Common.Helpers;

public static class GamificationLevelHelper
{
    private static readonly int[] RequiredExpByLevel =
    {
        0, 100, 125, 150, 175, 200, 225, 250, 275, 300,
        325, 350, 375, 400, 425, 450, 475, 500, 525, 550,
        575, 600, 625, 650, 675, 700, 750, 800, 900, 1000
    };

    public static int ChampionTotalExp => RequiredExpByLevel.Sum();

    public static int GetLevel(int totalExp)
    {
        var accumulatedExp = 0;
        var level = 1;

        for (var index = 1; index < RequiredExpByLevel.Length; index++)
        {
            accumulatedExp += RequiredExpByLevel[index];
            if (totalExp < accumulatedExp)
            {
                return level;
            }

            level = index + 1;
        }

        return level;
    }

    public static string GetTitle(int totalExp) => GetLevel(totalExp) switch
    {
        <= 5 => "Beginner",
        <= 10 => "Explorer",
        <= 15 => "Achiever",
        <= 20 => "Expert",
        <= 25 => "Master",
        _ => "Champion"
    };
}
