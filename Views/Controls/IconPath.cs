using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace KakeiboApp.Views.Controls;

/// <summary>
/// A 24×24 stroke icon from Services/Icons, scaled to <see cref="Size"/>. Decorative: hidden from screen readers.
/// </summary>
public class IconPath : ContentView
{
    static readonly PathGeometryConverter Converter = new();

    readonly Path _path = new()
    {
        Aspect = Stretch.None,
        StrokeLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
    };

    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(IconPath), null, propertyChanged: (b, _, _) => ((IconPath)b).Update());

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(IconPath), 24d, propertyChanged: (b, _, _) => ((IconPath)b).Update());

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(IconPath), null, propertyChanged: (b, _, n) => ((IconPath)b)._path.Stroke = (Color?)n);

    public static readonly BindableProperty WeightProperty =
        BindableProperty.Create(nameof(Weight), typeof(double), typeof(IconPath), 2d, propertyChanged: (b, _, _) => ((IconPath)b).Update());

    public IconPath()
    {
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        InputTransparent = true;
        AutomationProperties.SetIsInAccessibleTree(this, false);
        Content = _path;
        Update();
    }

    public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public Color? Color { get => (Color?)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    /// <summary>Stroke width in the 24×24 design units.</summary>
    public double Weight { get => (double)GetValue(WeightProperty); set => SetValue(WeightProperty, value); }

    void Update()
    {
        var scale = Size / 24d;
        WidthRequest = Size;
        HeightRequest = Size;
        _path.WidthRequest = Size;
        _path.HeightRequest = Size;
        _path.StrokeThickness = Weight * scale;
        _path.Data = string.IsNullOrEmpty(Glyph) ? null : (Geometry?)Converter.ConvertFromInvariantString(Glyph);
        _path.RenderTransform = new ScaleTransform(scale, scale);
    }
}
