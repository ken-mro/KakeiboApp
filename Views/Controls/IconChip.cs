using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Controls;

/// <summary>
/// A round chip with an icon in the middle (category and section icons). Decorative.
/// </summary>
public class IconChip : Border
{
    readonly IconPath _icon = new();

    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(IconChip), null, propertyChanged: (b, _, n) => ((IconChip)b)._icon.Glyph = (string?)n);

    public static readonly BindableProperty DiameterProperty =
        BindableProperty.Create(nameof(Diameter), typeof(double), typeof(IconChip), 40d, propertyChanged: (b, _, _) => ((IconChip)b).Resize());

    public static readonly BindableProperty IconSizeProperty =
        BindableProperty.Create(nameof(IconSize), typeof(double), typeof(IconChip), 20d, propertyChanged: (b, _, n) => ((IconChip)b)._icon.Size = (double)n);

    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(IconChip), null, propertyChanged: (b, _, n) => ((IconChip)b)._icon.Color = (Color?)n);

    public IconChip()
    {
        StrokeThickness = 0;
        Padding = 0;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        InputTransparent = true;
        AutomationProperties.SetIsInAccessibleTree(this, false);
        _icon.Size = IconSize;
        Content = _icon;
        Resize();
    }

    public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }
    public double IconSize { get => (double)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public Color? IconColor { get => (Color?)GetValue(IconColorProperty); set => SetValue(IconColorProperty, value); }

    void Resize()
    {
        WidthRequest = Diameter;
        HeightRequest = Diameter;
        StrokeShape = new RoundRectangle { CornerRadius = Diameter / 2 };
    }
}
