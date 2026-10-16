namespace KakeiboApp.Views;

/// <summary>
/// A page the guide's tour can point at. The tour lays its overlay over <see cref="TourRoot"/> and asks the
/// page for the view behind a step's target name, scrolled into view.
/// </summary>
public interface ITourHost
{
    Grid TourRoot { get; }

    /// <summary>Returns the view for <paramref name="target"/>, scrolled into view, or null when it is not shown.</summary>
    Task<View?> PrepareTourTargetAsync(string target);

    /// <summary>While the tour runs, a page can show its usual layout even without data.</summary>
    void SetTouring(bool touring);
}

public static class TourGeometry
{
    /// <summary>
    /// The bounds of <paramref name="view"/> in <paramref name="root"/>'s coordinates. On Android this comes from
    /// the native views, which also covers CollectionView headers and scrolled content.
    /// </summary>
    public static Rect BoundsIn(View view, View root)
    {
#if ANDROID
        if (view.Handler?.PlatformView is Android.Views.View native && root.Handler?.PlatformView is Android.Views.View nativeRoot)
        {
            int[] a = new int[2], b = new int[2];
            native.GetLocationInWindow(a);
            nativeRoot.GetLocationInWindow(b);
            var density = DeviceDisplay.Current.MainDisplayInfo.Density;
            return new Rect((a[0] - b[0]) / density, (a[1] - b[1]) / density, native.Width / density, native.Height / density);
        }
#endif
        double x = 0, y = 0;
        for (Element? e = view; e is VisualElement v && !ReferenceEquals(v, root); e = v.Parent)
        {
            x += v.X + v.TranslationX;
            y += v.Y + v.TranslationY;
            if (v.Parent is ScrollView scroll)
            {
                x -= scroll.ScrollX;
                y -= scroll.ScrollY;
            }
        }
        return new Rect(x, y, view.Width, view.Height);
    }
}
