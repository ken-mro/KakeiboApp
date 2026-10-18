namespace KakeiboApp.Services;

/// <summary>
/// One stop of the guide: the tab to open, the part of that screen to highlight, and what to say about it.
/// When the part is not on screen (an empty list), <see cref="FallbackTarget"/> and <see cref="FallbackBody"/> are used.
/// A highlight normally stops above the bubble; <see cref="HighlightsWhole"/> outlines the whole part even where the
/// bubble covers it, for a part that is explained as a whole (the 生活費 card with all its kinds).
/// </summary>
public record TourStep(string Route, string Target, string Title, string Body, string? FallbackTarget = null, string? FallbackBody = null,
    bool HighlightsWhole = false);

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
            "はじめに、収入・貯金・固定費を入れます。行をタップすると一覧がひらき、右下の「＋」から入れられます。収入から貯金と固定費を引いた金額が、今月使えるお金です。"),
        new(HomeRoute, "add", "生活費をつける",
            "毎日の出費はここからつけます。下の「生活費」のタブからもつけられます。金額を入れて種類を選び、「登録」するだけです。"),
        // The amounts and the bars only: with the bubble above the add button, the whole card does not fit above it.
        new(HomeRoute, "summary", "残りのお金と今月のペース",
            "入れた金額から、今月あといくら使えるか（残りのお金）と、1日あたりに使える金額が出ます。下の2本のバーは今月のペースです。「支出」のバーが「日数」より先に進んでいたら、使いすぎのサインです。"),
        new(SpecialExpenseRoute, "add", "特別出費をつける",
            "旅行や冠婚葬祭など、ふだんとは別の大きな出費は、ここからつけます。特別出費は別枠で、残りのお金には入りません。"),
        new(SettingsRoute, "guide", "もっと知りたいときは",
            "この案内は「使い方」から、もう一度見られます。予算の決め方や、貯金の切り崩し、繰り越しは、その下の「くわしい使い方」で説明しています。"),
    ];

    /// <summary>Nothing here repeats <see cref="Quick"/>.</summary>
    public static readonly IReadOnlyList<TourStep> Detailed =
    [
        new(HomeRoute, "spending", "予算を決める",
            "生活費の種類ごとに、予算を決められます。予算を超えた種類には「オーバー」と表示されます。",
            HighlightsWhole: true),
        new(HomeRoute, "savings", "貯金の内訳・切り崩し",
            "先月までの貯金を目的ごとに見られます。貯金をタップすると、今月切り崩す金額を入れられます。"),
        new(HomeRoute, "hero", "切り崩したとき",
            "切り崩したお金は、その月の残りのお金に足されます。残りのお金の下に「うち切り崩し」として金額が表示されます。"),
        new(SettingsRoute, "carry", "繰り越し",
            "オンにすると、先月までに使い切らなかったお金を、今月使えるお金に加えます。"),
    ];
}

/// <summary>
/// Where the tour draws. The bubble stays in one place for a whole tour: just above the floating add button
/// when one of its pages explains that button, otherwise lower, just above the tab bar. The tab bar is left
/// undimmed so the selected tab shows which screen is explained.
/// Pages scroll the highlighted part to the top so it stays clear of the bubble.
/// </summary>
public static class TourLayout
{
    public const double Margin = 16;
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
    /// The target plus padding, kept on screen. A part that starts above the bubble but is taller than the room
    /// left is cut off just above the bubble, so the bubble never covers the highlight, unless <paramref name="whole"/>
    /// asks for the whole part (it then runs behind the bubble, as far as the dimmed area goes).
    /// </summary>
    public static Rect Highlight(Rect target, Size screen, double bubbleTop, bool whole = false)
    {
        // Keep the 3px outline inside the overlay.
        var top = Math.Max(OutlineInset, target.Y - HighlightPadding);
        var bottom = target.Bottom + HighlightPadding;
        if (whole) bottom = Math.Min(bottom, Dimmed(screen).Bottom - OutlineInset);
        else if (top < bubbleTop) bottom = Math.Min(bottom, bubbleTop - Gap);
        var left = Math.Max(OutlineInset, target.X - HighlightPadding);
        var right = Math.Min(screen.Width - OutlineInset, target.Right + HighlightPadding);
        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    /// <summary>The dimmed area: everything but the tab bar strip.</summary>
    public static Rect Dimmed(Size screen) => new(0, 0, screen.Width, Math.Max(0, screen.Height - DockHeight));
}
