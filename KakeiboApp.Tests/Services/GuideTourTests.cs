using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class GuideTourTests
{
    static readonly Size Phone = new(400, 800);

    static IEnumerable<TourStep> AllSteps => [.. GuideTour.Quick, .. GuideTour.Detailed];

    static int IndexOf(string route, string target) =>
        GuideTour.Quick.Select(s => (s.Route, s.Target)).ToList().IndexOf((route, target));

    [Fact]
    public void Quick_StartsWithAnOverviewWithoutAHighlight()
    {
        var first = GuideTour.Quick[0];
        Assert.Equal(GuideTour.OverviewTarget, first.Target);
        Assert.Contains("今月使えるお金", first.Body);
    }

    [Fact]
    public void Quick_EntersTheMoneyThenShowsTheResultThenPointsAtTheDetailedGuide()
    {
        var usable = IndexOf(GuideTour.HomeRoute, "usable");
        var spending = IndexOf(GuideTour.HomeRoute, "add");
        var special = IndexOf(GuideTour.SpecialExpenseRoute, "add");
        var result = IndexOf(GuideTour.HomeRoute, "hero");
        var more = IndexOf(GuideTour.SettingsRoute, "guide");

        Assert.Equal([1, 2, 3, 4, 5], new[] { usable, spending, special, result, more });
        Assert.Equal(GuideTour.Quick.Count - 1, more);
        Assert.Contains("くわしい使い方", GuideTour.Quick[^1].Body);
    }

    [Fact]
    public void Quick_ExplainsThePaceWithTheResult()
    {
        Assert.Contains("ペース", GuideTour.Quick[IndexOf(GuideTour.HomeRoute, "hero")].Body);
        Assert.DoesNotContain(GuideTour.Detailed, s => s.Target == "pace" || s.Body.Contains("ペース"));
    }

    [Fact]
    public void Quick_HasNoPageAboutEditing() =>
        Assert.DoesNotContain(GuideTour.Quick, s => s.Target == "hint" || s.Title.Contains("なおす"));

    [Fact]
    public void Detailed_CoversBudgetsWithdrawalsAndCarryOver_AndNotEntering()
    {
        var targets = GuideTour.Detailed.Select(s => s.Target).ToList();

        Assert.Equal(["spending", "savings", "hero", "carry"], targets);
        Assert.DoesNotContain(GuideTour.Detailed, s => s.Target == "search" || s.Target == "add");
        Assert.DoesNotContain(GuideTour.Quick, s => s.Target == "spending");
    }

    [Fact]
    public void NothingIsExplainedTwice()
    {
        var titles = AllSteps.Select(s => s.Title).ToList();
        var bodies = AllSteps.Select(s => s.Body).ToList();

        Assert.Equal(titles.Count, titles.Distinct().Count());
        Assert.Equal(bodies.Count, bodies.Distinct().Count());
    }

    [Fact]
    public void NoStep_PointsAtTheTabBar() =>
        Assert.DoesNotContain(AllSteps, s => s.Target == "tabs");

    [Fact]
    public void Steps_WithAFallbackTarget_AlsoHaveAFallbackText() =>
        Assert.All(AllSteps.Where(s => s.FallbackTarget is not null), s => Assert.False(string.IsNullOrEmpty(s.FallbackBody)));

    [Fact]
    public void Bubble_SitsJustAboveTheTabBar()
    {
        // 800 - 104 - 200: as low as it goes while the tab bar stays in view.
        Assert.Equal(496, TourLayout.BubbleTop(Phone, 200));
    }

    [Fact]
    public void Bubble_IsAboveTheAddButtonOnlyInToursThatExplainIt()
    {
        Assert.True(TourLayout.ExplainsAddButton(GuideTour.Quick));
        Assert.False(TourLayout.ExplainsAddButton(GuideTour.Detailed));
    }

    [Fact]
    public void Bubble_InATourAboutTheAddButton_SitsAboveTheButton()
    {
        // 800 - 180 - 200, so the add button between the bubble and the tab bar stays in view.
        Assert.Equal(420, TourLayout.BubbleTop(Phone, 200, aboveAddButton: true));
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

        // The outline is drawn 2 inside the edges so it is not cut off.
        Assert.Equal(2, highlight.X);
        Assert.Equal(2, highlight.Y);
        Assert.Equal(396, highlight.Width);
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
