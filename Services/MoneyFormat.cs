using System.Globalization;

namespace KakeiboApp.Services;

/// <summary>
/// 金額の表示と入力。表示は円で「¥90,000」、マイナスは「−¥5,000」(U+2212)。
/// </summary>
public static class MoneyFormat
{
    public const string Minus = "−";

    public static string Yen(decimal amount)
    {
        var rounded = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        var digits = Math.Abs(rounded).ToString("#,0", CultureInfo.InvariantCulture);
        return rounded < 0 ? $"{Minus}¥{digits}" : $"¥{digits}";
    }

    /// <summary>差し引く金額(貯金・固定費)。0 のときは記号を付けない。</summary>
    public static string Deduction(decimal amount) => amount == 0 ? Yen(0) : Minus + Yen(amount);

    /// <summary>入力欄に入れる文字列。0 は空欄。</summary>
    public static string ForInput(decimal amount) =>
        amount == 0 ? string.Empty : amount.ToString("#,0", CultureInfo.InvariantCulture);

    /// <summary>
    /// 入力された金額を読み取る。全角数字・カンマ・「¥」「円」を受け付ける。1 以上の整数だけを正しい金額とする。
    /// </summary>
    public static bool TryParseAmount(string? text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var normalized = text.Normalize(System.Text.NormalizationForm.FormKC)
            .Replace(",", string.Empty)
            .Replace("¥", string.Empty)
            .Replace("\\", string.Empty)
            .Replace("円", string.Empty)
            .Trim();

        if (normalized.Length == 0 || normalized.Length > 12 || !normalized.All(char.IsAsciiDigit)) return false;

        amount = decimal.Parse(normalized, CultureInfo.InvariantCulture);
        return amount >= 1;
    }
}
