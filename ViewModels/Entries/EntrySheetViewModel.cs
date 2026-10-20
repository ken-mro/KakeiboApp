using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Services;

namespace KakeiboApp.ViewModels.Entries;

public enum SheetOutcome { None, Saved, Deleted }

/// <summary>The fields of the sheet that can have a problem, top to bottom.</summary>
public enum SheetField { Date, Category, Amount }

/// <summary>What <see cref="EntrySheetViewModel"/>'s check found: the entry to save, or the fields to fix.</summary>
public readonly record struct SheetCheck(EntryDraft? Draft, IReadOnlyList<SheetField> Problems);

/// <summary>
/// A category tile in the spending sheet (「なにに使った？」).
/// </summary>
public partial class CategoryOption(string name) : ObservableObject
{
    public string Name { get; } = name;
    public CategoryStyle Style { get; } = CategoryStyle.For(name);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    bool _isSelected;

    public string Description => IsSelected ? $"{Name}、選択中" : Name;
}

/// <summary>
/// The sheet that slides up from the bottom to add or edit any kind of entry, a month's withdrawal from a
/// saving, or a category's budget.
/// </summary>
public partial class EntrySheetViewModel : BaseViewModel
{
    public static readonly DateTime MinimumDate = new(2020, 1, 1);
    public static readonly DateTime MaximumDate = new(2100, 1, 1);

    readonly EntryService _service;
    readonly EntryRow? _row;

    public EntrySheetViewModel(EntryService service, EntryKind kind, DateTime month, EntryRow? row, IEnumerable<Category> categories)
    {
        _service = service;
        _row = row;
        Kind = kind;
        Month = month;

        Categories = new ObservableCollection<CategoryOption>(categories.Select(c => new CategoryOption(c.Name)));

        if (row is null)
        {
            Date = kind switch
            {
                // Same as the old 生活費の登録 tab: today.
                EntryKind.Spending => DateTime.Today,
                EntryKind.SpecialExpense => BudgetCalculator.DefaultSpecialExpenseDate(month, DateTime.Today),
                _ => month,
            };
            Select(Categories.FirstOrDefault());
        }
        else
        {
            Date = row.Date;
            Name = row.Name;
            Note = row.Note;
            FromWhere = row.FromWhere;
            AmountText = MoneyFormat.ForInput(kind == EntryKind.SavingBalance ? row.Secondary : row.Amount);
            Select(Categories.FirstOrDefault(c => c.Name == row.Category) ?? Categories.FirstOrDefault());
        }
        _saved = Snapshot();
    }

    /// <summary>The fields as they were when the sheet opened, or right after the last 登録.</summary>
    string _saved;

    string Snapshot() => string.Join('\u0001', Date.Date.ToString("O"), SelectedCategory, Name, AmountText, FromWhere, Note);

    /// <summary>Something is typed or changed and not saved: leaving the sheet would lose it.</summary>
    public bool HasUnsavedInput => Snapshot() != _saved;

    public EntryKind Kind { get; }
    public DateTime Month { get; }
    /// <summary>Editing a row that can be deleted shows けす beside 保存.</summary>
    public bool CanDelete => _row is { CanDelete: true };

    /// <summary>Set by the sheet so the view model can close it.</summary>
    public Func<Task>? CloseAsync { get; set; }

    public SheetOutcome Outcome { get; private set; }

    public string Heading => Kind switch
    {
        EntryKind.SavingBalance => $"{_row?.Title}から切り崩す",
        EntryKind.Budget => $"{_row?.Title}の予算",
        _ when _row is not null => Kind.EditTitle(),
        _ => Kind.AddTitle(),
    };

    /// <summary>金額, like every other field's short label; the two amount-only sheets say which amount they ask for.</summary>
    public string AmountLabel => Kind switch
    {
        EntryKind.FixedCost => "毎月の金額",
        EntryKind.SavingBalance => "切り崩す金額",
        EntryKind.Budget => $"{Month.Month}月の予算",
        _ => "金額",
    };

    public string? Hint => Kind switch
    {
        EntryKind.SavingBalance => $"{MoneyFormat.Yen(_row?.Amount ?? 0)} まで切り崩せます。切り崩したお金は、貯金から今月の生活に回したお金として、残りのお金に入ります。",
        EntryKind.Budget => BudgetHint(),
        EntryKind.SpecialExpense => "特別出費は別枠です。残りのお金には入りません。",
        _ => null,
    };

