using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using KakeiboApp.Services;
using KakeiboApp.ViewModels.Entries;
using KakeiboApp.Views;
using KakeiboApp.Views.Controls;
using KakeiboApp.Views.Sheets;
using Plugin.Maui.Biometric;

namespace KakeiboApp.ViewModels;

/// <summary>
/// A category row of the 生活費 card: spending against the month's budget. Tap it to enter the budget.
/// </summary>
public class CategoryBudgetRow(EntryRow budget)
{
    public EntryRow Budget { get; } = budget;
    public string Name => Budget.Title;
    public CategoryStyle Style => Budget.Style;
    decimal Spent => Budget.Secondary;
    decimal Limit => Budget.Amount;
    public bool HasBudget => Limit > 0;
    public bool IsOver => HasBudget && Spent > Limit;
    public string SpentText => MoneyFormat.Yen(Spent);
    // Without a budget the row invites the user to set one, in the accent colour, since tapping the row does that.
    public string BudgetText => HasBudget ? $"/ {MoneyFormat.Yen(Limit)}" : "予算を決める";
    public Color BudgetColor => ThemeTokens.Get(HasBudget ? "InkMuted" : "AccentInk");
    public string BudgetFont => HasBudget ? "ZenMaru" : "ZenMaruBold";
    public double Ratio => HasBudget ? (double)(Spent / Limit) : 0;
    public Color BarColor => IsOver ? ThemeTokens.Get("Deficit") : Style.Bar;
    public Color AmountColor => ThemeTokens.Get(IsOver ? "DeficitInk" : "Ink");
    public string AmountFont => IsOver ? "ZenMaruBold" : "ZenMaruMedium";

    public string Description => $"{Name}、{SpentText} 使用、" + (HasBudget ? $"予算 {MoneyFormat.Yen(Limit)}" : "予算なし") + (IsOver ? "、予算オーバー" : string.Empty);
}

