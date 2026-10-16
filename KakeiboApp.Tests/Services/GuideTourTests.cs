using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class GuideTourTests
{
    static readonly Size Phone = new(400, 800);

    static IEnumerable<IReadOnlyList<TourStep>> AllTours => [GuideTour.Quick, .. GuideTour.Topics.Select(t => t.Steps)];

    static IEnumerable<TourStep> AllSteps => AllTours.SelectMany(t => t);

    [Fact]
    public void Quick_IsShortAndEndsByPointingAtTheDetailedGuide()
    {
        Assert.InRange(GuideTour.Quick.Count, 3, 7);
        var last = GuideTour.Quick[^1];
        Assert.Equal(GuideTour.SettingsRoute, last.Route);
        Assert.Contains("くわしい使い方", last.Body);
    }

    [Fact]
    public void Quick_ExplainsUsableMoneyThenSpendingThenSpecialExpenses()
    {
        var steps = GuideTour.Quick.Select(s => (s.Route, s.Target)).ToList();

        int IndexOf(string route, string target) => steps.IndexOf((route, target));
        var usable = IndexOf(GuideTour.HomeRoute, "usable");
        var spending = IndexOf(GuideTour.HomeRoute, "add");
        var special = IndexOf(GuideTour.SpecialExpenseRoute, "add");

        Assert.True(usable >= 0 && usable < spending && spending < special, string.Join(", ", steps));
    }

    [Fact]
    public void Quick_HasNoPageAboutEditing() =>
        Assert.DoesNotContain(GuideTour.Quick, s => s.Target == "hint" || s.Title.Contains("なおす"));

    [Fact]
    public void Quick_LeavesBudgetsToTheTopics()
    {
        Assert.DoesNotContain(GuideTour.Quick, s => s.Target == "spending");
        Assert.Contains("予算", GuideTour.Quick[^1].Body);
        Assert.Contains(GuideTour.Topics, t => t.Steps.Any(s => s.Target == "spending"));
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
    public void Topics_AreShortAndNamedUniquely()
    {
        Assert.InRange(GuideTour.Topics.Count, 2, 5);
        Assert.All(GuideTour.Topics, t => Assert.InRange(t.Steps.Count, 2, 4));
        Assert.Equal(GuideTour.Topics.Count, GuideTour.Topics.Select(t => t.Name).Distinct().Count());
    }

    [Fact]
    public void Topics_CoverBudgetsPaceWithdrawalsCarryOverAndSearch()
    {
        var targets = GuideTour.Topics.SelectMany(t => t.Steps).Select(s => s.Target).ToList();

        Assert.Contains("spending", targets);
        Assert.Contains("pace", targets);
        Assert.Contains("savings", targets);
        Assert.Contains("carry", targets);
        Assert.Contains("search", targets);
    }

    [Fact]
    public void NoStep_PointsAtTheTabBar() =>
        Assert.DoesNotContain(AllSteps, s => s.Target == "tabs");

    [Fact]
    public void Steps_WithAFallbackTarget_AlsoHaveAFallbackText() =>
        Assert.All(AllSteps.Where(s => s.FallbackTarget is not null), s => Assert.False(string.IsNullOrEmpty(s.FallbackBody)));

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
