using KakeiboApp.Models;
using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class PaceAdviceTests
{
    static readonly DateTime October = new(2026, 10, 1);

    static string Advice(DateTime month, DateTime today, decimal usable, decimal spending)
    {
        var summary = new MonthSummary
        {
            UsableMoney = usable,
            SpendingTotal = spending,
            Remaining = BudgetCalculator.MonthlyRemainingTotal(usable, spending, 0),
        };
        return PaceAdvice.For(summary, BudgetCalculator.Pace(month, today, summary));
    }

    [Fact]
    public void AheadOfTheDays_SuggestsADailyLimit()
    {
        Assert.Equal("ちょっと早めのペースです。\n1日 ¥5,294 までにすると、月末までちょうど足ります。",
            Advice(October, new DateTime(2026, 10, 15), 300000, 210000));
    }

    [Fact]
    public void BehindTheDays_SaysThePaceIsGood()
    {
        Assert.StartsWith("いいペースです。", Advice(October, new DateTime(2026, 10, 20), 300000, 60000));
    }

    [Fact]
    public void Overspent_SaysByHowMuch()
    {
        Assert.StartsWith("今月は ¥20,000 オーバーしています。", Advice(October, new DateTime(2026, 10, 15), 100000, 120000));
    }

    [Fact]
    public void PastMonth_SaysWhatWasLeft()
    {
        Assert.Equal("この月は ¥60,000 残りました。", Advice(new DateTime(2026, 9, 1), new DateTime(2026, 10, 15), 100000, 40000));
    }

    [Fact]
    public void PastMonth_Overspent_SaysWhatWasShort()
    {
        Assert.Equal("この月は ¥5,000 足りませんでした。", Advice(new DateTime(2026, 9, 1), new DateTime(2026, 10, 15), 100000, 105000));
    }

    [Fact]
    public void FutureMonth_SaysItHasNotStarted()
    {
        Assert.StartsWith("まだ始まっていない月です。", Advice(new DateTime(2026, 11, 1), new DateTime(2026, 10, 15), 300000, 0));
    }
}
