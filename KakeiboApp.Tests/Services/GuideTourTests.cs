using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class GuideTourTests
{
    static readonly Size Phone = new(400, 800);

    [Fact]
    public void Steps_VisitEveryTabAndEndInSettings()
    {
        var routes = GuideTour.Steps.Select(s => s.Route).Distinct().ToList();

        Assert.Equal([GuideTour.HomeRoute, GuideTour.ListRoute, GuideTour.SpecialExpenseRoute, GuideTour.SettingsRoute], routes);
        Assert.Equal("guide", GuideTour.Steps[^1].Target);
    }

    [Fact]
    public void Steps_ExplainBudgetsWithdrawalsAndSpecialExpenses()
    {
        var bodies = string.Join("\n", GuideTour.Steps.Select(s => s.Body));

        Assert.Contains("予算", bodies);
        Assert.Contains("切り崩", bodies);
        Assert.Contains("残りのお金には入りません", bodies);
        Assert.Contains("もとに戻す", bodies);
    }

    [Fact]
    public void Steps_WithAFallbackTarget_AlsoHaveAFallbackText()
    {
        Assert.All(GuideTour.Steps.Where(s => s.FallbackTarget is not null), s => Assert.False(string.IsNullOrEmpty(s.FallbackBody)));
    }

    [Fact]
    public void Highlight_AddsPaddingAroundTheTarget()
    {
        var highlight = TourLayout.Highlight(new Rect(16, 100, 368, 200), Phone);

        Assert.Equal(new Rect(10, 94, 380, 212), highlight);
    }

    [Fact]
    public void Highlight_StaysOnScreenAtTheEdges()
    {
        var highlight = TourLayout.Highlight(new Rect(0, 0, 400, 50), Phone);

        Assert.Equal(0, highlight.X);
        Assert.Equal(0, highlight.Y);
        Assert.Equal(400, highlight.Width);
    }

    [Fact]
    public void Bubble_GoesBelowTheHighlightWhenItFits()
    {
        Assert.Equal(312, TourLayout.BubbleTop(new Rect(10, 100, 380, 200), Phone, 200));
    }

    [Fact]
    public void Bubble_GoesAboveWhenThereIsNoRoomBelow()
    {
        // The add button near the bottom: the bubble sits above it.
        Assert.Equal(488, TourLayout.BubbleTop(new Rect(200, 700, 180, 60), Phone, 200));
    }

    [Fact]
    public void Bubble_GoesToTheBottomWhenNeitherFits()
    {
        // A tall card filling most of the screen.
        Assert.Equal(584, TourLayout.BubbleTop(new Rect(10, 40, 380, 700), Phone, 200));
    }

    [Fact]
    public void Bubble_KeepsClearOfTheSystemBars()
    {
        // Pinned to the bottom, but above the navigation bar: 800 - 24 - 16 - 200.
        Assert.Equal(560, TourLayout.BubbleTop(new Rect(10, 40, 380, 700), Phone, 200, topInset: 24, bottomInset: 24));
    }
}
