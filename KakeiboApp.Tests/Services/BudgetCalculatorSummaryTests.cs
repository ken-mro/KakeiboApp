using KakeiboApp.Models;
using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class BudgetCalculatorSummaryTests
{
    static readonly DateTime September = new(2026, 9, 1);
    static readonly DateTime October = new(2026, 10, 1);
    static readonly DateTime November = new(2026, 11, 1);

    static SpendingItem Spending(DateTime date, Category category, decimal amount) =>
        new() { Date = date, Category = category, Name = "item", Amount = amount };

    static MonthlySaving Saving(DateTime date, string name, decimal amount) =>
        new() { Date = date, Name = name, Amount = amount, Note = string.Empty };

    /// <summary>
    /// The data seeded on the emulator before the redesign. The expected values are the ones version 1.0 showed for it.
    /// </summary>
    static class Seeded
    {
        public static readonly Category[] Categories =
            new[] { "食費", "日用品・雑貨", "娯楽", "車", "医療", "その他" }.Select(n => new Category { Name = n }).ToArray();

        static Category C(string name) => Categories.Single(x => x.Name == name);

        public static MonthlyIncome[] Incomes =>
        [
            new() { Date = September, Name = "給料", Amount = 300000 },
            new() { Date = October, Name = "給料", Amount = 300000 },
            new() { Date = October, Name = "副業", Amount = 15000 },
        ];

        public static MonthlySaving[] Savings =>
        [
            Saving(September, "旅行", 20000), Saving(September, "車", 10000),
            Saving(October, "旅行", 20000), Saving(October, "旅行", -5000),
        ];

        public static MonthlyFixedCost[] FixedCosts =>
        [
            new() { Date = September, Name = "家賃", Amount = 80000 }, new() { Date = September, Name = "電気", Amount = 8000 },
            new() { Date = October, Name = "家賃", Amount = 80000 }, new() { Date = October, Name = "電気", Amount = 7500 },
            new() { Date = October, Name = "ネット", Amount = 5000 },
        ];

        public static MonthlyBudget[] Budgets =>
        [
            new() { Date = September, Category = C("食費"), Amount = 50000 },
            new() { Date = October, Category = C("食費"), Amount = 50000 },
            new() { Date = October, Category = C("日用品・雑貨"), Amount = 15000 },
            new() { Date = October, Category = C("娯楽"), Amount = 10000 },
        ];

        public static SpendingItem[] SpendingItems =>
        [
            Spending(new DateTime(2026, 9, 12), C("食費"), 42000), Spending(new DateTime(2026, 9, 20), C("娯楽"), 3000),
            Spending(new DateTime(2026, 10, 16), C("食費"), 3480), Spending(new DateTime(2026, 10, 16), C("食費"), 1200),
            Spending(new DateTime(2026, 10, 16), C("車"), 6000), Spending(new DateTime(2026, 10, 15), C("娯楽"), 2000),
            Spending(new DateTime(2026, 10, 15), C("日用品・雑貨"), 1560), Spending(new DateTime(2026, 10, 3), C("娯楽"), 9500),
            Spending(new DateTime(2026, 10, 5), C("医療"), 3200), Spending(new DateTime(2026, 10, 8), C("その他"), 4000),
        ];

        public static SpecialExpense[] SpecialExpenses =>
        [
            new() { Date = new DateTime(2026, 10, 10), Name = "結婚式のご祝儀", Amount = 30000 },
            new() { Date = new DateTime(2026, 3, 20), Name = "旅行", Amount = 50000 },
        ];

        public static MonthSummary Summarize(DateTime month, bool carriesOver = false) =>
            BudgetCalculator.Summarize(month, carriesOver, Incomes, Savings, FixedCosts, Categories, Budgets, SpendingItems, SpecialExpenses);
    }

    public class Summary
    {
        [Fact]
        public void MatchesTheTotalsVersion1ShowedForTheSameData()
        {
            var summary = Seeded.Summarize(October);

            Assert.Equal(315000, summary.IncomeTotal);
            Assert.Equal(20000, summary.SavingTotal);
            Assert.Equal(92500, summary.FixedCostTotal);
            Assert.Equal(202500, summary.UsableMoney);
            Assert.Equal(30940, summary.SpendingTotal);
            Assert.Equal(75000, summary.BudgetTotal);
            Assert.Equal(5000, summary.LivingOff);
            Assert.Equal(176560, summary.Remaining);
            Assert.Equal(137000, summary.RemainingUntilPreviousMonth);
            Assert.Equal(25000, summary.SavingBalanceTotal);
            Assert.Equal(30000, summary.SpecialExpenseTotal);
        }

        [Fact]
        public void ComposesTheExistingFormulas()
        {
            var summary = Seeded.Summarize(October, carriesOver: true);

            var usable = BudgetCalculator.MonthlyUsableMoney(true, summary.RemainingUntilPreviousMonth,
                summary.IncomeTotal, summary.SavingTotal, summary.FixedCostTotal);
            Assert.Equal(usable, summary.UsableMoney);
            Assert.Equal(339500, summary.UsableMoney);
            Assert.Equal(BudgetCalculator.MonthlyRemainingTotal(usable, summary.SpendingTotal, summary.LivingOff), summary.Remaining);
            Assert.Equal(summary.Budgets.Sum(x => x.MonthlySpending), summary.SpendingTotal);
        }

        [Fact]
        public void PerCategoryAmounts_MatchVersion1()
        {
            var summary = Seeded.Summarize(October);

            Assert.Equal([4680m, 1560m, 11500m, 6000m, 3200m, 4000m], summary.Budgets.Select(x => x.MonthlySpending));
            Assert.Equal([50000m, 15000m, 10000m, 0m, 0m, 0m], summary.Budgets.Select(x => x.MonthlyBudget.Amount));
        }

        [Fact]
        public void SavingBalances_MatchVersion1()
        {
            var balances = Seeded.Summarize(October).SavingBalances;

            Assert.Equal(["旅行", "車"], balances.Select(x => x.Name));
            Assert.Equal([20000m, 10000m], balances.Select(x => x.Amount));
            Assert.Equal([5000m, 0m], balances.Select(x => x.LivingOffSavingAmount));
        }

        [Fact]
        public void SpecialExpenses_DoNotChangeTheRemainingMoney()
        {
            var without = BudgetCalculator.Summarize(October, false, Seeded.Incomes, Seeded.Savings, Seeded.FixedCosts,
                Seeded.Categories, Seeded.Budgets, Seeded.SpendingItems, []);

            Assert.Equal(without.Remaining, Seeded.Summarize(October).Remaining);
        }

        [Fact]
        public void SpecialExpenseTotalInYear_AddsTheWholeYear()
        {
            Assert.Equal(80000, BudgetCalculator.SpecialExpenseTotalInYear(Seeded.SpecialExpenses, 2026));
            Assert.Equal(0, BudgetCalculator.SpecialExpenseTotalInYear(Seeded.SpecialExpenses, 2025));
        }
    }

    public class EmptyMonth
    {
        [Fact]
        public void AMonthWithoutRecords_IsEmpty()
        {
            Assert.True(BudgetCalculator.IsEmptyMonth(Seeded.Summarize(November)));
        }

        [Fact]
        public void AMonthWithRecords_IsNotEmpty()
        {
            Assert.False(BudgetCalculator.IsEmptyMonth(Seeded.Summarize(October)));
        }

        [Fact]
        public void WithCarryOver_MoneyCarriedIntoTheMonth_MakesItNotEmpty()
        {
            Assert.False(BudgetCalculator.IsEmptyMonth(Seeded.Summarize(November, carriesOver: true)));
        }

        [Fact]
        public void ABudgetAlone_MakesTheMonthNotEmpty()
        {
            var food = new Category { Name = "食費" };
            var summary = BudgetCalculator.Summarize(November, false, [], [], [], [food],
                [new MonthlyBudget { Date = November, Category = food, Amount = 1 }], [], []);

            Assert.False(BudgetCalculator.IsEmptyMonth(summary));
        }
    }

    public class Pace
    {
        static MonthSummary Summary(decimal usable, decimal spending, decimal livingOff = 0) => new()
        {
            UsableMoney = usable,
            SpendingTotal = spending,
            LivingOff = livingOff,
            Remaining = BudgetCalculator.MonthlyRemainingTotal(usable, spending, livingOff),
        };

        [Fact]
        public void CurrentMonth_CountsTodayAsElapsedAndRemaining()
        {
            var pace = BudgetCalculator.Pace(October, new DateTime(2026, 10, 15), Summary(300000, 210000));

            Assert.Equal(31, pace.DaysInMonth);
            Assert.Equal(15, pace.ElapsedDays);
            Assert.Equal(17, pace.RemainingDays);
            Assert.Equal(15m / 31m, pace.DayRatio);
            Assert.Equal(0.7m, pace.SpentRatio);
            Assert.True(pace.IsAhead);
        }

        [Fact]
        public void DailyAllowance_IsTheRemainingMoneyOverTheRemainingDays()
        {
            var pace = BudgetCalculator.Pace(October, new DateTime(2026, 10, 15), Summary(300000, 210000));

            // ¥90,000 left for 17 days, as in the design.
            Assert.Equal("¥5,294", MoneyFormat.Yen(pace.DailyAllowance));
        }

        [Fact]
        public void SpendingBehindTheDays_IsNotAhead()
        {
            var pace = BudgetCalculator.Pace(October, new DateTime(2026, 10, 20), Summary(300000, 60000));

            Assert.False(pace.IsAhead);
        }

        [Fact]
        public void WithdrawalsAddToTheMoneyAvailableForSpending()
        {
            var pace = BudgetCalculator.Pace(October, new DateTime(2026, 10, 15), Summary(90000, 50000, livingOff: 10000));

            Assert.Equal(0.5m, pace.SpentRatio);
        }

        [Fact]
        public void PastMonth_HasEveryDayElapsedAndTheWholeRemainderAsTheDailyAmount()
        {
            var pace = BudgetCalculator.Pace(September, new DateTime(2026, 10, 15), Summary(100000, 40000));

            Assert.Equal(30, pace.ElapsedDays);
            Assert.Equal(0, pace.RemainingDays);
            Assert.Equal(1m, pace.DayRatio);
            Assert.Equal(60000, pace.DailyAllowance);
        }

        [Fact]
        public void FutureMonth_HasNoDaysElapsedAndSpreadsOverTheWholeMonth()
        {
            var pace = BudgetCalculator.Pace(November, new DateTime(2026, 10, 15), Summary(300000, 0));

            Assert.Equal(0, pace.ElapsedDays);
            Assert.Equal(30, pace.RemainingDays);
            Assert.Equal(0m, pace.DayRatio);
            Assert.Equal(10000, pace.DailyAllowance);
        }

        [Fact]
        public void LeapFebruary_Has29Days()
        {
            var pace = BudgetCalculator.Pace(new DateTime(2028, 2, 1), new DateTime(2028, 2, 10), Summary(1, 0));

            Assert.Equal(29, pace.DaysInMonth);
            Assert.Equal(20, pace.RemainingDays);
        }

        [Fact]
        public void OverspentMonth_HasNoDailyAllowance()
        {
            var pace = BudgetCalculator.Pace(October, new DateTime(2026, 10, 15), Summary(100000, 120000));

            Assert.Equal(0, pace.DailyAllowance);
            Assert.Equal(1.2m, pace.SpentRatio);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 500, 1)]
        [InlineData(-1000, 500, 1)]
        public void NothingToSpend_CountsAnySpendingAsAllOfIt(decimal usable, decimal spending, decimal expected)
        {
            var pace = BudgetCalculator.Pace(October, new DateTime(2026, 10, 15), Summary(usable, spending));

            Assert.Equal(expected, pace.SpentRatio);
        }
    }

    public class Grouping
    {
        [Fact]
        public void GroupByDay_PutsTheNewestDayAndTheNewestEntryFirst()
        {
            var items = new[]
            {
                new SpendingItem { Id = 1, Date = new DateTime(2026, 10, 15), Name = "a" },
                new SpendingItem { Id = 2, Date = new DateTime(2026, 10, 16), Name = "b" },
                new SpendingItem { Id = 3, Date = new DateTime(2026, 10, 15), Name = "c" },
            };

            var groups = BudgetCalculator.GroupByDay(items, x => x.Date, x => x.Id);

            Assert.Equal([new DateTime(2026, 10, 16), new DateTime(2026, 10, 15)], groups.Select(g => g.Key));
            Assert.Equal(["c", "a"], groups[1].Select(x => x.Name));
        }

        [Fact]
        public void Total_OfTheMonthsSpending_MatchesTheSummary()
        {
            var inMonth = BudgetCalculator.InMonth(Seeded.SpendingItems, x => x.Date, October);

            Assert.Equal(Seeded.Summarize(October).SpendingTotal, BudgetCalculator.Total(inMonth, x => x.Amount));
        }
    }
}
