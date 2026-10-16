using KakeiboApp.Models;
using KakeiboApp.ViewModels.Entries;

namespace KakeiboApp.Services;

/// <summary>
/// Example amounts the guide shows while it explains a month without records, so a new user sees what the
/// cards look like in use. They are only shown, never saved, and go through BudgetCalculator like real data.
/// </summary>
public static class TourSample
{
    public const string Notice = "金額は例です";

    public static MonthSummary Summary(DateTime month, IEnumerable<Category> categories)
    {
        var day = Day(month);
        var food = new Category { Name = "食費" };
        var daily = new Category { Name = "日用品・雑貨" };
        var previousMonth = month.AddMonths(-1);
        return BudgetCalculator.Summarize(month, carriesOver: false,
            incomes: [new MonthlyIncome { Date = month, Name = "給料", Amount = 250000 }],
            savings:
            [
                new MonthlySaving { Date = month, Name = "旅行", Amount = 20000 },
                new MonthlySaving { Date = previousMonth, Name = "旅行", Amount = 20000 },
                // 切り崩し: a negative saving in this month.
                new MonthlySaving { Date = month, Name = "旅行", Amount = -5000 },
            ],
            fixedCosts:
            [
                new MonthlyFixedCost { Date = month, Name = "家賃", Amount = 80000 },
                new MonthlyFixedCost { Date = month, Name = "電気・ガス・水道", Amount = 15000 },
            ],
            categories: categories,
            budgets:
            [
                new MonthlyBudget { Date = month, Category = food, Amount = 40000 },
                new MonthlyBudget { Date = month, Category = daily, Amount = 10000 },
            ],
            spendingItems: SpendingItems(day, food, daily),
            specialExpenses: [new SpecialExpense { Date = day, Name = "旅行", Amount = 30000, FromWhere = "旅行の貯金" }]);
    }

    /// <summary>The rows of an empty 生活費 or 特別出費 list during the guide.</summary>
    public static EntryPage Page(EntryKind kind, DateTime month)
    {
        var day = Day(month);
        var rows = kind switch
        {
            EntryKind.Spending => SpendingItems(day, new Category { Name = "食費" }, new Category { Name = "日用品・雑貨" })
                .Select(EntryService.Row).ToList(),
            EntryKind.SpecialExpense =>
                [EntryService.Row(new SpecialExpense { Date = day, Name = "旅行", Amount = 30000, FromWhere = "旅行の貯金" })],
            _ => new List<EntryRow>(),
        };
        if (rows.Count == 0) return new EntryPage([], 0, 0);
        return new EntryPage([new EntryGroup(EntryService.DayHeader(day), rows)], rows.Count, BudgetCalculator.Total(rows, r => r.Amount));
    }

    static List<SpendingItem> SpendingItems(DateTime day, Category food, Category daily) =>
    [
        new() { Id = 3, Date = day, Category = food, Name = "スーパー", Amount = 2480 },
        new() { Id = 2, Date = day, Category = food, Name = "ランチ", Amount = 980 },
        new() { Id = 1, Date = day, Category = daily, Name = "ティッシュ・洗剤", Amount = 640 },
    ];

    // Today in this month, or the 1st of another month.
    static DateTime Day(DateTime month) =>
        BudgetCalculator.IsInMonth(DateTime.Today, month) ? DateTime.Today : new DateTime(month.Year, month.Month, 1);
}
