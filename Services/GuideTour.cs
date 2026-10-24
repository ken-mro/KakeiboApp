namespace KakeiboApp.Services;

/// <summary>
/// One stop of the guide: the tab to open, the part of that screen to highlight, and what to say about it.
/// When the part is not on screen (an empty list), <see cref="FallbackTarget"/> and <see cref="FallbackBody"/> are used.
/// </summary>
public record TourStep(string Route, string Target, string Title, string Body, string? FallbackTarget = null, string? FallbackBody = null);

/// <summary>
/// The guide as tours of the real screens. <see cref="Quick"/> (使い方) is what a new user needs to start and is
/// shown on the first launch and after an update. <see cref="Detailed"/> (くわしい使い方) explains only what Quick
/// does not. Both can be opened from 設定.
/// </summary>
public static class GuideTour
{
    public const string HomeRoute = "MainPage";
    public const string ListRoute = "SpendingListPage";
    public const string SpecialExpenseRoute = "SpecialExpenseListPage";
    public const string SettingsRoute = "SettingPage";

    /// <summary>A step about the whole screen: nothing is highlighted, and the home is shown from the top.</summary>
    public const string OverviewTarget = "overview";

    /// <summary>
    /// What the app is for, then entering 今月使えるお金 and 生活費 and what the home shows once they are in
    /// (残りのお金 and the pace), then 特別出費, which stays outside that. Budgets and the rest are left to <see cref="Detailed"/>.
    /// </summary>
    public static readonly IReadOnlyList<TourStep> Quick =
    [
        new(HomeRoute, OverviewTarget, "このアプリでできること",
            "毎月の収入から貯金と固定費を引いた「今月使えるお金」の中で、生活費をやりくりするための家計簿です。まずは、今月のお金を入れていきましょう。"),
        new(HomeRoute, "usable", "今月使えるお金を入れる",
            "はじめに、収入・貯金・固定費を入れます。どれも次の「＋ つける」から入れられます。「今月使えるお金」をタップすると内訳が表示され、行をタップすると一覧がひらきます。先月の分は、一覧からコピーもできます。収入から貯金と固定費を引いた金額が、今月使えるお金です。"),
        new(HomeRoute, "add", "生活費をつける",
            "「＋ つける」をタップすると、収入・貯金・固定費・特別出費・生活費の付箋が出ます。毎日の出費は「生活費」を選びます。ボタンから指を上にすべらせて、選びたい付箋で離しても選べます。どのタブからもつけられます。"),
        // The amounts and the bars only, which is what the text is about: the whole card would run behind the bubble.
        new(HomeRoute, "summary", "残りのお金と今月のペース",
            "今月あといくら使えるか（残りのお金）と、1日あたりに使える金額が出ます。下の2本のバーは今月のペースです。「支出」が「日数」より先なら、使いすぎのサインです。"),
        new(SpecialExpenseRoute, "add", "特別出費をつける",
            "旅行や冠婚葬祭など、ふだんとは別の大きな出費は、「＋ つける」の「特別出費」からつけます。特別出費は別枠で、残りのお金には入りません。"),
        new(SettingsRoute, "guide", "もっと知りたいときは",
            "この案内は「使い方」から、もう一度見られます。予算の決め方や、貯金の切り崩し、繰り越しは、その下の「くわしい使い方」で説明しています。"),
    ];

    /// <summary>Nothing here repeats <see cref="Quick"/>.</summary>
    public static readonly IReadOnlyList<TourStep> Detailed =
    [
        new(HomeRoute, "spending", "予算を決める",
            "生活費の種類ごとに、予算を決められます。「生活費」をタップすると種類ごとの金額が表示され、「予算を決める」で、すべての種類の予算をまとめて入れられます。予算を超えた種類には「オーバー」と表示されます。"),
        new(HomeRoute, "savings", "貯金の内訳・切り崩し",
            "先月までの貯金を目的ごとに見られます。貯金をタップすると、今月切り崩す金額を入れられます。"),
        // The amounts and the bars, as on 使い方's page: the text is about うち切り崩し.
        new(HomeRoute, "summary", "切り崩したとき",
            "切り崩したお金は、その月の残りのお金に足されます。残りのお金の下に「うち切り崩し」として金額が表示されます。"),
        new(SettingsRoute, "carry", "繰り越し",
            "オンにすると、先月までに使い切らなかったお金を、今月使えるお金に加えます。"),
        new(SettingsRoute, "side", "「＋ つける」の位置",
            "「＋ つける」を画面の右と左のどちらに置くかを選べます。"),
    ];
}

