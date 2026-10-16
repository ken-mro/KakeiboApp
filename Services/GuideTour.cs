namespace KakeiboApp.Services;

/// <summary>
/// One stop of the guide: the tab to open, the part of that screen to highlight, and what to say about it.
/// When the part is not on screen (an empty list), <see cref="FallbackTarget"/> and <see cref="FallbackBody"/> are used.
/// </summary>
public record TourStep(string Route, string Target, string Title, string Body, string? FallbackTarget = null, string? FallbackBody = null);

/// <summary>One part of くわしい使い方: a short tour on one subject, opened from its own row in 設定.</summary>
public record GuideTopic(string Name, string Caption, IReadOnlyList<TourStep> Steps);

/// <summary>
/// The guide as tours of the real screens. <see cref="Quick"/> (使い方) is what a new user needs to start and is
/// shown on the first launch and after an update. <see cref="Topics"/> (くわしい使い方) explain only what Quick does
/// not, split into short tours by subject. All of them can be opened from 設定.
/// </summary>
public static class GuideTour
{
    public const string HomeRoute = "MainPage";
    public const string ListRoute = "SpendingListPage";
    public const string SpecialExpenseRoute = "SpecialExpenseListPage";
    public const string SettingsRoute = "SettingPage";

    /// <summary>
    /// In the order a new user needs it: what 残りのお金 is, the 今月使えるお金 it comes from, then adding spending
    /// and special expenses. Budgets and the rest are left to <see cref="Topics"/>.
    /// </summary>
    public static readonly IReadOnlyList<TourStep> Quick =
    [
        new(HomeRoute, "hero", "残りのお金",
            "今月あといくら使えるかです。今月使えるお金から生活費を引いて、1日あたりの金額も出します。"),
        new(HomeRoute, "usable", "まず、今月使えるお金を入れる",
            "はじめに、収入・貯金・固定費を入れます。行をタップすると一覧がひらき、右下の「＋」から入れられます。収入から貯金と固定費を引いた金額が、今月使えるお金です。"),
        new(HomeRoute, "add", "生活費をつける",
            "毎日の出費はここからつけます。下の「生活費」のタブからもつけられます。金額を入れて種類を選び、「登録」するだけです。つけるたびに、残りのお金がへります。"),
        new(SpecialExpenseRoute, "add", "特別出費をつける",
            "旅行や冠婚葬祭など、ふだんとは別の大きな出費は、ここからつけます。特別出費は別枠で、残りのお金には入りません。"),
        new(SettingsRoute, "guide", "もっと知りたいときは",
            "この案内は「使い方」から、もう一度見られます。予算、ペースの見方、貯金の切り崩し、検索などは、その下の「くわしい使い方」で説明しています。"),
    ];

    /// <summary>Nothing here repeats <see cref="Quick"/>; each topic is a few pages on one subject.</summary>
    public static readonly IReadOnlyList<GuideTopic> Topics =
    [
        new("予算とペース", "種類ごとの予算と、使いすぎのサイン",
        [
            new(HomeRoute, "spending", "予算をつける",
                "種類ごとの出費と予算です。種類をタップすると、その月の予算を入れるシートがひらきます。予算を超えた種類には「オーバー」と表示されます。"),
            new(HomeRoute, "pace", "今月のペース",
                "「日数」は月の何日目か、「支出」は使えるお金のうちどれだけ使ったかです。支出のバーが日数より先に進んでいたら、使いすぎのサインです。"),
        ]),
        new("貯金・切り崩し・繰り越し", "先月までの貯金を使うときと、残ったお金の扱い",
        [
            new(HomeRoute, "savings", "貯金の内訳・切り崩し",
                "先月までの貯金を目的ごとに見られます。貯金をタップすると、今月切り崩す金額を入れられます。"),
            new(HomeRoute, "hero", "切り崩したとき",
                "切り崩したお金は、その月の残りのお金に足されます。残りのお金の下に「うち切り崩し」として金額が表示されます。"),
            new(SettingsRoute, "carry", "繰り越し",
                "オンにすると、先月までに使い切らなかったお金を、今月使えるお金に加えます。"),
        ]),
        new("つけ方とさがし方", "固定費のコピー、日付、つづけて登録、検索",
        [
            new(HomeRoute, "usable", "固定費をコピーする",
                "固定費は毎月ほぼ同じです。固定費の一覧の「先月の固定費をコピーする」で、前の月の分をまとめて入れられます。"),
            new(HomeRoute, "add", "日付と、つづけて登録",
                "日付を選ばなければ、今日の日付になります。「メモや日付も書く」から日付を変えられます。「つづけて登録」なら、シートをひらいたまま次の出費を入れられます。"),
            new(SpecialExpenseRoute, "add", "どこから払ったか",
                "特別出費には、どこから払ったか（例：旅行の貯金）も書いておけます。このタブは年ごとの一覧で、上に年間の合計が出ます。"),
            new(ListRoute, "search", "さがす",
                "すべての月の生活費から、内容・メモ・種類・金額でさがせます。特別出費のタブでも同じようにさがせます。"),
        ]),
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
    /// left is cut off just above the bubble, so the bubble never covers the highlight.
    /// </summary>
    public static Rect Highlight(Rect target, Size screen, double bubbleTop)
    {
        // Keep the 3px outline inside the overlay.
        var top = Math.Max(OutlineInset, target.Y - HighlightPadding);
        var bottom = target.Bottom + HighlightPadding;
        if (top < bubbleTop) bottom = Math.Min(bottom, bubbleTop - Gap);
        var left = Math.Max(OutlineInset, target.X - HighlightPadding);
        var right = Math.Min(screen.Width - OutlineInset, target.Right + HighlightPadding);
        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    /// <summary>The dimmed area: everything but the tab bar strip.</summary>
    public static Rect Dimmed(Size screen) => new(0, 0, screen.Width, Math.Max(0, screen.Height - DockHeight));
}
