using KakeiboApp.Models;

namespace KakeiboApp.Services;

/// <summary>
/// ホームの吹き出しに出す、ぶたからのひとこと。
/// </summary>
public static class PaceAdvice
{
    public static string For(MonthSummary summary, MonthPace pace)
    {
        var daily = MoneyFormat.Yen(pace.DailyAllowance);

        if (pace.ElapsedDays == 0)
        {
            return summary.Remaining > 0
                ? $"まだ始まっていない月です。\n1日 {daily} までなら、月末まで足ります。"
                : "まだ始まっていない月です。\n収入や予算を先に入れておけます。";
        }

        if (pace.RemainingDays == 0)
        {
            return summary.Remaining >= 0
                ? $"この月は {MoneyFormat.Yen(summary.Remaining)} 残りました。"
                : $"この月は {MoneyFormat.Yen(-summary.Remaining)} 足りませんでした。";
        }

        if (summary.Remaining < 0)
        {
            return $"今月は {MoneyFormat.Yen(-summary.Remaining)} オーバーしています。\n切り崩しや予算を見直してみましょう。";
        }

        return pace.IsAhead
            ? $"ちょっと早めのペースです。\n1日 {daily} までにすると、月末までちょうど足ります。"
            : $"いいペースです。\n1日 {daily} までなら、月末まで足ります。";
    }
}