    public bool HasHint => Hint is not null;

    /// <summary>The kind's budget in the previous month, for 「9月と同じ」 on the budget sheet; 0 without one.</summary>
    public decimal PreviousAmount { get; init; }

    /// <summary>「9月と同じ ¥50,000」: shown while the amount typed is not already that.</summary>
    public bool ShowsPreviousAmount => Kind == EntryKind.Budget && PreviousAmount > 0
        && !(MoneyFormat.TryParseAmount(AmountText, out var amount) && amount == PreviousAmount);

    public string PreviousAmountText => $"{Month.AddMonths(-1).Month}月と同じ {MoneyFormat.Yen(PreviousAmount)}";

    [RelayCommand]
    void UsePreviousAmount() => AmountText = MoneyFormat.ForInput(PreviousAmount);

    // The old grid showed 残りの予算 and 1日あたり per category; the sheet keeps them, one figure per line.
    string BudgetHint()
    {
        var used = $"{Month.Month}月の{_row?.Title}: {MoneyFormat.Yen(_row?.Secondary ?? 0)}";
        if (_row?.Source is not Models.BudgetControlResult { MonthlyBudget.Amount: > 0 } budget) return used;
        return $"{used}\n残りの予算: {MoneyFormat.Yen(budget.RemainingBudget)}(1日あたり {MoneyFormat.Yen(budget.DailyRemainingBudget)})";
    }

    public string NamePlaceholder => Kind switch
    {
        EntryKind.Income => "例：給料",
        EntryKind.Saving => "例：旅行",
        EntryKind.FixedCost => "例：家賃",
        EntryKind.SpecialExpense => "例：結婚式のご祝儀",
        _ => "例：スーパー",
    };

    public bool ShowsCategories => Kind == EntryKind.Spending;
    public bool ShowsNameAndNote => Kind is not (EntryKind.SavingBalance or EntryKind.Budget);
    public bool ShowsDate => Kind.IsDated();
    public bool ShowsFromWhere => Kind == EntryKind.SpecialExpense;
    public string SaveText => _row is null ? "登録" : "保存";

    public ObservableCollection<CategoryOption> Categories { get; }

    static string DayText(DateTime date) => Services.EntryService.DayHeader(date);

    /// <summary>今日 / 昨日: the two days most entries are for, one tap each instead of the date picker.</summary>
    [RelayCommand]
    void SetDay(string daysAgo) => Date = DateTime.Today.AddDays(-int.Parse(daysAgo, System.Globalization.CultureInfo.InvariantCulture));

    public bool IsToday => Date.Date == DateTime.Today;
    public bool IsYesterday => Date.Date == DateTime.Today.AddDays(-1);

