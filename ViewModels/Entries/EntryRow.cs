using KakeiboApp.Services;

namespace KakeiboApp.ViewModels.Entries;

/// <summary>
/// One row of the shared list, whatever the kind. <see cref="Source"/> is the model it was made from.
/// </summary>
public class EntryRow
{
    public required EntryKind Kind { get; init; }
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Category { get; init; }
    public string FromWhere { get; init; } = string.Empty;
    /// <summary>SavingBalance: this month's withdrawal (positive). Budget: the month's spending.</summary>
    public decimal Secondary { get; init; }
    public required object Source { get; init; }

    public string Title => Kind switch
    {
        EntryKind.Budget => Category ?? string.Empty,
        _ when string.IsNullOrWhiteSpace(Name) => Category ?? Kind.Name(),
        _ => Name,
    };

    /// <summary>The line under the title: the category or where it was paid from. The memo has its own lines (<see cref="Memo"/>).</summary>
    public string Subtitle => Kind switch
    {
        // An entry without a name already shows its category as the title.
        EntryKind.Spending => string.IsNullOrWhiteSpace(Name) ? string.Empty : Category ?? string.Empty,
        EntryKind.SpecialExpense => string.IsNullOrWhiteSpace(FromWhere) ? string.Empty : $"{FromWhere}から",
        EntryKind.SavingBalance => Secondary > 0
            ? $"貯金 {MoneyFormat.Yen(Amount)} ・ 今月の切り崩し {MoneyFormat.Yen(Secondary)}"
            : $"貯金 {MoneyFormat.Yen(Amount)}",
        _ => string.Empty,
    };

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    /// <summary>The memo as it was typed, line breaks and all.</summary>
    public string Memo => Kind is EntryKind.SavingBalance or EntryKind.Budget ? string.Empty : CleanNote(Note);

    public bool HasMemo => Memo.Length > 0;

    /// <summary>
    /// A memo as it is saved and shown: Windows line breaks made "\n", blank lines at its start and end and spaces
    /// at the end of each line dropped. Line breaks, blank lines and indents inside it are kept, so it reads as it
    /// was typed.
    /// </summary>
    public static string CleanNote(string? note)
    {
        if (string.IsNullOrEmpty(note)) return string.Empty;
        var lines = note.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Select(l => l.TrimEnd()).ToList();
        while (lines.Count > 0 && lines[0].Length == 0) lines.RemoveAt(0);
        while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return string.Join("\n", lines);
    }

    /// <summary>The amount on the right: the balance for SavingBalance, otherwise the amount.</summary>
    public decimal DisplayAmount => Kind == EntryKind.SavingBalance ? Amount - Secondary : Amount;

    public string AmountText => MoneyFormat.Yen(DisplayAmount);

    public CategoryStyle Style => Kind switch
    {
        EntryKind.Spending or EntryKind.Budget => CategoryStyle.For(Category),
        EntryKind.SpecialExpense => CategoryStyle.SpecialExpense,
        EntryKind.SavingBalance => CategoryStyle.SavingBalance,
        // 収入・貯金・固定費 use the accent chip, as on the home page.
        _ => new CategoryStyle(Kind.Glyph(), ThemeTokens.Get("AccentSoft"), ThemeTokens.Get("AccentInk"), ThemeTokens.Get("Accent")),
    };

    /// <summary>A withdrawal row can only be deleted when there is a withdrawal.</summary>
    public bool CanDelete => Kind switch
    {
        EntryKind.SavingBalance => Secondary > 0,
        EntryKind.Budget => Id != 0,
        _ => true,
    };

    public string DeleteLabel => Kind == EntryKind.SavingBalance ? $"{Title}の切り崩しをけす" : $"{Title}をけす";

    public string Description => string.Join("、", new[] { Title, Subtitle, AmountText, HasMemo ? $"メモ {Memo.Replace('\n', ' ')}" : null }
        .Where(p => !string.IsNullOrWhiteSpace(p)));

    public string DeletedMessage => Kind == EntryKind.SavingBalance
        ? $"「{Title}」の切り崩しをけしました"
        : $"「{Title}」をけしました";

    /// <summary>Rows of a day share one rounded card: only the first and last rows round their corners.</summary>
    public bool IsFirstInGroup { get; set; }
    public bool IsLastInGroup { get; set; }

    public Microsoft.Maui.Controls.Shapes.RoundRectangle RowShape => new()
    {
        CornerRadius = new CornerRadius(IsFirstInGroup ? 24 : 0, IsFirstInGroup ? 24 : 0, IsLastInGroup ? 24 : 0, IsLastInGroup ? 24 : 0),
    };

    public bool ShowsDivider => !IsLastInGroup;
}

/// <summary>A chip of the search's period (すべて, 今月, …); the selected one is filled.</summary>
public partial class PeriodChip(Services.EntrySearch.Period period) : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public Services.EntrySearch.Period Period { get; } = period;
    public string Label { get; } = Services.EntrySearch.Label(period);

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(Description))]
    bool _isSelected = period == Services.EntrySearch.Period.All;

    public string Description => IsSelected ? $"{Label}、選択中" : Label;
}

/// <summary>
/// A day (or, for undated kinds, the whole month) of rows.
/// </summary>
public class EntryGroup : List<EntryRow>
{
    public EntryGroup(string header, IEnumerable<EntryRow> rows) : base(rows)
    {
        Header = header;
        for (var i = 0; i < Count; i++)
        {
            this[i].IsFirstInGroup = i == 0;
            this[i].IsLastInGroup = i == Count - 1;
        }
    }

    public string Header { get; }
    public bool HasHeader => !string.IsNullOrEmpty(Header);

    // Undated kinds have one group without a header; give it no height so it leaves no gap.
    public double HeaderHeight => HasHeader ? -1 : 0;
    public Thickness HeaderMargin => HasHeader ? new Thickness(8, 16, 8, 8) : new Thickness(0, 8, 0, 0);
}

/// <summary>
/// What a sheet saves: the fields every kind may use.
/// </summary>
public record EntryDraft
{
    public DateTime Date { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Category { get; init; }
    public string FromWhere { get; init; } = string.Empty;
}