/// <summary>
/// Where the tour draws. The bubble stays in one place for a whole tour: just above the floating add button
/// when one of its pages explains that button, otherwise lower, just above the tab bar. The tab bar is left
/// undimmed so the selected tab shows which screen is explained.
/// Pages scroll the highlighted part toward the top, as far as the page itself scrolls, so it is shown where it
/// is seen without the tour. A part that is still in the bubble's way (one low on the page, or too tall for the
/// room above the bubble) has the bubble at the top of the screen on that page, and is moved down only if it
/// would be under it (<see cref="MovesBubbleUp"/>).
/// </summary>
public static class TourLayout
{
    public const double Margin = 16;
    /// <summary>Room between the bubble and an outline next to it.</summary>
    public const double Gap = 12;
    public const double HighlightPadding = 6;
    public const double OutlineInset = 2;

    /// <summary>The strip at the bottom with the floating tab bar (76 high, 16 from the bottom), left undimmed.</summary>
    public const double DockHeight = 100;

    /// <summary>The bubble's bottom edge, measured from the bottom: just above the tab bar strip.</summary>
    public const double BubbleBottomInset = DockHeight + 4;

    /// <summary>The same, in a tour that explains the add button: above it (56 high, 112 from the bottom).</summary>
    public const double AboveAddButtonInset = 180;

    public static double BubbleTop(Size screen, double bubbleHeight, bool aboveAddButton = false) =>
        Math.Max(Margin, screen.Height - (aboveAddButton ? AboveAddButtonInset : BubbleBottomInset) - bubbleHeight);

    /// <summary>The floating add button's target name on every page.</summary>
    public const string AddButtonTarget = "add";

    public static bool ExplainsAddButton(IEnumerable<TourStep> steps) =>
        steps.Any(s => s.Target == AddButtonTarget || s.FallbackTarget == AddButtonTarget);

    /// <summary>
    /// The target plus padding, kept on screen. A part that still reaches the bubble (too tall even below a bubble
    /// at the top) is outlined in full, behind the bubble, as far as the dimmed area goes: cut off at the bubble,
    /// the outline would end in the middle of the part and look misplaced.
    /// </summary>
    public static Rect Highlight(Rect target, Size screen)
    {
        // Keep the 3px outline inside the overlay.
        var top = Math.Max(OutlineInset, target.Y - HighlightPadding);
        var bottom = Math.Min(target.Bottom + HighlightPadding, Dimmed(screen).Bottom - OutlineInset);
        var left = Math.Max(OutlineInset, target.X - HighlightPadding);
        var right = Math.Min(screen.Width - OutlineInset, target.Right + HighlightPadding);
        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    /// <summary>The bubble's top edge on a page where it is moved to the top of the screen.</summary>
    public const double TopBubbleTop = Margin;

    /// <summary>Where a part's top edge goes when the bubble is at the top: just below the bubble and its outline gap.</summary>
    public static double BelowTopBubble(double bubbleHeight) => TopBubbleTop + bubbleHeight + Gap + HighlightPadding;

    /// <summary>
    /// Whether to move the bubble to the top for <paramref name="target"/>: its outline would run into the bubble at
    /// <paramref name="bubbleTop"/> (a part above it, or one behind it low on the page), and the whole part fits
    /// between a bubble at the top and the tab bar strip. A part clear of the bubble (above it, or below it like the
    /// add button) keeps the bubble where it is, and one too tall even for that is outlined behind the bubble as before.
    /// </summary>
    public static bool MovesBubbleUp(Rect target, Size screen, double bubbleTop, double bubbleHeight)
    {
        if (target.Width <= 0 || target.Height <= 0) return false;
        var outline = Highlight(target, screen);
        if (outline.Bottom <= bubbleTop - Gap || outline.Top >= bubbleTop + bubbleHeight) return false;
        return BelowTopBubble(bubbleHeight) + target.Height + HighlightPadding <= Dimmed(screen).Bottom - OutlineInset;
    }

    /// <summary>The dimmed area: everything but the tab bar strip.</summary>
    public static Rect Dimmed(Size screen) => new(0, 0, screen.Width, Math.Max(0, screen.Height - DockHeight));
}
