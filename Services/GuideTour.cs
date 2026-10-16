namespace KakeiboApp.Services;

/// <summary>
/// One stop of the guide: the tab to open, the part of that screen to highlight, and what to say about it.
/// When the part is not on screen (an empty list), <see cref="FallbackTarget"/> and <see cref="FallbackBody"/> are used.
/// </summary>
public record TourStep(string Route, string Target, string Title, string Body, string? FallbackTarget = null, string? FallbackBody = null);

/// <summary>
/// The guide as tours of the real screens. <see cref="Quick"/> (使い方) is what a new user needs to start and is
/// shown on the first launch and after an update; <see cref="Detailed"/> (くわしい使い方) explains every part.
/// Both can be opened again from 設定.
/// </summary>
public static class GuideTour
{
    public const string HomeRoute = "MainPage";
    public const string ListRoute = "SpendingListPage";
    public const string SpecialExpenseRoute = "SpecialExpenseListPage";
    public const string SettingsRoute = "SettingPage";

    // Editing is shown once, on the 生活費 list; the other lists work the same way.
    const string EditBody = "行をタップすると、金額や内容をなおせます。左にスワイプすると「けす」が出て、画面の下の「もとに戻す」で戻せます。特別出費や収入・貯金・固定費の一覧でも同じです。";
    const string EditFallbackBody = "生活費は、ここからもつけられます。つけた行はタップでなおせ、左にスワイプでけせます。特別出費や収入・貯金・固定費の一覧でも同じです。";

    /// <summary>
    /// In the order a new user needs it: what 残りのお金 is, the 今月使えるお金 it comes from, adding spending and
    /// special expenses, then fixing entries. Budgets and the rest are left to <see cref="Detailed"/>.
    /// </summary>
    public static readonly IReadOnlyList<TourStep> Quick =
    [
        new(HomeRoute, "hero", "残りのお金",
            "今月あといくら使えるかです。今月使えるお金から生活費を引いて、1日あたりの金額も出します。"),
        new(HomeRoute, "usable", "まず、今月使えるお金を入れる",
            "はじめに、収入・貯金・固定費を入れます。行をタップすると一覧がひらき、右下の「＋」から入れられます。収入から貯金と固定費を引いた金額が、今月使えるお金です。"),
        new(HomeRoute, "add", "生活費をつける",
            "毎日の出費はここからつけます。金額を入れて種類を選び、「登録」するだけです。つけるたびに、残りのお金がへります。"),
        new(SpecialExpenseRoute, "add", "特別出費をつける",
            "旅行や冠婚葬祭など、ふだんとは別の大きな出費は、ここからつけます。特別出費は別枠で、残りのお金には入りません。"),
        new(ListRoute, "hint", "なおす・けす", EditBody, FallbackTarget: "add", FallbackBody: EditFallbackBody),
        new(SettingsRoute, "guide", "もっと知りたいときは",
            "この案内は「使い方」から、もう一度見られます。予算、ペースの見方、貯金の切り崩し、検索などは「くわしい使い方」で説明しています。"),
    ];

    /// <summary>The same order as <see cref="Quick"/>, with every other part where it appears on the screen.</summary>
    public static readonly IReadOnlyList<TourStep> Detailed =
    [
        new(HomeRoute, "hero", "残りのお金",
            "今月あといくら使えるかです。1日あたりに使える金額と、月末までの日数もわかります。貯金を切り崩した月は、その金額も表示されます。"),
        new(HomeRoute, "pace", "今月のペース",
            "「日数」は月の何日目か、「支出」は使えるお金のうちどれだけ使ったかです。支出のバーが日数より先に進んでいたら、使いすぎのサインです。"),
        new(HomeRoute, "usable", "今月使えるお金",
            "収入 − 貯金 − 固定費 です。まずここを入れると、残りのお金がわかります。行をタップすると一覧がひらき、入れる・なおす・けすができます。固定費の一覧では、先月の固定費をコピーできます。"),
        new(HomeRoute, "add", "生活費をつける",
            "金額を入れて種類を選び、「登録」します。日付を選ばなければ今日の日付になります。「つづけて登録」なら、シートをひらいたまま次の出費を入れられます。"),
        new(HomeRoute, "spending", "予算をつける",
            "種類ごとの出費と予算です。種類をタップすると、その月の予算を入れるシートがひらきます。予算を超えた種類には「オーバー」と表示されます。"),
        new(HomeRoute, "savings", "貯金の内訳・切り崩し",
            "先月までの貯金を目的ごとに見られます。貯金をタップすると、今月切り崩す金額を入れられます。切り崩したお金は、残りのお金に入ります。"),
        new(HomeRoute, "special", "特別出費",
            "旅行や冠婚葬祭など、生活費とは別枠の出費です。残りのお金には入りません。タップすると、特別出費のタブに移ります。"),
        new(SpecialExpenseRoute, "add", "特別出費をつける",
            "特別出費は、このタブの「＋ 特別出費をつける」からつけます。どこから払ったか（例：旅行の貯金）も書いておけます。"),
        new(ListRoute, "hint", "なおす・けす", EditBody, FallbackTarget: "add", FallbackBody: EditFallbackBody),
        new(ListRoute, "search", "さがす",
            "すべての月の生活費から、内容・メモ・種類・金額でさがせます。特別出費のタブでも同じようにさがせます。"),
        new(SettingsRoute, "carry", "繰り越し",
            "オンにすると、先月までに使い切らなかったお金を、今月使えるお金に加えます。"),
        new(SettingsRoute, "detail", "もう一度見るには",
            "この説明は、設定の「くわしい使い方」からいつでも見られます。「使い方」では、はじめに必要なことだけを説明しています。"),
    ];
}

/// <summary>
/// Where the tour draws. The bubble always sits in the same place, just above the floating add button and the
/// tab bar, and the tab bar is left undimmed so the selected tab shows which screen is being explained.
/// Pages scroll the highlighted part to the top so it stays clear of the bubble.
/// </summary>
public static class TourLayout
{
    public const double Margin = 16;
    public const double Gap = 12;
    public const double HighlightPadding = 6;

    /// <summary>The strip at the bottom with the floating tab bar (76 high, 16 from the bottom), left undimmed.</summary>
    public const double DockHeight = 100;

    /// <summary>The bubble's bottom edge, measured from the bottom: above the add button (56 high, 112 from the bottom).</summary>
    public const double BubbleBottomInset = 180;

    public static double BubbleTop(Size screen, double bubbleHeight) =>
        Math.Max(Margin, screen.Height - BubbleBottomInset - bubbleHeight);

    /// <summary>
    /// The target plus padding, kept on screen. A part that starts above the bubble but is taller than the room
    /// left is cut off just above the bubble, so the bubble never covers the highlight.
    /// </summary>
    public static Rect Highlight(Rect target, Size screen, double bubbleTop)
    {
        var top = Math.Max(0, target.Y - HighlightPadding);
        var bottom = target.Bottom + HighlightPadding;
        if (top < bubbleTop) bottom = Math.Min(bottom, bubbleTop - Gap);
        return new Rect(Math.Max(0, target.X - HighlightPadding), top,
            Math.Min(screen.Width, target.Width + HighlightPadding * 2), Math.Max(0, bottom - top));
    }

    /// <summary>The dimmed area: everything but the tab bar strip.</summary>
    public static Rect Dimmed(Size screen) => new(0, 0, screen.Width, Math.Max(0, screen.Height - DockHeight));
}
