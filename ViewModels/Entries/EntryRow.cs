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

    public string Subtitle => Kind switch
    {
        EntryKind.Spending => Join(Category, Note),
        EntryKind.SpecialExpense => Join(string.IsNullOrWhiteSpace(FromWhere) ? null : $"{FromWhere}から", Note),
        EntryKind.SavingBalance => Secondary > 0
            ? $"貯金 {MoneyFormat.Yen(Amount)} ・ 今月の切り崩し {MoneyFormat.Yen(Secondary)}"
            : $"貯金 {MoneyFormat.Yen(Amount)}",
        _ => Note,
    };

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

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

    public string Description => HasSubtitle ? $"{Title}、{Subtitle}、{AmountText}" : $"{Title}、{AmountText}";

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

    static string Join(params string?[] parts) =>
        string.Join(" ・ ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
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
