namespace KakeiboApp.Services;

/// <summary>
/// Where a title like 「2026年の特別出費」 may break when it does not fit on one line: right after the first 「の」.
/// The page shows the two parts in a wrapping layout, so a long title becomes 「2026年の」/「特別出費」.
/// </summary>
public static class TitleBreak
{
    public static (string Lead, string Tail) Split(string? title)
    {
        if (string.IsNullOrEmpty(title)) return (string.Empty, string.Empty);
        var at = title.IndexOf('の');
        return at < 0 || at == title.Length - 1 ? (title, string.Empty) : (title[..(at + 1)], title[(at + 1)..]);
    }
}
