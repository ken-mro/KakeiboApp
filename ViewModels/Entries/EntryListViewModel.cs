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

    /// <summary>The month (or year) arrows are hidden while searching: results come from every month.</summary>
    public bool IsBrowsing => IsTab && !IsSearching;

    public UndoToastModel Undo { get; } = new();

    /// <summary>While the guide runs, the 生活費 and 特別出費 lists show <see cref="TourSample"/> rows.</summary>
    public bool IsTouring { get; set; }

    public bool ShowsSample { get; private set; }

    public void Initialize(EntryKind kind, bool isTab)
    {
        Kind = kind;
        IsTab = isTab;
        IsSearching = false;
        SearchText = string.Empty;
        OnPropertyChanged(string.Empty);
    }

    DateTime Month => SharedProperty.Instance.SelectedDate;

    [ObservableProperty]
    string _title = string.Empty;

    [ObservableProperty]
    ObservableCollection<EntryGroup> _groups = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows))]
    [NotifyPropertyChangedFor(nameof(HasNote))]
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
        _ when IsSearching => "合計",
        EntryKind.SpecialExpense => "年間の合計",
        EntryKind.SavingBalance => "貯金総額",
        _ => "合計",
    };

    public string SummaryDescription => $"{CountText}、{TotalLabel} {TotalText}";

    [ObservableProperty]
    string _emptyText = string.Empty;

    public bool CanAdd => Kind.CanAdd();
    public bool ShowsAdd => CanAdd && !IsSearching;
    public string AddText => Kind.AddTitle();

    // How to edit and delete is not visible on a row, so the list says it above the rows.
    const string EditHint = "行をタップすると、金額や内容をなおせます。左にスワイプすると、けせます。";

    public bool HasNote => Kind == EntryKind.SpecialExpense || HasRows;
    public string NoteText => Kind switch
    {
        _ when IsSearching => $"{SearchScope}からさがしています。\n" + EditHint,
        EntryKind.SpecialExpense when HasRows => "特別出費は別枠です。残りのお金には入りません。\n" + EditHint,
        EntryKind.SpecialExpense => "特別出費は別枠です。残りのお金には入りません。",
        EntryKind.SavingBalance => "行をタップすると、その貯金から今月切り崩す金額を入れられます。切り崩したお金は、今月の残りのお金に入ります。切り崩しをやめるときは、左にスワイプしてけします。",
        _ => EditHint,
    };

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

    public bool CanSearch => IsTab && Kind is EntryKind.Spending or EntryKind.SpecialExpense;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBrowsing))]
    [NotifyPropertyChangedFor(nameof(ShowsAdd))]
    bool _isSearching;

    [ObservableProperty]
    string _searchText = string.Empty;

    public string SearchDescription => $"{Kind.Name()}をさがす";

    /// <summary>「すべての月の生活費」「すべての年の特別出費」</summary>
    string SearchScope => $"すべての{(Kind == EntryKind.SpecialExpense ? "年" : "月")}の{Kind.Name()}";

    public string SearchPlaceholder => Kind == EntryKind.SpecialExpense
        ? "内容・メモ・払ったところ・金額"
        : "内容・メモ・種類・金額";

    CancellationTokenSource? _searchDelay;

    // Wait for a pause in typing so each key press does not reload the list.
    async partial void OnSearchTextChanged(string value)
    {
        if (!IsSearching) return;
        _searchDelay?.Cancel();
        var delay = _searchDelay = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, delay.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        await LoadAsync();
        if (IsSearching && value == SearchText && !string.IsNullOrWhiteSpace(value)) SemanticScreenReader.Default.Announce($"{CountText}見つかりました");
    }

    [RelayCommand]
    async Task OpenSearchAsync()
    {
        if (!CanSearch || IsSearching) return;
        Undo.Dismiss();
        SearchText = string.Empty;
        IsSearching = true;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task CloseSearchAsync()
    {
        if (!IsSearching) return;
        _searchDelay?.Cancel();
        Undo.Dismiss();
        IsSearching = false;
        SearchText = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            if (IsSearching)
            {
                await LoadSearchAsync();
                return;
            }

            Title = Kind.ListTitle(Month);
            var page = await service.LoadAsync(Kind, Month);
            ShowsSample = IsTouring && Kind is EntryKind.Spending or EntryKind.SpecialExpense;
            if (ShowsSample) page = TourSample.Page(Kind, Month);
            Groups = new ObservableCollection<EntryGroup>(page.Groups);
            IsEmpty = page.Count == 0;
            CountText = $"{page.Count}件";
            TotalText = MoneyFormat.Yen(page.Total);
            EmptyText = Kind.EmptyText(Month);

            IsTotalOver = false;
            RemainingUntilPreviousMonthText = null;
            if (!ShowsSample && Kind is EntryKind.Spending or EntryKind.SavingBalance)
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
            OnPropertyChanged(nameof(NoteText));
            // Re-read the colour token: the theme may have changed while IsTotalOver stayed the same.
            OnPropertyChanged(nameof(TotalColor));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("読み込めませんでした", "データを読み込めませんでした。アプリを開き直して、もう一度ためしてください。", "OK");
        }
    }

    async Task LoadSearchAsync()
    {
        var query = SearchText;
        var page = string.IsNullOrWhiteSpace(query) ? new EntryPage([], 0, 0) : await service.SearchAsync(Kind, query);
        // A newer query (or closing the search) replaced this one while it was loading.
        if (!IsSearching || query != SearchText) return;

        Title = SearchDescription;
        Groups = new ObservableCollection<EntryGroup>(page.Groups);
        IsEmpty = page.Count == 0;
        CountText = $"{page.Count}件";
        TotalText = MoneyFormat.Yen(page.Total);
        EmptyText = string.IsNullOrWhiteSpace(query)
            ? $"さがしたい言葉や金額を入れてください。\n{SearchScope}からさがします。"
            : $"「{query.Trim()}」に当てはまる{Kind.Name()}はありません。";
        IsTotalOver = false;
        RemainingUntilPreviousMonthText = null;
        PreviousFixedCostCount = 0;
        OnPropertyChanged(nameof(TotalLabel));
        OnPropertyChanged(nameof(SummaryDescription));
        OnPropertyChanged(nameof(NoteText));
        OnPropertyChanged(nameof(TotalColor));
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
        if (!ShowsAdd) return;
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