public partial class MainPageViewModel(
    IBiometric biometric,
    AppShell appShell,
    SettingPreferences settings,
    EntryService service,
    GuideService guide) : BaseViewModel
{
    public UndoToastModel Undo { get; } = new();

    DateTime Month => SharedProperty.Instance.SelectedDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocked))]
    [NotifyPropertyChangedFor(nameof(ShowsSummary))]
    [NotifyPropertyChangedFor(nameof(ShowsEmpty))]
    bool _isUnlocked;

    public bool IsLocked => !IsUnlocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsSummary))]
    [NotifyPropertyChangedFor(nameof(ShowsEmpty))]
    bool _isEmptyMonth;

    /// <summary>The guide's tour points at the summary cards, so it shows them (with ¥0) for a month without records too.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsSummary))]
    [NotifyPropertyChangedFor(nameof(ShowsEmpty))]
    bool _isTouring;

    public bool ShowsSummary => IsUnlocked && (!IsEmptyMonth || IsTouring);

    /// <summary>During the tour the cards show <see cref="TourSample"/> amounts instead of the month's own.</summary>
    [ObservableProperty] bool _showsSample;
    public bool ShowsEmpty => IsUnlocked && IsEmptyMonth && !IsTouring;

    [ObservableProperty] string _monthTitle = string.Empty;

    // 残りのお金
    [ObservableProperty] string _remainingText = string.Empty;
    [ObservableProperty] Color _remainingColor = Colors.Transparent;
    [ObservableProperty] string _dailyText = string.Empty;
    [ObservableProperty] string _daysLeftText = string.Empty;
    [ObservableProperty] bool _hasDaysLeft;
    [ObservableProperty] string _livingOffText = string.Empty;
    [ObservableProperty] bool _hasLivingOff;
    [ObservableProperty] string _heroDescription = string.Empty;
    /// <summary>The mascot follows the month's pace (<see cref="PigMoods"/>).</summary>
    [ObservableProperty] string _pigImage = PigMood.Good.Image();

    // 今月のペース
    [ObservableProperty] double _dayRatio;
    [ObservableProperty] double _spentRatio;
    [ObservableProperty] string _dayText = string.Empty;
    [ObservableProperty] string _spentText = string.Empty;
    [ObservableProperty] Color _spentBarColor = Colors.Transparent;
    [ObservableProperty] Color _spentTextColor = Colors.Transparent;
    [ObservableProperty] string _spentFont = "ZenMaru";
    [ObservableProperty] string _paceDescription = string.Empty;

    // 今月使えるお金 and 生活費 are accordions: the heading and its amount, and the rows below it, closed at first.
    [ObservableProperty] bool _isUsableOpen;
    [ObservableProperty] bool _isSpendingOpen;

    [RelayCommand]
    void ToggleUsable() => IsUsableOpen = !IsUsableOpen;

    [RelayCommand]
    void ToggleSpending() => IsSpendingOpen = !IsSpendingOpen;

    // 今月使えるお金
    [ObservableProperty] string _usableText = string.Empty;
    [ObservableProperty] bool _carriesOver;
    [ObservableProperty] string _carryOverText = string.Empty;
    [ObservableProperty] string _incomeText = string.Empty;
    [ObservableProperty] string _savingText = string.Empty;
    [ObservableProperty] string _fixedCostText = string.Empty;

    // 生活費
    [ObservableProperty] string _spendingTotalText = string.Empty;
    [ObservableProperty] string _budgetTotalText = string.Empty;
    [ObservableProperty] string _spendingDescription = string.Empty;
    [ObservableProperty] Color _spendingTotalColor = Colors.Transparent;
    [ObservableProperty] ObservableCollection<CategoryBudgetRow> _categories = [];

    // 特別出費
    [ObservableProperty] string _specialExpenseText = string.Empty;

    // データがない月
    [ObservableProperty] string _emptyTitle = string.Empty;
    [ObservableProperty] int _previousFixedCostCount;
    [ObservableProperty] string _copyFixedCostsText = string.Empty;
    [ObservableProperty] string _copyFixedCostsButtonText = string.Empty;
    public bool CanCopyFixedCosts => PreviousFixedCostCount > 0;
    partial void OnPreviousFixedCostCountChanged(int value) => OnPropertyChanged(nameof(CanCopyFixedCosts));

    public async Task LoadAsync()
    {
        MonthTitle = $"{Month.Year}年{Month.Month}月";
        if (!IsUnlocked) return;

        MonthSummary summary;
        try
        {
            summary = await service.SummarizeAsync(Month, settings.RecordsTotalRemainingToThisMonth);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("読み込めませんでした", "データを読み込めませんでした。アプリを開き直して、もう一度ためしてください。", "OK");
            return;
        }

        IsEmptyMonth = BudgetCalculator.IsEmptyMonth(summary);
        ShowsSample = IsTouring;
        if (ShowsSample) summary = TourSample.Summary(Month, await service.GetCategoriesAsync());

        var pace = BudgetCalculator.Pace(Month, DateTime.Today, summary);

        EmptyTitle = $"{Month.Month}月はまだまっしろです";
        PreviousFixedCostCount = summary.FixedCostTotal == 0 ? await service.CountFixedCostsAsync(Month.AddMonths(-1)) : 0;
        CopyFixedCostsText = $"{Month.AddMonths(-1).Month}月の固定費が{PreviousFixedCostCount}件あります";
        CopyFixedCostsButtonText = $"{Month.Month}月にコピーする";

        RemainingText = MoneyFormat.Yen(summary.Remaining);
        RemainingColor = ThemeTokens.Get(summary.Remaining < 0 ? "DeficitInk" : "Ink");
        DailyText = MoneyFormat.Yen(pace.DailyAllowance);
        HasDaysLeft = pace.RemainingDays > 0;
        DaysLeftText = $"あと{pace.RemainingDays}日";
        HasLivingOff = summary.LivingOff > 0;
        LivingOffText = $"うち切り崩し {MoneyFormat.Yen(summary.LivingOff)}";
        HeroDescription = $"残りのお金 {RemainingText}、1日あたり {DailyText}" + (HasDaysLeft ? $"、{DaysLeftText}" : string.Empty)
            + (HasLivingOff ? $"、{LivingOffText}" : string.Empty);

        DayRatio = (double)pace.DayRatio;
        SpentRatio = (double)pace.SpentRatio;
        DayText = $"{pace.ElapsedDays}/{pace.DaysInMonth}日";
        SpentText = $"{Math.Round(pace.SpentRatio * 100, MidpointRounding.AwayFromZero)}%";
        SpentBarColor = ThemeTokens.Get(pace.IsAhead ? "Deficit" : "Accent");
        SpentTextColor = ThemeTokens.Get(pace.IsAhead ? "DeficitInk" : "InkMuted");
        SpentFont = pace.IsAhead ? "ZenMaruBold" : "ZenMaru";
        PaceDescription = $"今月のペース。日数 {pace.ElapsedDays}日経過、{pace.DaysInMonth}日中。支出 {SpentText}" + (pace.IsAhead ? "、日数より早めです" : string.Empty);
        PigImage = PigMoods.For(summary, pace).Image();

        UsableText = MoneyFormat.Yen(summary.UsableMoney);
        CarriesOver = summary.CarriesOver;
        CarryOverText = MoneyFormat.Yen(summary.RemainingUntilPreviousMonth);
        IncomeText = MoneyFormat.Yen(summary.IncomeTotal);
        SavingText = MoneyFormat.Deduction(summary.SavingTotal);
        FixedCostText = MoneyFormat.Deduction(summary.FixedCostTotal);

        SpendingTotalText = MoneyFormat.Yen(summary.SpendingTotal);
        BudgetTotalText = $" / 予算 {MoneyFormat.Yen(summary.BudgetTotal)}";
        SpendingDescription = $"生活費 {SpendingTotalText}{BudgetTotalText}";
        SpendingTotalColor = ThemeTokens.Get(summary.BudgetTotal > 0 && summary.SpendingTotal > summary.BudgetTotal ? "DeficitInk" : "Ink");
        Categories = new ObservableCollection<CategoryBudgetRow>(summary.Budgets.Select(b => new CategoryBudgetRow(EntryService.Row(b))));

        SpecialExpenseText = MoneyFormat.Yen(summary.SpecialExpenseTotal);
    }

    public async Task AuthenticationRequest()
    {
        if (settings.IsFingerprintEnabled)
        {
            var result = await biometric.AuthenticateAsync(new AuthenticationRequest
            {
                Title = "認証",
                Description = "生体認証を行います",
                NegativeText = "キャンセル",
            }, CancellationToken.None);

            if (!result.Status.Equals(BiometricResponseStatus.Success))
            {
                await LoadAsync();
                return;
            }
        }

        IsUnlocked = true;
        SharedProperty.Instance.IsUnlocked = true;
        appShell.SetTabVisibility(true);
        await LoadAsync();

        // The guide is shown after authentication: see docs/HOW_TO.md「使い方」.
        await guide.ShowIfNeededAsync();
    }

    [RelayCommand]
    Task AuthenticateAsync() => AuthenticationRequest();

    [RelayCommand]
    async Task PreviousMonthAsync() => await MoveMonthAsync(-1);

    [RelayCommand]
    async Task NextMonthAsync() => await MoveMonthAsync(1);

    async Task MoveMonthAsync(int step)
    {
        var next = Month.AddMonths(step);
        if (next < EntrySheetViewModel.MinimumDate || next > EntrySheetViewModel.MaximumDate) return;
        await SelectMonthAsync(next);
    }

    public async Task SelectMonthAsync(DateTime month)
    {
        Undo.Dismiss();
        SharedProperty.Instance.SelectedDate = new DateTime(month.Year, month.Month, 1);
        await LoadAsync();
        SemanticScreenReader.Default.Announce(MonthTitle);
    }

    [RelayCommand]
    async Task OpenListAsync(EntryKind kind) =>
        await Shell.Current.GoToAsync(EntryListPage.Route, new ShellNavigationQueryParameters { ["kind"] = kind });

    [RelayCommand]
    async Task OpenSpecialExpensesAsync() => await Shell.Current.GoToAsync("//" + FloatingTabBar.SpecialExpenseRoute, false);

    [RelayCommand]
    async Task AddAsync(EntryKind kind)
    {
        Undo.Dismiss();
        var vm = new EntrySheetViewModel(service, kind, Month, null, await service.GetCategoriesAsync());
        await EntrySheet.ShowAsync(vm);
        if (vm.Outcome == SheetOutcome.Saved) await LoadAsync();
    }

    [RelayCommand]
    async Task EditBudgetAsync(CategoryBudgetRow row)
    {
        Undo.Dismiss();
        var vm = new EntrySheetViewModel(service, EntryKind.Budget, Month, row.Budget, []);
        var outcome = await EntrySheet.ShowAsync(vm);
        if (outcome == SheetOutcome.Saved) await LoadAsync();
        if (outcome != SheetOutcome.Deleted) return;

        try
        {
            await service.DeleteAsync(row.Budget);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("けせませんでした", "時間をおいて、もう一度ためしてください。", "OK");
            return;
        }

        await LoadAsync();
        Undo.Show($"{row.Name}の予算をけしました", async () =>
        {
            await service.RestoreAsync(row.Budget);
            await LoadAsync();
        });
    }

    [RelayCommand]
    async Task CopyFixedCostsAsync()
    {
        var copied = await service.CopyFixedCostsFromPreviousMonthAsync(Month);
        await LoadAsync();
        SemanticScreenReader.Default.Announce($"{copied}件の固定費をコピーしました");
    }
}
