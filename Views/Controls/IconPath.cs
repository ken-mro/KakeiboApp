namespace KakeiboApp.Views.Controls;

/// <summary>
/// A 24×24 stroke icon from Services/Icons, drawn at <see cref="Size"/>. Decorative: hidden from screen readers.
/// The canvas is scaled like an SVG viewBox, so the icon stays centred and is never clipped
/// (a Path with a RenderTransform shifted and cut the scaled geometry).
/// </summary>
public class IconPath : GraphicsView, IDrawable
{
    const float ViewBox = 24f;
    static readonly Microsoft.Maui.Controls.Shapes.PathGeometryConverter Converter = new();

    PathF? _path;

    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(IconPath), null, propertyChanged: (b, _, n) => ((IconPath)b).SetGlyph((string?)n));

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(IconPath), 24d, propertyChanged: (b, _, _) => ((IconPath)b).Resize());

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(IconPath), null, propertyChanged: (b, _, _) => ((IconPath)b).Invalidate());

    public static readonly BindableProperty WeightProperty =
        BindableProperty.Create(nameof(Weight), typeof(double), typeof(IconPath), 2d, propertyChanged: (b, _, _) => ((IconPath)b).Invalidate());

    public IconPath()
    {
        Drawable = this;
        BackgroundColor = Colors.Transparent;
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        InputTransparent = true;
        AutomationProperties.SetIsInAccessibleTree(this, false);
        Resize();
    }

    public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public Color? Color { get => (Color?)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    /// <summary>Stroke width in the 24×24 design units.</summary>
    public double Weight { get => (double)GetValue(WeightProperty); set => SetValue(WeightProperty, value); }

    void SetGlyph(string? glyph)
    {
        // MAUI's own path parser (the one Path.Data uses); PathBuilder misreads compact SVG such as "5.7.7" and arcs.
        _path = null;
        if (!string.IsNullOrEmpty(glyph) && Converter.ConvertFromInvariantString(glyph) is Microsoft.Maui.Controls.Shapes.Geometry geometry)
        {
            _path = new PathF();
            geometry.AppendPath(_path);
        }
        Invalidate();
    }

    void Resize()
    {
        WidthRequest = Size;
        HeightRequest = Size;
        Invalidate();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_path is null || Color is null) return;

        var scale = (float)Math.Min(Width, Height) / ViewBox;
        if (scale <= 0) return;

        canvas.SaveState();
        // Centre the 24×24 box in the view, then scale it, as an SVG viewBox would.
        canvas.Translate((float)(Width - ViewBox * scale) / 2, (float)(Height - ViewBox * scale) / 2);
        canvas.Scale(scale, scale);
        canvas.StrokeColor = Color;
        canvas.StrokeSize = (float)Weight;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(_path);
        canvas.RestoreState();
    }
}
