using KakeiboApp.Models;
using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class BudgetCalculatorTests
{
    static readonly DateTime August = new(2026, 8, 1);
    static readonly DateTime September = new(2026, 9, 1);
    static readonly DateTime October = new(2026, 10, 1);

    static readonly Category Food = new() { Name = "食費" };
    static readonly Category Fun = new() { Name = "娯楽" };

    static SpendingItem Spending(DateTime date, Category category, decimal amount) =>
        new() { Date = date, Category = category, Name = "item", Amount = amount };

    static MonthlySaving Saving(DateTime date, string name, decimal amount) =>
        new() { Date = date, Name = name, Amount = amount, Note = string.Empty };

    public class Month
    {
        [Theory]
        [InlineData(2026, 10, 1, true)]
        [InlineData(2026, 10, 31, true)]
        [InlineData(2026, 9, 30, false)]
        [InlineData(2026, 11, 1, false)]
        [InlineData(2025, 10, 15, false)]
        public void IsInMonth_MatchesYearAndMonth(int year, int month, int day, bool expected)
        {
            Assert.Equal(expected, BudgetCalculator.IsInMonth(new DateTime(year, month, day), October));
        }

        [Fact]
        public void InMonth_KeepsOnlyItemsOfTheSelectedMonth()
        {
            var incomes = new[]
            {
                new MonthlyIncome { Date = September, Amount = 1 },
                new MonthlyIncome { Date = October, Amount = 2 },
                new MonthlyIncome { Date = new DateTime(2025, 10, 1), Amount = 3 },
                new MonthlyIncome { Date = new DateTime(2026, 10, 20), Amount = 4 },
            };

            var result = BudgetCalculator.InMonth(incomes, x => x.Date, October);

            Assert.Equal([2m, 4m], result.Select(x => x.Amount));
        }

        [Fact]
        public void SavingsInMonth_ExcludesWithdrawals()
        {
            var savings = new[]
            {
                Saving(October, "旅行", 10000),
                Saving(October, "旅行", -3000),
                Saving(September, "車", 5000),
            };

            var result = BudgetCalculator.SavingsInMonth(savings, October);

            Assert.Equal([10000m], result.Select(x => x.Amount));
        }
    }

    public class BudgetControlResults
    {
        [Fact]
        public void BuildsOneRowPerCategoryInCategoryOrder()
        {
            var result = BudgetCalculator.BuildBudgetControlResults([Food, Fun], [], [], October);

            Assert.Equal([Food, Fun], result.Select(x => x.Category));
        }

        [Fact]
        public void SumsTheMonthsSpendingPerCategory()
        {
            var spending = new[]
            {
                Spending(new DateTime(2026, 10, 3), Food, 1000),
                Spending(new DateTime(2026, 10, 31), Food, 2000),
                Spending(new DateTime(2026, 9, 30), Food, 4000),
                Spending(new DateTime(2026, 10, 5), Fun, 500),
            };

            var result = BudgetCalculator.BuildBudgetControlResults([Food, Fun], [], spending, October);

            Assert.Equal(3000, result[0].MonthlySpending);
            Assert.Equal(500, result[1].MonthlySpending);
        }

        [Fact]
        public void UsesTheMonthsBudgetForEachCategory()
        {
            var budgets = new[]
            {
                new MonthlyBudget { Id = 1, Date = October, Category = Food, Amount = 30000 },
                new MonthlyBudget { Id = 2, Date = September, Category = Fun, Amount = 9999 },
            };

            var result = BudgetCalculator.BuildBudgetControlResults([Food, Fun], budgets, [], October);

            Assert.Equal(1, result[0].MonthlyBudget.Id);
            Assert.Equal(30000, result[0].MonthlyBudget.Amount);
        }

        [Fact]
        public void CategoryWithoutBudget_GetsAnUnsavedZeroBudgetForTheMonth()
        {
            var result = BudgetCalculator.BuildBudgetControlResults([Fun], [], [], October);

            var budget = result.Single().MonthlyBudget;
            Assert.Equal(0, budget.Id);
            Assert.Equal(0, budget.Amount);
            Assert.Equal(October, budget.Date);
            Assert.Equal(Fun, budget.Category);
        }
    }

    public class CarryOver
    {
        [Fact]
        public void TotalRemainingUntilPreviousMonth_IsIncomeMinusSavingsFixedCostsAndSpendingBeforeTheMonth()
        {
            var incomes = new[]
            {
                new MonthlyIncome { Date = August, Amount = 300000 },
                new MonthlyIncome { Date = September, Amount = 300000 },
                new MonthlyIncome { Date = October, Amount = 300000 },
            };
            var savings = new[]
            {
                Saving(September, "旅行", 50000),
                Saving(September, "旅行", -10000),
                Saving(October, "旅行", -5000),
            };
            var fixedCosts = new[]
            {
                new MonthlyFixedCost { Date = September, Amount = 80000 },
                new MonthlyFixedCost { Date = October, Amount = 80000 },
            };
            var spending = new[]
            {
                Spending(new DateTime(2026, 9, 20), Food, 1200),
                Spending(new DateTime(2026, 9, 30), Fun, 800),
                Spending(October, Food, 999),
            };

            var result = BudgetCalculator.TotalRemainingUntilPreviousMonth(October, incomes, savings, fixedCosts, spending);

            // 600,000 income - (50,000 - 10,000) net savings - 80,000 fixed - 2,000 spending
            Assert.Equal(478000, result);
        }

        [Fact]
        public void TotalRemainingUntilPreviousMonth_CanBeNegative()
        {
            var result = BudgetCalculator.TotalRemainingUntilPreviousMonth(October, [], [],
                [new MonthlyFixedCost { Date = September, Amount = 80000 }], []);

            Assert.Equal(-80000, result);
        }

        [Fact]
        public void TotalRemainingUntilPreviousMonth_IsZeroWithoutData()
        {
            Assert.Equal(0, BudgetCalculator.TotalRemainingUntilPreviousMonth(October, [], [], [], []));
        }

        [Fact]
        public void SavingsUntilPreviousMonth_GroupsByNameAndAttachesThisMonthsWithdrawal()
        {
            var withdrawal = Saving(October, "旅行", -3000);
            var savings = new[]
            {
                Saving(August, "旅行", 10000),
                Saving(September, "旅行", 10000),
                withdrawal,
                Saving(October, "旅行", 20000),
            };

            var result = BudgetCalculator.SavingsUntilPreviousMonth(savings, October);

            var travel = Assert.Single(result);
            Assert.Equal("旅行", travel.Name);
            Assert.Equal(20000, travel.Amount);
            Assert.Equal(3000, travel.LivingOffSavingAmount);
            Assert.Equal(17000, travel.Balance);
            Assert.Same(withdrawal, travel.LivingOffSaving);
        }

        [Fact]
        public void SavingsUntilPreviousMonth_HidesSavingsThatAddUpToZero()
        {
            var savings = new[]
            {
                Saving(August, "車", 5000),
                Saving(September, "車", -5000),
                Saving(September, "旅行", 7000),
            };

            var result = BudgetCalculator.SavingsUntilPreviousMonth(savings, October);

            Assert.Equal(["旅行"], result.Select(x => x.Name));
        }

        [Fact]
        public void SavingsUntilPreviousMonth_WithoutWithdrawal_PreparesARowForTheMonth()
        {
            var result = BudgetCalculator.SavingsUntilPreviousMonth([Saving(September, "旅行", 7000)], October);

            var travel = Assert.Single(result);
            Assert.Equal(0, travel.LivingOffSavingAmount);
            Assert.Equal(October, travel.LivingOffSaving.Date);
        }

        [Fact]
        public void SavingsUntilPreviousMonth_OnlyMatchesWithdrawalsDatedOnTheFirstOfTheMonth()
        {
            // Withdrawals are saved with the selected month's date (the 1st), so other days are not matched.
            var savings = new[]
            {
                Saving(September, "旅行", 7000),
                Saving(new DateTime(2026, 10, 15), "旅行", -1000),
            };

            var travel = Assert.Single(BudgetCalculator.SavingsUntilPreviousMonth(savings, October));

            Assert.Equal(0, travel.LivingOffSavingAmount);
        }
    }

    public class Totals
    {
        [Fact]
        public void MonthlyUsableMoney_WithoutCarryOver_IsIncomeMinusSavingsAndFixedCosts()
        {
            var result = BudgetCalculator.MonthlyUsableMoney(false, 478000, 300000, 50000, 80000);

            Assert.Equal(170000, result);
        }

        [Fact]
        public void MonthlyUsableMoney_WithCarryOver_AddsTheRemainingUntilPreviousMonth()
        {
            var result = BudgetCalculator.MonthlyUsableMoney(true, 478000, 300000, 50000, 80000);

            Assert.Equal(648000, result);
        }

        [Fact]
        public void MonthlyUsableMoney_WithCarryOver_AddsANegativeRemainingToo()
        {
            var result = BudgetCalculator.MonthlyUsableMoney(true, -20000, 300000, 50000, 80000);

            Assert.Equal(150000, result);
        }

        [Fact]
        public void MonthlyRemainingTotal_IsUsableMoneyMinusSpendingPlusWithdrawals()
        {
            var result = BudgetCalculator.MonthlyRemainingTotal(170000, 1200, 3000);

            Assert.Equal(171800, result);
        }

        [Fact]
        public void MonthlyRemainingTotal_CanBeNegative()
        {
            Assert.Equal(-500, BudgetCalculator.MonthlyRemainingTotal(1000, 1500, 0));
        }
    }

    public class FixedCostCopy
    {
        [Fact]
        public void CopiesPreviousMonthsFixedCostsAsNewRowsInTheSelectedMonth()
        {
            var fixedCosts = new[]
            {
                new MonthlyFixedCost { Id = 3, Date = August, Name = "古い", Amount = 1 },
                new MonthlyFixedCost { Id = 5, Date = September, Name = "家賃", Amount = 80000, Note = "毎月" },
                new MonthlyFixedCost { Id = 6, Date = September, Name = "電気", Amount = 8000 },
                new MonthlyFixedCost { Id = 7, Date = October, Name = "家賃", Amount = 80000 },
            };

            var result = BudgetCalculator.CopyFixedCostsFromPreviousMonth(fixedCosts, October);

            Assert.Equal(["家賃", "電気"], result.Select(x => x.Name));
            Assert.All(result, x =>
            {
                Assert.Equal(0, x.Id);
                Assert.Equal(October, x.Date);
            });
            Assert.Equal([80000m, 8000m], result.Select(x => x.Amount));
            Assert.Equal("毎月", result[0].Note);
        }

        [Fact]
        public void InJanuary_CopiesFromDecemberOfThePreviousYear()
        {
            var fixedCosts = new[]
            {
                new MonthlyFixedCost { Id = 9, Date = new DateTime(2026, 12, 1), Name = "家賃", Amount = 80000 },
                new MonthlyFixedCost { Id = 8, Date = new DateTime(2027, 12, 1), Name = "未来", Amount = 1 },
            };

            var result = BudgetCalculator.CopyFixedCostsFromPreviousMonth(fixedCosts, new DateTime(2027, 1, 1));

            Assert.Equal(["家賃"], result.Select(x => x.Name));
        }

        [Fact]
        public void ReturnsNothingWhenThePreviousMonthIsEmpty()
        {
            Assert.Empty(BudgetCalculator.CopyFixedCostsFromPreviousMonth([], October));
        }
    }

    public class IncomeAndSavingCopy
    {
        [Fact]
        public void CopiesPreviousMonthsIncomesAsNewRowsInTheSelectedMonth()
        {
            var incomes = new[]
            {
                new MonthlyIncome { Id = 2, Date = September, Name = "給料", Amount = 300000, Note = "手取り" },
                new MonthlyIncome { Id = 4, Date = October, Name = "給料", Amount = 310000 },
            };

            var copy = Assert.Single(BudgetCalculator.CopyIncomesFromPreviousMonth(incomes, October));

            Assert.Equal((0, October, "給料", 300000m, "手取り"), (copy.Id, copy.Date, copy.Name, copy.Amount, copy.Note));
        }

        [Fact]
        public void CopiesSavingsButNotWithdrawals()
        {
            var savings = new[]
            {
                new MonthlySaving { Id = 1, Date = September, Name = "旅行", Amount = 20000 },
                new MonthlySaving { Id = 2, Date = September, Name = "旅行", Amount = -5000 },
            };

            var copy = Assert.Single(BudgetCalculator.CopySavingsFromPreviousMonth(savings, October));

            Assert.Equal((0, October, 20000m), (copy.Id, copy.Date, copy.Amount));
        }
    }

    public class BudgetCopy
    {
        static readonly Category Food = new() { Name = "食費" };
        static readonly Category Fun = new() { Name = "娯楽" };
        static readonly Category Car = new() { Name = "車" };

        [Fact]
        public void BudgetsInMonth_LeavesOutKindsWithoutABudget()
        {
            var budgets = new[]
            {
                new MonthlyBudget { Id = 1, Date = September, Category = Food, Amount = 40000 },
                new MonthlyBudget { Id = 2, Date = September, Category = Fun, Amount = 0 },
                new MonthlyBudget { Id = 3, Date = October, Category = Car, Amount = 9000 },
            };

            var result = BudgetCalculator.BudgetsInMonth([Food, Fun, Car], budgets, September);

            Assert.Equal(new Dictionary<string, decimal> { ["食費"] = 40000 }, result);
        }

        [Fact]
        public void AddsMissingBudgetsChangesDifferentOnesAndKeepsTheRest()
        {
            var budgets = new[]
            {
                new MonthlyBudget { Id = 1, Date = September, Category = Food, Amount = 40000 },
                new MonthlyBudget { Id = 2, Date = September, Category = Fun, Amount = 10000 },
                new MonthlyBudget { Id = 3, Date = September, Category = Car, Amount = 5000 },
                new MonthlyBudget { Id = 4, Date = October, Category = Fun, Amount = 8000 },
                new MonthlyBudget { Id = 5, Date = October, Category = Car, Amount = 5000 },
            };

            var writes = BudgetCalculator.CopyBudgetsFromPreviousMonth([Food, Fun, Car], budgets, October);

            Assert.Equal(
                [(0, "食費", 40000m), (4, "娯楽", 10000m)],
                writes.Select(b => (b.Id, b.Category.Name, b.Amount)));
            Assert.All(writes, b => Assert.Equal(October, b.Date));
        }

        [Fact]
        public void KeepsBudgetsOfKindsThePreviousMonthHasNoneFor()
        {
            var budgets = new[] { new MonthlyBudget { Id = 1, Date = October, Category = Food, Amount = 30000 } };

            Assert.Empty(BudgetCalculator.CopyBudgetsFromPreviousMonth([Food, Fun], budgets, October));
        }
    }

    public class SpecialExpenseDate
    {
        [Fact]
        public void InTheCurrentMonth_DefaultsToToday()
        {
            var today = new DateTime(2026, 10, 15);

            Assert.Equal(today, BudgetCalculator.DefaultSpecialExpenseDate(October, today));
        }

        [Theory]
        [InlineData(2026, 9)]
        [InlineData(2026, 11)]
        [InlineData(2025, 10)]
        public void InAnotherMonth_DefaultsToTheFirstOfThatMonth(int year, int month)
        {
            var selected = new DateTime(year, month, 1);

            Assert.Equal(selected, BudgetCalculator.DefaultSpecialExpenseDate(selected, new DateTime(2026, 10, 15)));
        }
    }
}
