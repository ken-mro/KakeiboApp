using System.Text;
using KakeiboApp.ViewModels.Entries;

namespace KakeiboApp.Services;

/// <summary>
/// 生活費・特別出費の検索。言葉をスペースで区切ると、すべてを含む行だけが当てはまる。
/// 内容・メモ・種類・どこから払ったかは部分一致で、大文字と小文字、全角と半角、ひらがなとカタカナを区別しない。
/// 金額として読める言葉（「980」「¥1,200」）は、金額がちょうど同じ行にも当てはまる。
/// </summary>
public static class EntrySearch
{
    /// <summary>The period chips of the search, in their order on screen.</summary>
    public enum Period { All, ThisMonth, LastMonth, ThisYear, Custom }

    public static string Label(this Period period) => period switch
    {
        Period.All => "すべて",
        Period.ThisMonth => "今月",
        Period.LastMonth => "先月",
        Period.ThisYear => "今年",
        _ => "期間を指定",
    };

    /// <summary>
    /// The first and last day of <paramref name="period"/> as of <paramref name="today"/>; for <see cref="Period.Custom"/>,
    /// <paramref name="from"/> and <paramref name="to"/> in order. Null for <see cref="Period.All"/>.
    /// </summary>
    public static (DateTime From, DateTime To)? Range(Period period, DateTime today, DateTime from, DateTime to)
    {
        var month = new DateTime(today.Year, today.Month, 1);
        return period switch
        {
            Period.ThisMonth => (month, month.AddMonths(1).AddDays(-1)),
            Period.LastMonth => (month.AddMonths(-1), month.AddDays(-1)),
            Period.ThisYear => (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31)),
            Period.Custom => from.Date <= to.Date ? (from.Date, to.Date) : (to.Date, from.Date),
            _ => null,
        };
    }

    /// <summary>The date is in the range, both ends included; any date is in no range.</summary>
    public static bool InRange(DateTime date, (DateTime From, DateTime To)? range) =>
        range is not { } r || (date.Date >= r.From && date.Date <= r.To);

    public static IReadOnlyList<string> Terms(string? query) =>
        string.IsNullOrWhiteSpace(query) ? [] : query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// 比べる前にそろえる: 全角英数と半角カナは NFKC で、ひらがなはカタカナに、英字は大文字に。
    /// (ICU の照合は長音「ー」を前の母音として扱うため、部分一致に使わない。)
    /// </summary>
    public static string Fold(string text)
    {
        var builder = new StringBuilder(text.Normalize(NormalizationForm.FormKC));
        for (var i = 0; i < builder.Length; i++)
        {
            var c = builder[i];
            if (c is >= 'ぁ' and <= 'ゖ' or 'ゝ' or 'ゞ') c = (char)(c + 0x60);
            builder[i] = char.ToUpperInvariant(c);
        }
        return builder.ToString();
    }

    public static bool Matches(EntryRow row, IReadOnlyList<string> terms) =>
        terms.Count > 0 && terms.All(term => Matches(row, term));

    static bool Matches(EntryRow row, string term) =>
        Contains(row.Name, term)
        || Contains(row.Note, term)
        || Contains(row.Category, term)
        || Contains(row.FromWhere, term)
        || (MoneyFormat.TryParseAmount(term, out var amount) && row.Amount == amount);

    static bool Contains(string? text, string term) =>
        !string.IsNullOrEmpty(text) && Fold(text).Contains(Fold(term), StringComparison.Ordinal);
}
