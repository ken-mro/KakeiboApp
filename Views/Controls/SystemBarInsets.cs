namespace KakeiboApp.Views.Controls;

/// <summary>
/// Keeps a tab page clear of the status bar and the navigation bar without a jump. When Shell switches tabs,
/// Android hands the new page the bars' insets only after its first frame or two, so MAUI's own safe area
/// lays it out under the status bar, with the floating tab bar lower, and then moves it back. The page pads
/// itself instead, with the insets the window already has.
/// </summary>
public static class SystemBarInsets
{
    public static void Apply(ContentPage page, Layout root)
    {
#if ANDROID
        page.SafeAreaEdges = SafeAreaEdges.None;
        root.SafeAreaEdges = SafeAreaEdges.None;
        void Pad() => root.Padding = Current() ?? root.Padding;
        Pad();
        // The first page is built before the window has its insets.
        page.Loaded += (_, _) => Pad();
        page.Appearing += (_, _) => Pad();
#endif
    }

#if ANDROID
    static Thickness? Current()
    {
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        if (decor is null) return null;
        var insets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(decor);
        if (insets is null) return null;
        var bars = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars()
            | AndroidX.Core.View.WindowInsetsCompat.Type.DisplayCutout());
        var density = decor.Resources?.DisplayMetrics?.Density ?? 1;
        return new Thickness(bars.Left / density, bars.Top / density, bars.Right / density, bars.Bottom / density);
    }
#endif
}
