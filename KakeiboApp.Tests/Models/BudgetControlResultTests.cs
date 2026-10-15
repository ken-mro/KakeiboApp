using KakeiboApp.Models;

namespace KakeiboApp.Tests.Models;

public class BudgetControlResultTests
{
    static BudgetControlResult Create(DateTime budgetMonth, decimal budget, decimal spending) => new()
    {
        MonthlyBudget = new MonthlyBudget
        {
            Date = budgetMonth,
            Category = new Category { Name = "食費" },
            Amount = budget,
        },
        MonthlySpending = spending,
    };

    [Fact]
    public void RemainingBudget_IsBudgetMinusSpending()
    {
        var result = Create(new DateTime(2026, 10, 1), 30000, 12000);

        Assert.Equal(18000, result.RemainingBudget);
    }

    [Fact]
    public void RemainingBudget_CanBeNegative()
    {
        var result = Create(new DateTime(2026, 10, 1), 1000, 1500);

        Assert.Equal(-500, result.RemainingBudget);
    }

    [Fact]
    public void Category_ComesFromTheBudget()
    {
        var result = Create(new DateTime(2026, 10, 1), 1000, 0);

        Assert.Equal(new Category { Name = "食費" }, result.Category);
    }

    [Theory]
    [InlineData(1000, 1000)]
    [InlineData(1000, 1500)]
    [InlineData(0, 0)]
    public void DailySpending_IsZero_WhenNothingRemains(decimal budget, decimal spending)
    {
        var result = Create(new DateTime(2026, 10, 1), budget, spending);

        Assert.Equal(0, result.GetDailySpending(new DateTime(2026, 10, 15)));
    }

    [Fact]
    public void DailySpending_ForPastMonth_IsTheWholeRemainingBudget()
    {
        var result = Create(new DateTime(2026, 9, 1), 30000, 12000);

        Assert.Equal(18000, result.GetDailySpending(new DateTime(2026, 10, 15)));
    }

    [Fact]
    public void DailySpending_ForFutureMonth_DividesByAllDaysOfThatMonth()
    {
        // November has 30 days.
        var result = Create(new DateTime(2026, 11, 1), 30000, 12000);

        Assert.Equal(600, result.GetDailySpending(new DateTime(2026, 10, 15)));
    }

    [Fact]
    public void DailySpending_ForFutureFebruaryOfNonLeapYear_DividesBy28()
    {
        var result = Create(new DateTime(2027, 2, 1), 28000, 0);

        Assert.Equal(1000, result.GetDailySpending(new DateTime(2026, 10, 15)));
    }

    [Fact]
    public void DailySpending_ForCurrentMonth_DividesByDaysLeftIncludingToday()
    {
        // October has 31 days: the 15th through the 31st is 17 days.
        var result = Create(new DateTime(2026, 10, 1), 34000, 0);

        Assert.Equal(2000, result.GetDailySpending(new DateTime(2026, 10, 15)));
    }

    [Fact]
    public void DailySpending_OnFirstDayOfMonth_DividesByWholeMonth()
    {
        var result = Create(new DateTime(2026, 10, 1), 31000, 0);

        Assert.Equal(1000, result.GetDailySpending(new DateTime(2026, 10, 1)));
    }

    [Fact]
    public void DailySpending_OnLastDayOfMonth_IsTheWholeRemainingBudget()
    {
        var result = Create(new DateTime(2026, 10, 1), 30000, 12000);

        Assert.Equal(18000, result.GetDailySpending(new DateTime(2026, 10, 31)));
    }

    [Fact]
    public void DailySpending_InLeapYearFebruary_Uses29Days()
    {
        var result = Create(new DateTime(2028, 2, 1), 29000, 0);

        Assert.Equal(1000, result.GetDailySpending(new DateTime(2028, 2, 1)));
    }

    [Fact]
    public void DailySpending_SameMonthOfAnotherYear_IsNotTreatedAsCurrent()
    {
        // October 2025 is in the past relative to October 2026.
        var result = Create(new DateTime(2025, 10, 1), 31000, 0);

        Assert.Equal(31000, result.GetDailySpending(new DateTime(2026, 10, 15)));
    }
}
