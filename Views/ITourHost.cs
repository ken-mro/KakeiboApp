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

    /// <summary>While the tour runs, a page can show its usual layout, with example amounts when it has no data.</summary>
    Task SetTouringAsync(bool touring);

    /// <summary>The page is showing the example amounts of <see cref="Services.TourSample"/>.</summary>
    bool ShowsSample => false;

    /// <summary>The area to outline for <paramref name="target"/>, in <paramref name="root"/>'s coordinates.</summary>
    Rect TourBoundsOf(View target, View root) => TourGeometry.BoundsIn(target, root);
}

public static class TourGeometry
{
    /// <summary>Room left above a part scrolled to the top, so its highlight and outline are not cut off.</summary>
    public const double TopRoom = 24;

    /// <summary>Scrolls <paramref name="view"/> to the top of <paramref name="scroll"/>, leaving <see cref="TopRoom"/> above it.</summary>
    public static async Task ScrollNearTopAsync(ScrollView scroll, View view)
    {
        await scroll.ScrollToAsync(view, ScrollToPosition.Start, false);
        if (scroll.ScrollY > 0) await scroll.ScrollToAsync(0, Math.Max(0, scroll.ScrollY - TopRoom), false);
    }

    /// <summary>
    /// Moves <paramref name="view"/> down by <paramref name="dy"/> (up when negative) inside the scroll view it is in.
    /// Near the start of the page there is nothing left to scroll back, so the rest is made up by shifting the
    /// content down; the scroll view is added to <paramref name="shifted"/> so <see cref="Unshift"/> can put it back.
    /// </summary>
    public static async Task MoveDownAsync(View view, double dy, ISet<ScrollView> shifted)
    {
        var scroll = ScrollOf(view);
        if (scroll is null || Math.Abs(dy) < 1) return;
        var y = scroll.ScrollY - dy;
        await scroll.ScrollToAsync(0, Math.Max(0, y), false);
        if (y < 0 && scroll.Content is View content)
        {
            content.TranslationY += -y;
            shifted.Add(scroll);
        }
    }

    /// <summary>Puts back the content <see cref="MoveDownAsync"/> shifted.</summary>
    public static void Unshift(ISet<ScrollView> shifted)
    {
        foreach (var scroll in shifted)
        {
            if (scroll.Content is View content) content.TranslationY = 0;
        }
        shifted.Clear();
    }

    static ScrollView? ScrollOf(Element view)
    {
        for (var e = view.Parent; e is not null; e = e.Parent)
        {
            if (e is ScrollView scroll) return scroll;
        }
        return null;
    }

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
