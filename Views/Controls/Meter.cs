using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Controls;

/// <summary>
/// A rounded bar on the Track colour, filled to <see cref="Ratio"/> (clamped to 0–1). Decorative: the
/// numbers next to it are what screen readers read.
/// </summary>
public class Meter : Border
{
    readonly Border _fill = new() { StrokeThickness = 0, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Fill };

    public static readonly BindableProperty RatioProperty =
        BindableProperty.Create(nameof(Ratio), typeof(double), typeof(Meter), 0d, propertyChanged: (b, _, _) => ((Meter)b).UpdateFill());

    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(nameof(FillColor), typeof(Color), typeof(Meter), null, propertyChanged: (b, _, n) => ((Meter)b)._fill.BackgroundColor = (Color?)n);

    public static readonly BindableProperty BarHeightProperty =
        BindableProperty.Create(nameof(BarHeight), typeof(double), typeof(Meter), 10d, propertyChanged: (b, _, _) => ((Meter)b).Resize());

    public Meter()
    {
        StrokeThickness = 0;
        Padding = 0;
        InputTransparent = true;
        AutomationProperties.SetIsInAccessibleTree(this, false);
        SetDynamicResource(BackgroundColorProperty, "Track");
        Content = _fill;
        Resize();
        SizeChanged += (_, _) => UpdateFill();
    }

    public double Ratio { get => (double)GetValue(RatioProperty); set => SetValue(RatioProperty, value); }
    public Color? FillColor { get => (Color?)GetValue(FillColorProperty); set => SetValue(FillColorProperty, value); }
    public double BarHeight { get => (double)GetValue(BarHeightProperty); set => SetValue(BarHeightProperty, value); }

    void Resize()
    {
        HeightRequest = BarHeight;
        StrokeShape = new RoundRectangle { CornerRadius = BarHeight / 2 };
        _fill.StrokeShape = new RoundRectangle { CornerRadius = BarHeight / 2 };
    }

    void UpdateFill()
    {
        if (Width <= 0) return;
        var ratio = Math.Clamp(double.IsNaN(Ratio) ? 0 : Ratio, 0, 1);
        _fill.WidthRequest = Width * ratio;
        _fill.IsVisible = ratio > 0;
    }
}
