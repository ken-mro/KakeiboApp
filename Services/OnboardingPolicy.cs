namespace KakeiboApp.Services;

/// <summary>
/// 「使い方」を自動で表示するかどうか。最後に使い方を見たアプリのバージョンと、今のバージョンを比べる。
/// </summary>
public static class OnboardingPolicy
{
    /// <param name="lastSeenVersion">最後に使い方を見たバージョン。一度も見ていなければ null か空。</param>
    /// <param name="currentVersion">今のアプリのバージョン(ApplicationDisplayVersion)。</param>
    public static bool ShouldShow(string? lastSeenVersion, string currentVersion)
    {
        // 1.x にはこの値がないため、既存ユーザーのアップデートも初回として表示する。
        if (string.IsNullOrWhiteSpace(lastSeenVersion)) return true;

        if (!TryParse(lastSeenVersion, out var lastSeen)) return true;
        if (!TryParse(currentVersion, out var current)) return false;

        // バージョンが下がった場合は、新しいほうの使い方を見ているので表示しない。
        return current > lastSeen;
    }

    // "2", "2.0" and "2.0.0" are the same version. Version.Parse alone treats missing parts as -1.
    static bool TryParse(string text, out Version version)
    {
        var trimmed = text.Trim();
        if (!trimmed.Contains('.')) trimmed += ".0";

        if (!Version.TryParse(trimmed, out var parsed))
        {
            version = new Version();
            return false;
        }

        version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0));
        return true;
    }
}
