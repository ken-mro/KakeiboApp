using KakeiboApp.Models;

namespace KakeiboApp.Services;

/// <summary>
/// ホーム画面に表示する金額の計算。日付は引数で受け取り、UI やデータベースには依存しない。
/// </summary>
public static class BudgetCalculator
{
    public static bool IsInMonth(DateTime date, DateTime month) =>
        date.Month.Equals(month.Month) && date.Year.Equals(month.Year);

    public static List<T> InMonth<T>(IEnumerable<T> items, Func<T, DateTime> dateOf, DateTime month) =>
        items.Where(x => IsInMonth(dateOf(x), month)).ToList();

    /// <summary>
    /// 選択月の貯金。切り崩し(マイナスの金額)は含めない。
    /// </summary>
    public static List<MonthlySaving> SavingsInMonth(IEnumerable<MonthlySaving> savings, DateTime month) =>
        savings.Where(x => IsInMonth(x.Date, month) && x.Amount > 0).ToList();

    /// <summary>
    /// カテゴリごとの予算と出費。予算が未登録のカテゴリは金額 0 の予算になる。
    /// </summary>
    public static List<BudgetControlResult> BuildBudgetControlResults(
        IEnumerable<Category> categories,
        IEnumerable<MonthlyBudget> budgets,
        IEnumerable<SpendingItem> spendingItems,
        DateTime month)
    {
        var monthsBudgets = InMonth(budgets, x => x.Date, month);
        var monthsSpendingItems = InMonth(spendingItems, x => x.Date, month);

        return categories.Select(category =>
        {
            var monthlySpending = monthsSpendingItems.Where(x => x.Category.Name.Equals(category.Name))
                                                     .Sum(x => x.Amount);
            var budget = monthsBudgets.Where(b => b.Category.Equals(category))
                                      .FirstOrDefault(new MonthlyBudget() { Category = category, Date = month });
            return new BudgetControlResult()
            {
                MonthlyBudget = budget,
                MonthlySpending = monthlySpending
            };
        }).ToList();
    }

    /// <summary>
    /// 先月までの残高 = 収入 - 貯金(切り崩し分を差し引いた正味) - 固定費 - 生活費。いずれも選択月より前の合計。
    /// 特別出費は生活費とは別枠のため、仕様として含めない。
    /// </summary>
    public static decimal TotalRemainingUntilPreviousMonth(
        DateTime month,
        IEnumerable<MonthlyIncome> incomes,
        IEnumerable<MonthlySaving> savings,
        IEnumerable<MonthlyFixedCost> fixedCosts,
        IEnumerable<SpendingItem> spendingItems)
    {
        var totalIncomeAmount = incomes.Where(x => x.Date < month).Sum(x => x.Amount);
        var totalSavingAmount = savings.Where(x => x.Date < month).Sum(x => x.Amount);
        var totalFixedCostAmount = fixedCosts.Where(x => x.Date < month).Sum(x => x.Amount);
        var totalSpendingAmount = spendingItems.Where(x => x.Date < month).Sum(x => x.Amount);

        return totalIncomeAmount - totalSavingAmount - totalFixedCostAmount - totalSpendingAmount;
    }

    /// <summary>
    /// 先月までの貯金を名前ごとにまとめ、選択月の切り崩しを結び付ける。合計が 0 の貯金は除く。
    /// </summary>
    public static List<SavingResult> SavingsUntilPreviousMonth(IEnumerable<MonthlySaving> savings, DateTime month)
    {
        var savingList = savings.ToList();
        var monthsLivingOffs = savingList.Where(x => x.Date.Equals(month) && x.Amount < 0).ToList();

        return savingList.Where(x => x.Date < month)
            .GroupBy(x => x.Name)
            .Select(x =>
            {
                var livingOff = monthsLivingOffs.FirstOrDefault(l => l.Name.Equals(x.Key));
                var totalSavingAmount = x.Sum(y => y.Amount);
                if (totalSavingAmount.Equals(0)) return null;
                return livingOff is null
                    ? new SavingResult(month, x.Key, totalSavingAmount)
                    : new SavingResult(livingOff, x.Key, totalSavingAmount);
            })
            .OfType<SavingResult>()
            .ToList();
    }

    /// <summary>
    /// 今月使えるお金 = (繰り越す場合は先月までの残高 +) 収入 - 貯金 - 固定費
    /// </summary>
    public static decimal MonthlyUsableMoney(
        bool recordsTotalRemainingToThisMonth,
        decimal totalRemainingUntilPreviousMonth,
        decimal monthlyIncomeTotal,
        decimal monthlySavingTotal,
        decimal monthlyFixedCostTotal)
    {
        var usableMoney = monthlyIncomeTotal - monthlySavingTotal - monthlyFixedCostTotal;
        return recordsTotalRemainingToThisMonth ? totalRemainingUntilPreviousMonth + usableMoney : usableMoney;
    }

    /// <summary>
    /// 残りのお金 = 今月使えるお金 - 生活費 + 切り崩し。特別出費は仕様として含めない。
    /// </summary>
    public static decimal MonthlyRemainingTotal(decimal monthlyUsableMoney, decimal monthlyVariableCostTotal, decimal monthlyLivingOff) =>
        monthlyUsableMoney - monthlyVariableCostTotal + monthlyLivingOff;

    /// <summary>
    /// 前月の固定費を選択月の新しい行として複製する。
    /// </summary>
    public static List<MonthlyFixedCost> CopyFixedCostsFromPreviousMonth(IEnumerable<MonthlyFixedCost> fixedCosts, DateTime month)
    {
        var previousMonth = month.AddMonths(-1);
        return InMonth(fixedCosts, x => x.Date, previousMonth).Select(c =>
        {
            c.Id = 0;
            c.Date = month;
            return c;
        }).ToList();
    }

