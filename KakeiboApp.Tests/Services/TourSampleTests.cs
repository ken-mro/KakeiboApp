using KakeiboApp.Models;
using KakeiboApp.Services;
using KakeiboApp.ViewModels.Entries;

namespace KakeiboApp.Tests.Services;

public class TourSampleTests
{
    static readonly DateTime October = new(2026, 10, 1);
    static readonly Category[] Categories = [new() { Name = "食費" }, new() { Name = "日用品・雑貨" }, new() { Name = "娯楽" }];

    [Fact]
    public void Summary_FillsEveryCardOfTheHome()
    {
        var summary = TourSample.Summary(October, Categories);

        Assert.False(BudgetCalculator.IsEmptyMonth(summary));
        Assert.True(summary.IncomeTotal > 0);
        Assert.True(summary.SavingTotal > 0);
        Assert.True(summary.FixedCostTotal > 0);
        Assert.True(summary.SpendingTotal > 0);
        Assert.True(summary.BudgetTotal > 0);
        Assert.True(summary.LivingOff > 0);
        Assert.True(summary.SpecialExpenseTotal > 0);
        Assert.NotEmpty(summary.SavingBalances);
    }

    [Fact]
    public void Summary_AddsUpLikeRealData()
    {
        var summary = TourSample.Summary(October, Categories);

        // 250,000 − 20,000 − 95,000 = 135,000; − 6,100 spending + 5,000 withdrawn.
        Assert.Equal(135000, summary.UsableMoney);
        Assert.Equal(6100, summary.SpendingTotal);
        Assert.Equal(133900, summary.Remaining);
    }

    [Fact]
    public void Summary_HasOneKindOverItsBudget()
    {
        // The budget page explains オーバー; the example shows it.
        var over = TourSample.Summary(October, Categories).Budgets
            .Where(b => b.MonthlyBudget.Amount > 0 && b.MonthlySpending > b.MonthlyBudget.Amount)
            .Select(b => b.Category.Name);

        Assert.Equal(["娯楽"], over);
    }

    [Fact]
    public void Summary_DoesNotCarryOver()
    {
        // The example's saving from the previous month must not lower 今月使えるお金.
        Assert.False(TourSample.Summary(October, Categories).CarriesOver);
    }

    [Theory]
    [InlineData(EntryKind.Spending, 4, 6100)]
    [InlineData(EntryKind.SpecialExpense, 1, 30000)]
    public void Page_HasRowsForTheLists(EntryKind kind, int count, int total)
    {
        var page = TourSample.Page(kind, October);

        Assert.Equal(count, page.Count);
        Assert.Equal(total, page.Total);
        Assert.Single(page.Groups);
    }
}
