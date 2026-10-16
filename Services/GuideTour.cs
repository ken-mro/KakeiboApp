namespace KakeiboApp.Services;

/// <summary>
/// One stop of the guide: the tab to open, the part of that screen to highlight, and what to say about it.
/// When the part is not on screen (an empty list), <see cref="FallbackTarget"/> and <see cref="FallbackBody"/> are used.
/// </summary>
public record TourStep(string Route, string Target, string Title, string Body, string? FallbackTarget = null, string? FallbackBody = null);

/// <summary>
/// The guide (使い方) as a tour of the real screens. Shown on the first launch, after an update, and from 設定 › 使い方.
/// </summary>
public static class GuideTour
{
    public const string HomeRoute = "MainPage";
    public const string ListRoute = "SpendingListPage";
    public const string SpecialExpenseRoute = "SpecialExpenseListPage";
    public const string SettingsRoute = "SettingPage";

    public static readonly IReadOnlyList<TourStep> Steps =
    [
        new(HomeRoute, "hero", "残りのお金",
            "今月あといくら使えるかです。1日あたりに使える金額と、月末までの日数もわかります。"),
        new(HomeRoute, "pace", "今月のペース",
            "「日数」は月の何日目か、「支出」は使えるお金のうちどれだけ使ったかです。支出のバーが日数より先に進んでいたら、使いすぎのサインです。"),
        new(HomeRoute, "usable", "今月使えるお金",
            "収入 − 貯金 − 固定費 です。行をタップすると一覧がひらき、追加・修正・削除ができます。"),
        new(HomeRoute, "spending", "予算をつける",
            "種類ごとの出費と予算です。種類をタップすると、その月の予算を入れるシートがひらきます。予算を超えた種類には「オーバー」と表示されます。"),
        new(HomeRoute, "savings", "貯金の内訳・切り崩し",
            "先月までの貯金を目的ごとに見られます。貯金をタップすると、今月切り崩す金額を入れられます。切り崩したお金は、残りのお金に入ります。"),
        new(HomeRoute, "special", "特別出費",
            "旅行や冠婚葬祭など、生活費とは別枠の出費です。残りのお金には入りません。"),
        new(HomeRoute, "add", "生活費をつける",
            "毎日の出費はここから。金額を入れて種類を選び、「登録」するだけです。日付やメモはなくても大丈夫です。"),
        new(HomeRoute, "tabs", "タブ",
            "下のタブで、一覧・特別出費・設定に切り替えます。"),
        new(ListRoute, "hint", "なおす・けす",
            "行をタップすると、金額や内容をなおせます。左にスワイプすると「けす」が出ます。けしたあとは、画面の下の「もとに戻す」で戻せます。",
            FallbackTarget: "add",
            FallbackBody: "生活費は、ここからもつけられます。つけた行はタップでなおせ、左にスワイプでけせます。"),
        new(SpecialExpenseRoute, "add", "特別出費をつける",
            "特別出費は、このタブの「＋ 特別出費をつける」からつけます。"),
        new(SettingsRoute, "guide", "もう一度見るには",
            "この使い方は、設定の「使い方」からいつでも見られます。"),
    ];
}

/// <summary>
/// Where the tour's bubble goes: below the highlighted part when it fits, otherwise above it,
/// otherwise at the bottom of the screen. The bubble spans the screen width minus the margins.
/// </summary>
public static class TourLayout
{
    public const double Margin = 16;
    public const double Gap = 12;
    public const double HighlightPadding = 6;

    public static Rect Highlight(Rect target, Size screen) =>
        new(Math.Max(0, target.X - HighlightPadding),
            Math.Max(0, target.Y - HighlightPadding),
            Math.Min(screen.Width, target.Width + HighlightPadding * 2),
            target.Height + HighlightPadding * 2);

    public static double BubbleTop(Rect highlight, Size screen, double bubbleHeight, double topInset = 0, double bottomInset = 0)
    {
        var below = highlight.Bottom + Gap;
        if (below + bubbleHeight <= screen.Height - bottomInset - Margin) return below;

        var above = highlight.Top - Gap - bubbleHeight;
        if (above >= topInset + Margin) return above;

        return Math.Max(topInset + Margin, screen.Height - bottomInset - Margin - bubbleHeight);
    }
}
