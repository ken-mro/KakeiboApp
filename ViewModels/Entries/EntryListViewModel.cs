using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using KakeiboApp.Services;
using KakeiboApp.Views.Sheets;

namespace KakeiboApp.ViewModels.Entries;

/// <summary>
/// The shared list for 収入・貯金・固定費・生活費・特別出費 and the savings breakdown
/// (design: project/List.dc.html). Tap a row to edit it in the sheet; swipe it to delete, with もとに戻す.
/// </summary>
public partial class EntryListViewModel(EntryService service, SettingPreferences settings) : BaseViewModel
{
    public EntryKind Kind { get; private set; } = EntryKind.Spending;

    /// <summary>A tab page has month arrows and the tab bar; a page opened from the home has a back button.</summary>
    public bool IsTab { get; private set; }
    public bool IsPushed => !IsTab;

    public UndoToastModel Undo { get; } = new();

    public void Initialize(EntryKind kind, bool isTab)
    {
        Kind = kind;
        IsTab = isTab;
        OnPropertyChanged(string.Empty);
    }

    DateTime Month => SharedProperty.Instance.SelectedDate;

    [ObservableProperty]
    string _title = string.Empty;

    [ObservableProperty]
    ObservableCollection<EntryGroup> _groups = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows))]
    bool _isEmpty;

    public bool HasRows => !IsEmpty;

    [ObservableProperty]
    string _countText = string.Empty;

    [ObservableProperty]
    string _totalText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalColor))]
    bool _isTotalOver;

    public Color TotalColor => ThemeTokens.Get(IsTotalOver ? "DeficitInk" : "Ink");

    public string TotalLabel => Kind switch
    {
        EntryKind.SpecialExpense => "年間の合計",
        EntryKind.SavingBalance => "貯金総額",
        _ => "合計",
    };

    public string SummaryDescription => $"{CountText}、{TotalLabel} {TotalText}";

    [ObservableProperty]
    string _emptyText = string.Empty;

    public bool CanAdd => Kind.CanAdd();
    public string AddText => Kind.AddTitle();

    public bool HasNote => Kind == EntryKind.SpecialExpense || Kind == EntryKind.SavingBalance;
    public string NoteText => Kind == EntryKind.SpecialExpense
        ? "特別出費は別枠です。残りのお金には入りません。"
        : "行をタップすると、その貯金から今月切り崩す金額を入れられます。切り崩したお金は、今月の残りのお金に入ります。";

    /// <summary>Shown on the savings breakdown when the remaining money is not carried over to the next month.</summary>
    [ObservableProperty]
    string? _remainingUntilPreviousMonthText;

    public bool HasRemainingUntilPreviousMonth => RemainingUntilPreviousMonthText is not null;

    partial void OnRemainingUntilPreviousMonthTextChanged(string? value) => OnPropertyChanged(nameof(HasRemainingUntilPreviousMonth));

    /// <summary>「9月の固定費が2件あります」 on the fixed-cost list.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCopyFixedCosts))]
    int _previousFixedCostCount;

    public bool CanCopyFixedCosts => Kind == EntryKind.FixedCost && PreviousFixedCostCount > 0;
    public string CopyFixedCostsText => $"{Month.AddMonths(-1).Month}月の固定費が{PreviousFixedCostCount}件あります";
    public string CopyFixedCostsButtonText => $"{Month.Month}月にコピーする";

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            Title = Kind.ListTitle(Month);
            var page = await service.LoadAsync(Kind, Month);
            Groups = new ObservableCollection<EntryGroup>(page.Groups);
            IsEmpty = page.Count == 0;
            CountText = $"{page.Count}件";
            TotalText = MoneyFormat.Yen(page.Total);
            EmptyText = Kind.EmptyText(Month);

            IsTotalOver = false;
            RemainingUntilPreviousMonthText = null;
            if (Kind is EntryKind.Spending or EntryKind.SavingBalance)
            {
                var summary = await service.SummarizeAsync(Month, settings.RecordsTotalRemainingToThisMonth);
                IsTotalOver = Kind == EntryKind.Spending && summary.BudgetTotal > 0 && page.Total > summary.BudgetTotal;
                if (Kind == EntryKind.SavingBalance && !summary.CarriesOver)
                {
                    RemainingUntilPreviousMonthText = MoneyFormat.Yen(summary.RemainingUntilPreviousMonth);
                }
            }

            PreviousFixedCostCount = Kind == EntryKind.FixedCost ? await service.CountFixedCostsAsync(Month.AddMonths(-1)) : 0;
            OnPropertyChanged(nameof(CopyFixedCostsText));
            OnPropertyChanged(nameof(CopyFixedCostsButtonText));
            OnPropertyChanged(nameof(SummaryDescription));
            // Re-read the colour token: the theme may have changed while IsTotalOver stayed the same.
            OnPropertyChanged(nameof(TotalColor));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("読み込めませんでした", "データを読み込めませんでした。アプリを開き直して、もう一度ためしてください。", "OK");
        }
    }

    [RelayCommand]
    async Task PreviousAsync() => await MoveAsync(-1);

    [RelayCommand]
    async Task NextAsync() => await MoveAsync(1);

    async Task MoveAsync(int step)
    {
        Undo.Dismiss();
        var months = Kind == EntryKind.SpecialExpense ? 12 * step : step;
        var next = Month.AddMonths(months);
        if (next < EntrySheetViewModel.MinimumDate || next > EntrySheetViewModel.MaximumDate) return;
        SharedProperty.Instance.SelectedDate = next;
        await LoadAsync();
        SemanticScreenReader.Default.Announce(Title);
    }

    public string PreviousDescription => Kind == EntryKind.SpecialExpense ? "前の年" : "前の月";
    public string NextDescription => Kind == EntryKind.SpecialExpense ? "次の年" : "次の月";

    [RelayCommand]
    async Task BackAsync() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    async Task AddAsync()
    {
        if (!CanAdd) return;
        Undo.Dismiss();
        var vm = new EntrySheetViewModel(service, Kind, Month, null, await service.GetCategoriesAsync());
        await EntrySheet.ShowAsync(vm);
        if (vm.Outcome == SheetOutcome.Saved) await LoadAsync();
    }

    [RelayCommand]
    async Task EditAsync(EntryRow row)
    {
        Undo.Dismiss();
        var vm = new EntrySheetViewModel(service, Kind, Month, row, await service.GetCategoriesAsync());
        var outcome = await EntrySheet.ShowAsync(vm);
        if (outcome == SheetOutcome.Deleted) await DeleteAsync(row);
        else if (outcome == SheetOutcome.Saved) await LoadAsync();
    }

    [RelayCommand]
    async Task DeleteAsync(EntryRow row)
    {
        if (!row.CanDelete) return;
        try
        {
            await service.DeleteAsync(row);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("けせませんでした", "時間をおいて、もう一度ためしてください。", "OK");
            return;
        }

        await LoadAsync();
        Undo.Show(row.DeletedMessage, async () =>
        {
            await service.RestoreAsync(row);
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
