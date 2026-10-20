using KakeiboApp.Models;

namespace KakeiboApp.Services;

/// <summary>The mascot on the 残りのお金 card, after the month's pace (<see cref="PigMoods.For"/>).</summary>
public enum PigMood
{
    /// <summary>いいペース, and a month that has not started yet.</summary>
    Good,
    /// <summary>ちょっと早め: spending is ahead of the days. A little thinner.</summary>
    Ahead,
    /// <summary>オーバー: this month's remaining money is below zero. Gaunt, without the coin.</summary>
    Over,
    /// <summary>A past month that ended with money left. Smiling, with sparkles.</summary>
    EndedWithMoneyLeft,
    /// <summary>A past month that ran short. The gaunt body with a disappointed face.</summary>
    EndedShort,
}

public static class PigMoods
{
    public static PigMood For(MonthSummary summary, MonthPace pace)
    {
        if (pace.ElapsedDays == 0) return PigMood.Good;
        if (pace.RemainingDays == 0) return summary.Remaining >= 0 ? PigMood.EndedWithMoneyLeft : PigMood.EndedShort;
        if (summary.Remaining < 0) return PigMood.Over;
        return pace.IsAhead ? PigMood.Ahead : PigMood.Good;
    }

    /// <summary>The image file (Resources/Images, built from the .svg of the same name).</summary>
    public static string Image(this PigMood mood) => mood switch
    {
        PigMood.Ahead => "pig_thin.png",
        PigMood.Over => "pig_gaunt.png",
        PigMood.EndedWithMoneyLeft => "pig_happy.png",
        PigMood.EndedShort => "pig_short.png",
        _ => "pig.png",
    };
}
