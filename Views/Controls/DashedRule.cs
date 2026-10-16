namespace KakeiboApp.Views.Controls;

/// <summary>
/// A 1px dashed divider in the Rule colour, as between rows in the design.
/// </summary>
public class DashedRule : GraphicsView, IDrawable
{
    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(DashedRule), Colors.Transparent, propertyChanged: (b, _, _) => ((DashedRule)b).Invalidate());

    public DashedRule()
    {
        HeightRequest = 1;
        InputTransparent = true;
        Drawable = this;
        AutomationProperties.SetIsInAccessibleTree(this, false);
        SetDynamicResource(ColorProperty, "Rule");
    }

    public Color Color { get => (Color)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Color;
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = [4, 3];
        canvas.DrawLine(0, 0.5f, dirtyRect.Width, 0.5f);
    }
}
