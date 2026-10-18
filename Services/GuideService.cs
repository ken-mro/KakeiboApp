using KakeiboApp.Repository;
using KakeiboApp.Views;
using KakeiboApp.Views.Controls;

namespace KakeiboApp.Services;

/// <summary>
/// Runs the guide as a tour of the real screens (<see cref="GuideTour"/>): it opens each step's tab,
/// highlights the part being explained and waits for 次へ or スキップ. The quick guide (使い方) is shown on the
/// first launch and the first launch after an update, and on request from 設定; the topics of くわしい使い方
/// only from 設定. Finishing or skipping the quick guide records the app version.
/// </summary>
public class GuideService(SettingPreferences settings)
{
    bool _isShowing;

    public static string CurrentVersion => AppInfo.Current.VersionString;

    public async Task ShowIfNeededAsync()
    {
        if (OnboardingPolicy.ShouldShow(settings.LastSeenGuideVersion, CurrentVersion))
        {
            await ShowAsync();
        }
    }

    /// <summary>使い方: what a new user needs to start.</summary>
    public Task ShowAsync() => RunAsync(GuideTour.Quick, "使い方", "はじめる", recordsVersion: true);

    /// <summary>くわしい使い方: what 使い方 leaves out.</summary>
    public Task ShowDetailedAsync() => RunAsync(GuideTour.Detailed, "くわしい使い方", "とじる", recordsVersion: false);

    async Task RunAsync(IReadOnlyList<TourStep> steps, string guideName, string lastButton, bool recordsVersion)
    {
        if (_isShowing) return;
        _isShowing = true;

        var overlay = new TourOverlay();
        // One place for the whole tour: above the add button when any page explains it, otherwise lower.
        var aboveAddButton = TourLayout.ExplainsAddButton(steps);
        overlay.Prepare(steps.SelectMany(s => s.FallbackBody is null
            ? new[] { (s.Title, s.Body) }
            : [(s.Title, s.Body), (s.Title, s.FallbackBody)]));
        var touched = new HashSet<ITourHost>();
        var shifted = new HashSet<ScrollView>();
        try
        {
            // 次へ moves on and 戻る goes back a page; a page whose screen cannot be opened is passed over in the same direction.
            var direction = 1;
            for (var i = 0; i >= 0 && i < steps.Count; i += direction)
            {
                var step = steps[i];
                var host = await OpenAsync(step.Route);
                if (host is null)
                {
                    if (i == 0) direction = 1;
                    continue;
                }

                if (touched.Add(host)) await host.SetTouringAsync(true);
                MoveOverlay(overlay, host.TourRoot);

                var body = step.Body;
                // A page moved down for the previous step's bubble goes back first, so it scrolls as usual.
                TourGeometry.Unshift(shifted);
                var target = await host.PrepareTourTargetAsync(step.Target);
                if (target is null && step.FallbackTarget is not null)
                {
                    target = await host.PrepareTourTargetAsync(step.FallbackTarget);
                    body = step.FallbackBody ?? body;
                }

                await WaitForSizeAsync(overlay);
                // Measured against the overlay itself: the page's root grid also spans the status bar, its children do not.
                var bounds = target is null ? Rect.Zero : await WaitForBoundsAsync(host, target, overlay);

                // A part too tall for the room above the bubble: this page has the bubble at the top, the part below it.
                var screen = new Size(overlay.Width, overlay.Height);
                var bubbleHeight = overlay.BubbleHeight(guideName, steps.Count, lastButton);
                var bubbleAtTop = target is not null
                    && TourLayout.MovesBubbleUp(bounds, screen, TourLayout.BubbleTop(screen, bubbleHeight, aboveAddButton), bubbleHeight);
                if (bubbleAtTop)
                {
                    await TourGeometry.MoveDownAsync(target!, TourLayout.BelowTopBubble(bubbleHeight) - bounds.Y, shifted);
                    bounds = await WaitForBoundsAsync(host, target!, overlay);
                }

                var choice = await overlay.ShowAsync(bounds, i, steps.Count, guideName, lastButton, step.Title, body, host.ShowsSample, aboveAddButton, bubbleAtTop);
                overlay.Hide();
                if (choice == TourChoice.Skip) break;
                direction = choice == TourChoice.Back ? -1 : 1;
            }
        }
        finally
        {
            if (overlay.Parent is Grid grid) grid.Remove(overlay);
            TourGeometry.Unshift(shifted);
            if (recordsVersion) settings.LastSeenGuideVersion = CurrentVersion;
            _isShowing = false;
            await Shell.Current.GoToAsync("//" + GuideTour.HomeRoute, false);
            // After going home, so the pages put their own data back (no example amounts) while they are shown.
            foreach (var host in touched)
            {
                try
                {
                    await host.SetTouringAsync(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
    }

    static async Task<ITourHost?> OpenAsync(string route)
    {
        if (Shell.Current.CurrentPage is not ITourHost current || RouteOf(current) != route)
        {
            await Shell.Current.GoToAsync("//" + route, false);
        }

        // A tab's page is created on its first visit; give it a moment to appear and lay out.
        for (var tries = 0; tries < 20; tries++)
        {
            if (Shell.Current.CurrentPage is ITourHost host && RouteOf(host) == route && host.TourRoot.Width > 0) return host;
            await Task.Delay(100);
        }
        return Shell.Current.CurrentPage as ITourHost;
    }

    static string RouteOf(ITourHost host) => host switch
    {
        MainPage => GuideTour.HomeRoute,
        SpendingListPage => GuideTour.ListRoute,
        SpecialExpenseListPage => GuideTour.SpecialExpenseRoute,
        SettingPage => GuideTour.SettingsRoute,
        _ => string.Empty,
    };

    static void MoveOverlay(TourOverlay overlay, Grid root)
    {
        if (ReferenceEquals(overlay.Parent, root)) return;
        if (overlay.Parent is Grid old) old.Remove(overlay);
        Grid.SetRowSpan(overlay, Math.Max(1, root.RowDefinitions.Count));
        Grid.SetColumnSpan(overlay, Math.Max(1, root.ColumnDefinitions.Count));
        root.Add(overlay);
    }

    static async Task<Rect> WaitForBoundsAsync(ITourHost host, View target, View root)
    {
        // Scrolling and the first layout of a tab finish a little after the call returns.
        await Task.Delay(150);
        for (var tries = 0; tries < 10 && target.Width <= 0; tries++) await Task.Delay(100);
        return host.TourBoundsOf(target, root);
    }

    static async Task WaitForSizeAsync(View overlay)
    {
        for (var tries = 0; tries < 10 && overlay.Width <= 0; tries++) await Task.Delay(50);
    }
}
