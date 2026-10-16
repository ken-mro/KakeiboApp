using KakeiboApp.Services;

namespace KakeiboApp.ViewModels.Entries;

/// <summary>
/// The kinds of data the shared list and sheet edit.
/// </summary>
public enum EntryKind
{
    Income,
    Saving,
    FixedCost,
    Spending,
    SpecialExpense,
    /// <summary>先月までの貯金の内訳。編集するのはその月の切り崩し。</summary>
    SavingBalance,
    /// <summary>種類ごとの予算。</summary>
    Budget,
}

public static class EntryKindText
{
    public static string Name(this EntryKind kind) => kind switch
    {
        EntryKind.Income => "収入",
        EntryKind.Saving => "貯金",
        EntryKind.FixedCost => "固定費",
        EntryKind.Spending => "生活費",
        EntryKind.SpecialExpense => "特別出費",
        EntryKind.SavingBalance => "貯金の内訳",
        EntryKind.Budget => "予算",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>The add button and the add sheet's title.</summary>
    public static string AddTitle(this EntryKind kind) => kind switch
    {
        EntryKind.Spending => "生活費をつける",
        EntryKind.SpecialExpense => "特別出費をつける",
        _ => $"{kind.Name()}を入れる",
    };

    public static string EditTitle(this EntryKind kind) => $"{kind.Name()}をなおす";

    public static string ListTitle(this EntryKind kind, DateTime month) => kind switch
    {
        EntryKind.SpecialExpense => $"{month.Year}年の特別出費",
        EntryKind.SavingBalance => "貯金の内訳・切り崩し",
        _ => $"{month.Month}月の{kind.Name()}",
    };

    /// <summary>Dated kinds are grouped by day and their sheet has a date field.</summary>
    public static bool IsDated(this EntryKind kind) => kind is EntryKind.Spending or EntryKind.SpecialExpense;

    public static bool CanAdd(this EntryKind kind) => kind is not (EntryKind.SavingBalance or EntryKind.Budget);

    public static string Glyph(this EntryKind kind) => kind switch
    {
        EntryKind.Income => Icons.Income,
        EntryKind.Saving or EntryKind.SavingBalance => Icons.Coin,
        EntryKind.FixedCost => Icons.House,
        EntryKind.SpecialExpense => Icons.Gift,
        _ => Icons.List,
    };

    public static string EmptyText(this EntryKind kind, DateTime month) => kind switch
    {
        EntryKind.Income => $"{month.Month}月の収入はまだありません。\n給料などを入れると、今月使えるお金がわかります。",
        EntryKind.Saving => $"{month.Month}月の貯金はまだありません。\n貯めたお金を目的ごとに入れておけます。",
        EntryKind.FixedCost => $"{month.Month}月の固定費はまだありません。\n家賃や光熱費など、毎月決まって出ていくお金です。",
        EntryKind.Spending => $"{month.Month}月の生活費はまだありません。\nホームの「生活費をつける」からも入れられます。",
        EntryKind.SpecialExpense => $"{month.Year}年の特別出費はまだありません。\n旅行や冠婚葬祭など、生活費とは別に管理したい出費です。",
        EntryKind.SavingBalance => "先月までの貯金はまだありません。\n貯金を入れると、ここから切り崩せます。",
        _ => string.Empty,
    };
}
