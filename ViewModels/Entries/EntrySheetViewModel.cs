using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Services;

namespace KakeiboApp.ViewModels.Entries;

public enum SheetOutcome { None, Saved, Deleted }

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
            // Editing an entry that already has a note or another day opens the details straight away.
            ShowsDetails = !string.IsNullOrEmpty(row.Name) || !string.IsNullOrEmpty(row.Note) || row.Date.Date != DateTime.Today;
        }
    }

    public EntryKind Kind { get; }
    public DateTime Month { get; }
    /// <summary>Editing a row that can be deleted shows けす instead of つづけて登録.</summary>
    public bool CanDelete => _row is { CanDelete: true };

    /// <summary>Set by the sheet so the view model can close it.</summary>
    public Func<Task>? CloseAsync { get; set; }

    /// <summary>Set by the sheet so the amount field gets the focus again after つづけて登録.</summary>
    public Action? FocusAmount { get; set; }

    public SheetOutcome Outcome { get; private set; }

    public string Heading => Kind switch
    {
        EntryKind.SavingBalance => $"{_row?.Title}から切り崩す",
        EntryKind.Budget => $"{_row?.Title}の予算",
        _ when _row is not null => Kind.EditTitle(),
        _ => Kind.AddTitle(),
    };

    public string AmountLabel => Kind switch
    {
        EntryKind.Income => "いくら入った？",
        EntryKind.Saving => "いくら貯めた？",
        EntryKind.FixedCost => "毎月いくら？",
        EntryKind.SavingBalance => "いくら切り崩す？",
        EntryKind.Budget => $"{Month.Month}月の予算はいくら？",
        _ => "いくら使った？",
    };

    public string? Hint => Kind switch
    {
        EntryKind.SavingBalance => $"{MoneyFormat.Yen(_row?.Amount ?? 0)} まで切り崩せます。切り崩したお金は、貯金から今月の生活に回したお金として、残りのお金に入ります。",
        EntryKind.Budget => BudgetHint(),
        EntryKind.SpecialExpense => "特別出費は別枠です。残りのお金には入りません。",
        _ => null,
    };

    public bool HasHint => Hint is not null;

    // The old grid showed 残りの予算 and 1日あたり per category; the sheet keeps them.
    string BudgetHint()
    {
        var used = $"{Month.Month}月の{_row?.Title}は、いま {MoneyFormat.Yen(_row?.Secondary ?? 0)} 使っています。";
        if (_row?.Source is not Models.BudgetControlResult { MonthlyBudget.Amount: > 0 } budget) return used;
        return $"{used}\n残りの予算 {MoneyFormat.Yen(budget.RemainingBudget)}（1日あたり {MoneyFormat.Yen(budget.DailyRemainingBudget)}）";
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
    /// <summary>Only the spending sheet folds its optional fields away, to keep it to amount → category.</summary>
    public bool FoldsDetails => Kind == EntryKind.Spending;
    public bool ShowsDetailFields => ShowsNameAndNote && (!FoldsDetails || ShowsDetails);
    public bool CanContinue => _row is null;
    public string SaveText => _row is null ? "登録" : "保存";

    public ObservableCollection<CategoryOption> Categories { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsDetailFields))]
    [NotifyPropertyChangedFor(nameof(DetailsToggleText))]
    bool _showsDetails;

    public string DetailsToggleText => ShowsDetails ? "メモや日付をとじる" : "メモや日付も書く（なくてもOK）";

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    string _errorText = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorText);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSavedText))]
    string _savedText = string.Empty;

    public bool HasSavedText => !string.IsNullOrEmpty(SavedText);

    public string? SelectedCategory => Categories.FirstOrDefault(c => c.IsSelected)?.Name;

    [RelayCommand]
    void SelectCategory(CategoryOption option) => Select(option);

    void Select(CategoryOption? option)
    {
        foreach (var c in Categories) c.IsSelected = ReferenceEquals(c, option);
    }

    [RelayCommand]
    void ToggleDetails() => ShowsDetails = !ShowsDetails;

    /// <summary>Checks the input and returns what to save, or sets <see cref="ErrorText"/>.</summary>
    EntryDraft? Validate()
    {
        if (!MoneyFormat.TryParseAmount(AmountText, out var amount))
        {
            ErrorText = "金額を入力してください。1円以上の金額を登録できます。";
            return null;
        }

        if (Kind == EntryKind.SavingBalance && _row is not null && amount > _row.Amount)
        {
            ErrorText = $"切り崩せるのは {MoneyFormat.Yen(_row.Amount)} までです。";
            return null;
        }

        if (ShowsCategories && SelectedCategory is null)
        {
            ErrorText = "なにに使ったかを選んでください。";
            return null;
        }

        if (ShowsDate && (Date < MinimumDate || Date > MaximumDate))
        {
            ErrorText = "日付は2020年1月1日から2100年1月1日までにしてください。";
            return null;
        }

        ErrorText = string.Empty;
        return new EntryDraft
        {
            Date = ShowsDate ? Date.Date : Date,
            Name = Name.Trim(),
            Note = Note.Trim(),
            FromWhere = FromWhere.Trim(),
            Amount = amount,
            Category = ShowsCategories ? SelectedCategory : _row?.Category,
        };
    }

    [RelayCommand]
    Task SaveAsync() => SaveCoreAsync(keepOpen: false);

    [RelayCommand]
    Task SaveAndContinueAsync() => SaveCoreAsync(keepOpen: true);

    async Task SaveCoreAsync(bool keepOpen)
    {
        if (IsBusy) return;
        var draft = Validate();
        if (draft is null)
        {
            SemanticScreenReader.Default.Announce(ErrorText);
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
            ErrorText = "保存できませんでした。時間をおいて、もう一度ためしてください。";
            SemanticScreenReader.Default.Announce(ErrorText);
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
        SavedText = $"「{title}」{MoneyFormat.Yen(draft.Amount)} を登録しました";
        SemanticScreenReader.Default.Announce(SavedText);
        AmountText = string.Empty;
        Name = string.Empty;
        Note = string.Empty;
        FromWhere = string.Empty;
        FocusAmount?.Invoke();
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
