using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using KakeiboApp.Services;

namespace KakeiboApp.ViewModels;

/// <summary>A kind on the 予算をまとめて決める page: its budget as typed, and what was spent so far.</summary>
public partial class BudgetLine(string name, decimal budget, decimal spent, Action changed) : ObservableObject
{
    public string Name { get; } = name;
    public CategoryStyle Style { get; } = CategoryStyle.For(name);
    public string SpentText { get; } = $"使った金額 {MoneyFormat.Yen(spent)}";

    [ObservableProperty]
    string _amountText = MoneyFormat.ForInput(budget);

    partial void OnAmountTextChanged(string value) => changed();

    /// <summary>The amount typed; blank (or not a number) is no budget.</summary>
    public decimal Amount => MoneyFormat.TryParseAmount(AmountText, out var amount) ? amount : 0;

    public bool IsBlank => Amount == 0;

    public string Description => $"{Name}の予算";
}

/// <summary>
/// 予算をまとめて決める: every kind's budget on one page, with what is left of the money for living if each is spent
/// in full. A total can be set first (予算の総額), to see how much of it is not given to a kind yet and split that
/// among the kinds left blank.
/// </summary>
public partial class BudgetsViewModel(EntryService service, SettingPreferences settings) : BaseViewModel
{
    DateTime Month => SharedProperty.Instance.SelectedDate;

    public string Title => $"{Month.Year}年{Month.Month}月の予算";

    public ObservableCollection<BudgetLine> Lines { get; } = [];

    /// <summary>今月使えるお金 + 切り崩し: what the budgets are spent from, as the home's 残りのお金.</summary>
    decimal _available;

    public string AvailableText => MoneyFormat.Yen(_available);

    [ObservableProperty]
    string _totalText = string.Empty;

    partial void OnTotalTextChanged(string value) => Recalculate();

    decimal? Total => MoneyFormat.TryParseAmount(TotalText, out var total) ? total : null;

    public bool HasTotal => Total is not null;

    decimal Sum => Lines.Sum(l => l.Amount);

    public string SumText => MoneyFormat.Yen(Sum);

    /// <summary>予算の総額 − 予算の合計: what is not given to a kind yet.</summary>
    decimal Unallocated => (Total ?? 0) - Sum;

    public string UnallocatedText => MoneyFormat.Yen(Unallocated);

    public Color UnallocatedColor => ThemeTokens.Get(Unallocated < 0 ? "DeficitInk" : "Ink");

    /// <summary>生活費に使えるお金 − 予算の合計: what is left if every budget is spent in full.</summary>
    decimal LeftOver => _available - Sum;

    public string LeftOverText => MoneyFormat.Yen(LeftOver);

    public Color LeftOverColor => ThemeTokens.Get(LeftOver < 0 ? "DeficitInk" : "Ink");

    public string UseAvailableText => $"使えるお金と同じ {AvailableText}";

    public bool CanUseAvailable => _available > 0 && Total != _available;

    public bool CanSplit => HasTotal && Unallocated >= 100 && Lines.Any(l => l.IsBlank);

    public string SplitText => $"残りを空欄の{Lines.Count(l => l.IsBlank)}種類に分ける";

    void Recalculate()
    {
        foreach (var name in new[]
                 {
                     nameof(HasTotal), nameof(SumText), nameof(UnallocatedText), nameof(UnallocatedColor),
                     nameof(LeftOverText), nameof(LeftOverColor), nameof(CanUseAvailable), nameof(CanSplit), nameof(SplitText),
                 })
        {
            OnPropertyChanged(name);
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            var summary = await service.SummarizeAsync(Month, settings.RecordsTotalRemainingToThisMonth);
            _available = summary.UsableMoney + summary.LivingOff;
            Lines.Clear();
            foreach (var b in summary.Budgets)
            {
                Lines.Add(new BudgetLine(b.Category.Name, b.MonthlyBudget.Amount, b.MonthlySpending, Recalculate));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("読み込めませんでした", "データを読み込めませんでした。アプリを開き直して、もう一度ためしてください。", "OK");
        }
        _loaded = Snapshot();
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(AvailableText));
        OnPropertyChanged(nameof(UseAvailableText));
        Recalculate();
    }

    [RelayCommand]
    void UseAvailable() => TotalText = MoneyFormat.ForInput(_available);

    /// <summary>The kinds left blank each get an equal part of what is not given yet, in hundreds of yen.</summary>
    [RelayCommand]
    void Split()
    {
        if (!CanSplit) return;
        var blanks = Lines.Where(l => l.IsBlank).ToList();
        var share = BudgetCalculator.EvenShare(Unallocated, blanks.Count);
        if (share <= 0) return;
        foreach (var line in blanks) line.AmountText = MoneyFormat.ForInput(share);
    }

    [RelayCommand]
    async Task SaveAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            await service.SaveBudgetsAsync(Month, Lines.ToDictionary(l => l.Name, l => l.Amount));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("保存できませんでした", "時間をおいて、もう一度ためしてください。", "OK");
            return;
        }
        finally
        {
            IsBusy = false;
        }
        SemanticScreenReader.Default.Announce($"{Month.Month}月の予算を保存しました");
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>The budgets as they were loaded, to ask before leaving with changes not saved.</summary>
    string _loaded = string.Empty;

    string Snapshot() => string.Join(';', Lines.Select(l => $"{l.Name}={l.Amount}"));

    public bool HasChanges => Snapshot() != _loaded;

    /// <summary>‹ and the back key: changes not saved are lost, so ask first.</summary>
    [RelayCommand]
    public async Task BackAsync()
    {
        if (HasChanges && !await Shell.Current.DisplayAlertAsync(
                "予算は保存されていません", "保存しないで戻りますか？", "戻る", "やめる"))
        {
            return;
        }
        await Shell.Current.GoToAsync("..");
    }
}