    partial void OnDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(IsToday));
        OnPropertyChanged(nameof(IsYesterday));
        DateError = string.Empty;
    }

    // A field's message goes away as soon as that field is changed.
    partial void OnAmountTextChanged(string value)
    {
        AmountError = string.Empty;
        OnPropertyChanged(nameof(ShowsPreviousAmount));
    }

    [ObservableProperty]
    string _amountText = string.Empty;

    [ObservableProperty]
    string _name = string.Empty;

    [ObservableProperty]
    string _note = string.Empty;

    [ObservableProperty]
    string _fromWhere = string.Empty;

    [ObservableProperty]
    DateTime _date;

    // Each field shows its own problem under it, with a red outline, so the whole form shows where to fix it.

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDateError))]
    string _dateError = string.Empty;

    public bool HasDateError => DateError.Length > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCategoryError))]
    string _categoryError = string.Empty;

    public bool HasCategoryError => CategoryError.Length > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAmountError))]
    string _amountError = string.Empty;

    public bool HasAmountError => AmountError.Length > 0;

    /// <summary>A problem that is not one field's: saving failed.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSaveError))]
    string _saveError = string.Empty;

    public bool HasSaveError => SaveError.Length > 0;

    /// <summary>Set by the sheet to bring the first field with a problem into view.</summary>
    public Action<SheetField>? ShowField { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSavedText))]
    string _savedText = string.Empty;

    public bool HasSavedText => !string.IsNullOrEmpty(SavedText);

    public string? SelectedCategory => SelectedOption?.Name;

    /// <summary>The kind picked in the 種類 dropdown.</summary>
    public CategoryOption? SelectedOption
    {
        get => Categories.FirstOrDefault(c => c.IsSelected);
        set => Select(value);
    }

    void Select(CategoryOption? option)
    {
        foreach (var c in Categories) c.IsSelected = ReferenceEquals(c, option);
        if (option is not null) CategoryError = string.Empty;
        OnPropertyChanged(nameof(SelectedOption));
    }

    /// <summary>
    /// Checks every field, sets each one's message, and returns what to save, or null with the fields to fix
    /// listed top to bottom in <see cref="SheetCheck.Problems"/>.
    /// </summary>
    SheetCheck Check()
    {
        var problems = new List<SheetField>();

        DateError = ShowsDate && (Date < MinimumDate || Date > MaximumDate)
            ? "日にちは2020年1月1日から2100年1月1日までにしてください。" : string.Empty;
        if (HasDateError) problems.Add(SheetField.Date);

        CategoryError = ShowsCategories && SelectedCategory is null ? "種類を選んでください。" : string.Empty;
        if (HasCategoryError) problems.Add(SheetField.Category);

        var hasAmount = MoneyFormat.TryParseAmount(AmountText, out var amount);
        AmountError = !hasAmount ? "金額を入力してください。1円以上の金額を登録できます。"
            : Kind == EntryKind.SavingBalance && _row is not null && amount > _row.Amount ? $"切り崩せるのは {MoneyFormat.Yen(_row.Amount)} までです。"
            : string.Empty;
        if (HasAmountError) problems.Add(SheetField.Amount);

        SaveError = string.Empty;
        if (problems.Count > 0) return new SheetCheck(null, problems);
        return new SheetCheck(new EntryDraft
        {
            Date = ShowsDate ? Date.Date : Date,
            Name = Name.Trim(),
            Note = EntryRow.CleanNote(Note),
            FromWhere = FromWhere.Trim(),
            Amount = amount,
            Category = ShowsCategories ? SelectedCategory : _row?.Category,
        }, problems);
    }

    string MessageOf(SheetField field) => field switch
    {
        SheetField.Date => DateError,
        SheetField.Category => CategoryError,
        _ => AmountError,
    };

    /// <summary>
    /// 登録 adds the entry and keeps the sheet open for the next one, the fields cleared but the date and kind kept
    /// (the sheet is closed with ×); 保存 of an entry being edited saves it and closes the sheet.
    /// </summary>
    [RelayCommand]
    async Task SaveAsync()
    {
        var keepOpen = _row is null;
        if (IsBusy) return;
        var (draft, problems) = Check();
        if (draft is null)
        {
            ShowField?.Invoke(problems[0]);
            SemanticScreenReader.Default.Announce(string.Join("。", problems.Select(MessageOf)));
            return;
        }

        try
        {
            IsBusy = true;
            if (_row is null) await _service.AddAsync(Kind, draft);
            else await _service.SaveAsync(_row, draft);
            Outcome = SheetOutcome.Saved;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            SaveError = "保存できませんでした。時間をおいて、もう一度ためしてください。";
            SemanticScreenReader.Default.Announce(SaveError);
            return;
        }
        finally
        {
            IsBusy = false;
        }

        if (!keepOpen)
        {
            if (CloseAsync is not null) await CloseAsync();
            return;
        }

        var title = string.IsNullOrEmpty(draft.Name) ? draft.Category ?? Kind.Name() : draft.Name;
        SavedText = ShowsDate
            ? $"{DayText(draft.Date)}に「{title}」{MoneyFormat.Yen(draft.Amount)} を登録しました"
            : $"「{title}」{MoneyFormat.Yen(draft.Amount)} を登録しました";
        SemanticScreenReader.Default.Announce(SavedText);
        AmountText = string.Empty;
        Name = string.Empty;
        Note = string.Empty;
        FromWhere = string.Empty;
        _saved = Snapshot();
        // The date and category carry over to the next entry. The keyboard stays closed, so the message and the
        // whole form are in view; the next entry starts with a tap on a field.
    }

    /// <summary>Delete is done by the page that opened the sheet, so it can offer もとに戻す.</summary>
    [RelayCommand]
    async Task DeleteAsync()
    {
        Outcome = SheetOutcome.Deleted;
        if (CloseAsync is not null) await CloseAsync();
    }

    [RelayCommand]
    async Task DismissAsync()
    {
        if (CloseAsync is not null) await CloseAsync();
    }
}