    /// <summary>
    /// 特別出費の日付の初期値。選択月が今月なら今日、それ以外は選択月の 1 日。
    /// </summary>
    public static DateTime DefaultSpecialExpenseDate(DateTime month, DateTime today) =>
        IsInMonth(today, month) ? today : month;

    public static decimal Total<T>(IEnumerable<T> items, Func<T, decimal> amountOf) => items.Sum(amountOf);

    /// <summary>
    /// ホームに表示する金額をまとめて計算する。式は上の各メソッドと同じで、ここでは組み合わせるだけ。
    /// </summary>
    public static MonthSummary Summarize(
        DateTime month,
        bool carriesOver,
        IEnumerable<MonthlyIncome> incomes,
        IEnumerable<MonthlySaving> savings,
        IEnumerable<MonthlyFixedCost> fixedCosts,
        IEnumerable<Category> categories,
        IEnumerable<MonthlyBudget> budgets,
        IEnumerable<SpendingItem> spendingItems,
        IEnumerable<SpecialExpense> specialExpenses)
    {
        var incomeList = incomes.ToList();
        var savingList = savings.ToList();
        var fixedCostList = fixedCosts.ToList();
        var spendingList = spendingItems.ToList();

        var incomeTotal = Total(InMonth(incomeList, x => x.Date, month), x => x.Amount);
        var savingTotal = Total(SavingsInMonth(savingList, month), x => x.Amount);
        var fixedCostTotal = Total(InMonth(fixedCostList, x => x.Date, month), x => x.Amount);
        var budgetResults = BuildBudgetControlResults(categories, budgets, spendingList, month);
        var spendingTotal = Total(budgetResults, x => x.MonthlySpending);
        var savingBalances = SavingsUntilPreviousMonth(savingList, month);
        var livingOff = Total(savingBalances, x => x.LivingOffSavingAmount);
        var remainingUntilPreviousMonth = TotalRemainingUntilPreviousMonth(month, incomeList, savingList, fixedCostList, spendingList);
        var usableMoney = MonthlyUsableMoney(carriesOver, remainingUntilPreviousMonth, incomeTotal, savingTotal, fixedCostTotal);

        return new MonthSummary
        {
            Month = month,
            IncomeTotal = incomeTotal,
            SavingTotal = savingTotal,
            FixedCostTotal = fixedCostTotal,
            CarriesOver = carriesOver,
            RemainingUntilPreviousMonth = remainingUntilPreviousMonth,
            UsableMoney = usableMoney,
            SpendingTotal = spendingTotal,
            BudgetTotal = Total(budgetResults, x => x.MonthlyBudget.Amount),
            LivingOff = livingOff,
            Remaining = MonthlyRemainingTotal(usableMoney, spendingTotal, livingOff),
            SpecialExpenseTotal = Total(InMonth(specialExpenses, x => x.Date, month), x => x.Amount),
            Budgets = budgetResults,
            SavingBalances = savingBalances,
            SavingBalanceTotal = Total(savingBalances, x => x.Balance),
        };
    }

    /// <summary>
    /// 何も記録されていない月。繰り越しがある場合は、繰り越し額が 0 のときだけ空とみなす。
    /// </summary>
    public static bool IsEmptyMonth(MonthSummary summary) =>
        summary.IncomeTotal == 0
        && summary.SavingTotal == 0
        && summary.FixedCostTotal == 0
        && summary.SpendingTotal == 0
        && summary.BudgetTotal == 0
        && summary.LivingOff == 0
        && summary.SpecialExpenseTotal == 0
        && (!summary.CarriesOver || summary.RemainingUntilPreviousMonth == 0);

    /// <summary>
    /// 今月のペース。1日あたりの金額は BudgetControlResult.GetDailySpending と同じ考え方で、残りのお金から求める。
    /// </summary>
    public static MonthPace Pace(DateTime month, DateTime today, MonthSummary summary)
    {
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        var thisMonth = new DateTime(today.Year, today.Month, 1);
        var selected = new DateTime(month.Year, month.Month, 1);

        var (elapsed, remainingDays) = selected < thisMonth ? (daysInMonth, 0)
            : thisMonth < selected ? (0, daysInMonth)
            : (today.Day, daysInMonth - today.Day + 1);

        var available = summary.UsableMoney + summary.LivingOff;
        var spentRatio = available > 0 ? summary.SpendingTotal / available
            : summary.SpendingTotal > 0 ? 1 : 0;

        var remaining = summary.Remaining;
        var daily = remaining <= 0 ? 0
            : remainingDays == 0 ? remaining
            : remaining / remainingDays;

        return new MonthPace
        {
            DaysInMonth = daysInMonth,
            ElapsedDays = elapsed,
            RemainingDays = remainingDays,
            DayRatio = (decimal)elapsed / daysInMonth,
            SpentRatio = spentRatio,
            DailyAllowance = daily,
        };
    }

    /// <summary>
    /// 選択年の特別出費の合計(特別出費の一覧の総額)。
    /// </summary>
    public static decimal SpecialExpenseTotalInYear(IEnumerable<SpecialExpense> specialExpenses, int year) =>
        Total(specialExpenses.Where(x => x.Date.Year == year), x => x.Amount);

    /// <summary>
    /// 日付ごとにまとめる。新しい日付が先、同じ日の中では登録の新しいものが先。
    /// </summary>
    public static List<IGrouping<DateTime, T>> GroupByDay<T>(IEnumerable<T> items, Func<T, DateTime> dateOf, Func<T, int> idOf) =>
        items.OrderByDescending(x => dateOf(x).Date)
             .ThenByDescending(idOf)
             .GroupBy(x => dateOf(x).Date)
             .ToList();
}
