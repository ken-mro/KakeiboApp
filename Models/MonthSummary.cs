namespace KakeiboApp.Models;

/// <summary>
/// Every amount shown on the home page for one month. Built by BudgetCalculator.Summarize.
/// </summary>
public record MonthSummary
{
    public DateTime Month { get; init; }
    public decimal IncomeTotal { get; init; }
    /// <summary>貯金の合計。切り崩し(マイナスの金額)は含めない。</summary>
    public decimal SavingTotal { get; init; }
    public decimal FixedCostTotal { get; init; }
    /// <summary>「残りのお金を翌月に繰り越す」の設定。</summary>
    public bool CarriesOver { get; init; }
    /// <summary>先月までの残高。</summary>
    public decimal RemainingUntilPreviousMonth { get; init; }
    public decimal UsableMoney { get; init; }
    public decimal SpendingTotal { get; init; }
    public decimal BudgetTotal { get; init; }
    /// <summary>選択月の切り崩しの合計(正の数)。</summary>
    public decimal LivingOff { get; init; }
    public decimal Remaining { get; init; }
    public decimal SpecialExpenseTotal { get; init; }
    public IReadOnlyList<BudgetControlResult> Budgets { get; init; } = [];
    public IReadOnlyList<SavingResult> SavingBalances { get; init; } = [];
    /// <summary>先月までの貯金の内訳の「貯金総額」(各行の 金額 − 切り崩し の合計)。</summary>
    public decimal SavingBalanceTotal { get; init; }
}

/// <summary>
/// 今月のペース。日数の割合と、生活費に使えるお金の消化率。
/// </summary>
public record MonthPace
{
    public int DaysInMonth { get; init; }
    /// <summary>経過日数(今日を含む)。過去の月は月の日数、未来の月は 0。</summary>
    public int ElapsedDays { get; init; }
    /// <summary>残りの日数(今日を含む)。過去の月は 0、未来の月は月の日数。</summary>
    public int RemainingDays { get; init; }
    public decimal DayRatio { get; init; }
    /// <summary>生活費 ÷ (今月使えるお金 + 切り崩し)。1 を超えることがある。</summary>
    public decimal SpentRatio { get; init; }
    /// <summary>使った割合が日数の割合より先に進んでいる(使いすぎのサイン)。</summary>
    public bool IsAhead => SpentRatio > DayRatio;
    /// <summary>1日あたり使えるお金。</summary>
    public decimal DailyAllowance { get; init; }
}
