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
    [NotifyPropertyChangedFor(nameof(TitleLead))]
    [NotifyPropertyChangedFor(nameof(TitleTail))]
    string _title = string.Empty;

    /// <summary>The title in two parts, so that it breaks only after 「の」 (<see cref="TitleBreak"/>).</summary>
    public string TitleLead => TitleBreak.Split(Title).Lead;
    public string TitleTail => TitleBreak.Split(Title).Tail;

    [ObservableProperty]
    ObservableCollection<EntryGroup> _groups = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows))]
    [NotifyPropertyChangedFor(nameof(HasNote))]
    [NotifyPropertyChangedFor(nameof(ShowsCopyPreviousLink))]
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

    /// <summary>＋ つける is hidden while searching, and on the savings breakdown, which is not added to.</summary>
    public bool ShowsAdd => Kind.CanAdd() && !IsSearching;

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

    /// <summary>
    /// The month's 残りのお金, on the savings breakdown: how much to withdraw so the month is not in the red.
    /// Null elsewhere.
    /// </summary>
    decimal? _monthRemaining;

    public bool HasMonthRemaining => _monthRemaining is not null;
    public string MonthRemainingLabel => $"{Month.Month}月の残りのお金";
    public string MonthRemainingText => MoneyFormat.Yen(_monthRemaining ?? 0);
    public Color MonthRemainingColor => ThemeTokens.Get(_monthRemaining < 0 ? "DeficitInk" : "Ink");
    public bool IsInTheRed => _monthRemaining < 0;
    public string InTheRedText => $"{MoneyFormat.Yen(-(_monthRemaining ?? 0))} 切り崩すと、赤字がなくなります。";
    public string MonthRemainingDescription => $"{MonthRemainingLabel} {MonthRemainingText}" + (IsInTheRed ? $"。{InTheRedText}" : string.Empty);

    void SetMonthRemaining(decimal? value)
    {
        _monthRemaining = value;
        foreach (var name in new[] { nameof(HasMonthRemaining), nameof(MonthRemainingLabel), nameof(MonthRemainingText),
                     nameof(MonthRemainingColor), nameof(IsInTheRed), nameof(InTheRedText), nameof(MonthRemainingDescription) })
        {
            OnPropertyChanged(name);
        }
    }

    /// <summary>「9月の固定費が2件あります」 on the lists of 収入, 貯金 and 固定費.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCopyPrevious))]
    [NotifyPropertyChangedFor(nameof(ShowsCopyPreviousLink))]
    int _previousCount;

    public bool CanCopyPrevious => EntryService.CopyableKinds.Contains(Kind) && PreviousCount > 0;
    public string CopyPreviousText => $"{Month.AddMonths(-1).Month}月の{Kind.Name()}が{PreviousCount}件あります";
    public string CopyPreviousButtonText => $"{Month.Month}月にコピーする";
    /// <summary>Under the rows; an empty list has the card with the copy button instead.</summary>
    public bool ShowsCopyPreviousLink => CanCopyPrevious && HasRows;
    public string KindGlyph => Kind.Glyph();
    public string CopyPreviousLinkText => $"{Month.AddMonths(-1).Month}月の{Kind.Name()}をコピーする";

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
            decimal? monthRemaining = null;
            if (!ShowsSample && Kind is EntryKind.Spending or EntryKind.SavingBalance)
            {
                var summary = await service.SummarizeAsync(Month, settings.RecordsTotalRemainingToThisMonth);
                IsTotalOver = Kind == EntryKind.Spending && summary.BudgetTotal > 0 && page.Total > summary.BudgetTotal;
                if (Kind == EntryKind.SavingBalance)
                {
                    monthRemaining = summary.Remaining;
                    if (!summary.CarriesOver) RemainingUntilPreviousMonthText = MoneyFormat.Yen(summary.RemainingUntilPreviousMonth);
                }
            }
            SetMonthRemaining(monthRemaining);

            PreviousCount = EntryService.CopyableKinds.Contains(Kind) ? await service.CountAsync(Kind, Month.AddMonths(-1)) : 0;
            OnPropertyChanged(nameof(CopyPreviousText));
            OnPropertyChanged(nameof(CopyPreviousButtonText));
            OnPropertyChanged(nameof(CopyPreviousLinkText));
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
        SetMonthRemaining(null);
        PreviousCount = 0;
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
    async Task EditAsync(EntryRow row)
    {
        Undo.Dismiss();
        var vm = new EntrySheetViewModel(service, Kind, Month, row, await service.GetCategoriesAsync())
        {
            MonthRemaining = _monthRemaining,
        };
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
    async Task CopyPreviousAsync()
    {
        if (!CanCopyPrevious) return;
        Undo.Dismiss();
        var previous = Month.AddMonths(-1);
        // A month that already has rows gets the copies next to them, so ask first.
        if (HasRows && !await Shell.Current.DisplayAlertAsync(
                $"{previous.Month}月の{Kind.Name()}をコピーしますか？",
                $"{Month.Month}月に{PreviousCount}件を追加します。いま入っている{CountText}は、そのまま残ります。", "コピーする", "やめる"))
        {
            return;
        }

        int copied;
        try
        {
            copied = await service.CopyFromPreviousMonthAsync(Kind, Month);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("コピーできませんでした", "時間をおいて、もう一度ためしてください。", "OK");
            await LoadAsync();
            return;
        }
        await LoadAsync();
        SemanticScreenReader.Default.Announce($"{copied}件の{Kind.Name()}をコピーしました");
    }
}
