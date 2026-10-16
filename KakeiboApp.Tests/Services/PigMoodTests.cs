using KakeiboApp.Models;
using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class PigMoodTests
{
    static MonthPace Pace(int elapsed, int remaining, decimal dayRatio = 0.5m, decimal spentRatio = 0.3m) => new()
    {
        DaysInMonth = 31, ElapsedDays = elapsed, RemainingDays = remaining, DayRatio = dayRatio, SpentRatio = spentRatio,
    };

    static MonthSummary Remaining(decimal amount) => new() { Remaining = amount };

    [Fact]
    public void SpendingBehindTheDays_IsGood() =>
        Assert.Equal(PigMood.Good, PigMoods.For(Remaining(50000), Pace(16, 16, 0.5m, 0.3m)));

    [Fact]
    public void SpendingAheadOfTheDays_IsThinner() =>
        Assert.Equal(PigMood.Ahead, PigMoods.For(Remaining(20000), Pace(16, 16, 0.5m, 0.8m)));

    [Fact]
    public void RemainingBelowZero_IsGaunt_EvenWhenAhead() =>
        Assert.Equal(PigMood.Over, PigMoods.For(Remaining(-1), Pace(16, 16, 0.5m, 1.1m)));

    [Theory]
    [InlineData(0, PigMood.EndedWithMoneyLeft)]
    [InlineData(12000, PigMood.EndedWithMoneyLeft)]
    [InlineData(-1, PigMood.EndedShort)]
    public void PastMonth_DependsOnWhatWasLeft(int remaining, PigMood expected) =>
        Assert.Equal(expected, PigMoods.For(Remaining(remaining), Pace(31, 0, 1m, 0.9m)));

    [Theory]
    [InlineData(50000)]
    [InlineData(-5000)]
    public void FutureMonth_IsGood(int remaining) =>
        Assert.Equal(PigMood.Good, PigMoods.For(Remaining(remaining), Pace(0, 31, 0m, 0m)));

    [Fact]
    public void EveryMood_HasItsOwnImage()
    {
        var images = Enum.GetValues<PigMood>().Select(m => m.Image()).ToList();

        Assert.Equal(images.Count, images.Distinct().Count());
        Assert.Equal("pig.png", PigMood.Good.Image());
    }

    [Fact]
    public void EveryImage_HasItsSvg()
    {
        var images = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Resources", "Images");
        Assert.All(Enum.GetValues<PigMood>(), m =>
            Assert.True(File.Exists(Path.Combine(images, Path.ChangeExtension(m.Image(), ".svg"))), m.Image()));
    }
}
