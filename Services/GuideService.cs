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

    /// <summary>One subject of くわしい使い方.</summary>
    public Task ShowTopicAsync(GuideTopic topic) => RunAsync(topic.Steps, topic.Name, "とじる", recordsVersion: false);

    async Task RunAsync(IReadOnlyList<TourStep> steps, string guideName, string lastButton, bool recordsVersion)
    {
        if (_isShowing) return;
        _isShowing = true;

        var overlay = new TourOverlay();
        overlay.Prepare(steps.SelectMany(s => s.FallbackBody is null
            ? new[] { (s.Title, s.Body) }
            : [(s.Title, s.Body), (s.Title, s.FallbackBody)]));
        var touched = new HashSet<ITourHost>();
        try
        {
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var host = await OpenAsync(step.Route);
                if (host is null) continue;

                if (touched.Add(host)) host.SetTouring(true);
                MoveOverlay(overlay, host.TourRoot);

                var body = step.Body;
                var target = await host.PrepareTourTargetAsync(step.Target);
                if (target is null && step.FallbackTarget is not null)
                {
                    target = await host.PrepareTourTargetAsync(step.FallbackTarget);
                    body = step.FallbackBody ?? body;
                }

                await WaitForSizeAsync(overlay);
                // Measured against the overlay itself: the page's root grid also spans the status bar, its children do not.
                var bounds = target is null ? Rect.Zero : await WaitForBoundsAsync(target, overlay);
                var choice = await overlay.ShowAsync(bounds, i, steps.Count, guideName, lastButton, step.Title, body);
                overlay.Hide();
                if (choice == TourChoice.Skip) break;
            }
        }
        finally
        {
            if (overlay.Parent is Grid grid) grid.Remove(overlay);
            foreach (var host in touched) host.SetTouring(false);
            if (recordsVersion) settings.LastSeenGuideVersion = CurrentVersion;
            _isShowing = false;
            await Shell.Current.GoToAsync("//" + GuideTour.HomeRoute, false);
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

    static async Task<Rect> WaitForBoundsAsync(View target, View root)
    {
        // Scrolling and the first layout of a tab finish a little after the call returns.
        await Task.Delay(150);
        for (var tries = 0; tries < 10 && target.Width <= 0; tries++) await Task.Delay(100);
        return TourGeometry.BoundsIn(target, root);
    }

    static async Task WaitForSizeAsync(View overlay)
    {
        for (var tries = 0; tries < 10 && overlay.Width <= 0; tries++) await Task.Delay(50);
    }
}
