namespace KakeiboApp.Services;

/// <summary>
/// 種類ごとの色とアイコン。色は Resources/Styles/Colors.xaml の値と同じ(ライト・ダーク共通)。
/// </summary>
public record CategoryStyle(string Glyph, Color Chip, Color Icon, Color Bar)
{
    static readonly Dictionary<string, CategoryStyle> Styles = new()
    {
        ["食費"] = new(Icons.Food, Color.FromArgb("#FFE3EA"), Color.FromArgb("#B23A62"), Color.FromArgb("#F28AA8")),
        ["日用品・雑貨"] = new(Icons.Daily, Color.FromArgb("#DDF3EA"), Color.FromArgb("#2E7A5E"), Color.FromArgb("#6CC3A0")),
        ["娯楽"] = new(Icons.Fun, Color.FromArgb("#FFF0C2"), Color.FromArgb("#8A6A00"), Color.FromArgb("#EDBE45")),
        ["車"] = new(Icons.Car, Color.FromArgb("#DDEBFF"), Color.FromArgb("#2F62A8"), Color.FromArgb("#78A8EC")),
        ["医療"] = new(Icons.Medical, Color.FromArgb("#ECE4FF"), Color.FromArgb("#6247B0"), Color.FromArgb("#A98DE6")),
        ["その他"] = new(Icons.Other, Color.FromArgb("#F0EAE3"), Color.FromArgb("#7A6450"), Color.FromArgb("#BFA88F")),
    };

    /// <summary>Unknown categories (none exist today) use the その他 style.</summary>
    public static CategoryStyle For(string? category) =>
        category is not null && Styles.TryGetValue(category, out var style) ? style : Styles["その他"];

    /// <summary>特別出費 uses the 娯楽 (yellow) chip, 貯金 the 食費 (pink) chip, as in the design.</summary>
    public static CategoryStyle SpecialExpense => Styles["娯楽"] with { Glyph = Icons.Gift };
    public static CategoryStyle SavingBalance => Styles["食費"] with { Glyph = Icons.Coin };
}
