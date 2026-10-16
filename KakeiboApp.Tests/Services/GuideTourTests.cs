using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class GuideTourTests
{
    static readonly Size Phone = new(400, 800);

    public static TheoryData<string> Guides => ["Quick", "Detailed"];

    static IReadOnlyList<TourStep> Get(string name) => name == "Quick" ? GuideTour.Quick : GuideTour.Detailed;

    [Fact]
    public void Quick_IsShortAndEndsByPointingAtTheDetailedGuide()
    {
        Assert.InRange(GuideTour.Quick.Count, 3, 6);
        var last = GuideTour.Quick[^1];
        Assert.Equal(GuideTour.SettingsRoute, last.Route);
        Assert.Contains("くわしい使い方", last.Body);
    }

    [Fact]
    public void Quick_CoversAddingBudgetsAndEditing()
    {
        var targets = GuideTour.Quick.Select(s => s.Target).ToList();

        Assert.Contains("add", targets);
        Assert.Contains("spending", targets);
        Assert.Contains("hint", targets);
    }

    [Fact]
    public void Detailed_VisitsEveryTabAndEndsInSettings()
    {
        var routes = GuideTour.Detailed.Select(s => s.Route).Distinct().ToList();

        Assert.Equal([GuideTour.HomeRoute, GuideTour.ListRoute, GuideTour.SpecialExpenseRoute, GuideTour.SettingsRoute], routes);
        Assert.Equal("detail", GuideTour.Detailed[^1].Target);
    }

    [Fact]
    public void Detailed_ExplainsBudgetsWithdrawalsSpecialExpensesAndSearch()
    {
        var bodies = string.Join("\n", GuideTour.Detailed.Select(s => s.Body));

        Assert.Contains("予算", bodies);
        Assert.Contains("切り崩", bodies);
        Assert.Contains("残りのお金には入りません", bodies);
        Assert.Contains("もとに戻す", bodies);
        Assert.Contains("さがせます", bodies);
    }

    [Theory]
    [MemberData(nameof(Guides))]
    public void NoStep_PointsAtTheTabBar(string guide) =>
        Assert.DoesNotContain(Get(guide), s => s.Target == "tabs");

    [Theory]
    [MemberData(nameof(Guides))]
    public void Steps_WithAFallbackTarget_AlsoHaveAFallbackText(string guide) =>
        Assert.All(Get(guide).Where(s => s.FallbackTarget is not null), s => Assert.False(string.IsNullOrEmpty(s.FallbackBody)));

    [Fact]
    public void Bubble_StaysInOnePlaceAboveTheAddButtonAndTabBar()
    {
        // 800 - 180 - 200, whatever is highlighted.
        Assert.Equal(420, TourLayout.BubbleTop(Phone, 200));
    }

    [Fact]
    public void Bubble_StaysOnScreenWhenItIsTall()
    {
        Assert.Equal(TourLayout.Margin, TourLayout.BubbleTop(Phone, 700));
    }

    [Fact]
    public void Highlight_AddsPaddingAroundTheTarget()
    {
        var highlight = TourLayout.Highlight(new Rect(16, 100, 368, 200), Phone, bubbleTop: 420);

        Assert.Equal(new Rect(10, 94, 380, 212), highlight);
    }

    [Fact]
    public void Highlight_StaysOnScreenAtTheEdges()
    {
        var highlight = TourLayout.Highlight(new Rect(0, 0, 400, 50), Phone, bubbleTop: 420);

        Assert.Equal(0, highlight.X);
        Assert.Equal(0, highlight.Y);
        Assert.Equal(400, highlight.Width);
    }

    [Fact]
    public void Highlight_OfATallCard_StopsAboveTheBubble()
    {
        // The budget card can be taller than the room above the bubble: cut it off 12 above the bubble.
        var highlight = TourLayout.Highlight(new Rect(16, 60, 368, 600), Phone, bubbleTop: 420);

        Assert.Equal(54, highlight.Top);
        Assert.Equal(408, highlight.Bottom);
    }

    [Fact]
    public void Highlight_BelowTheBubble_IsKept()
    {
        // The floating add button sits between the bubble and the tab bar.
        var highlight = TourLayout.Highlight(new Rect(200, 632, 180, 56), Phone, bubbleTop: 420);

        Assert.Equal(626, highlight.Top);
        Assert.Equal(694, highlight.Bottom);
    }

    [Fact]
    public void Dimmed_LeavesTheTabBarStripClear()
    {
        Assert.Equal(new Rect(0, 0, 400, 700), TourLayout.Dimmed(Phone));
    }
}
