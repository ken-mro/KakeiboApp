using KakeiboApp.Services;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Controls;

public enum TourChoice { Next, Skip }

/// <summary>
/// Dims the page except one rounded hole around the part being explained, and shows a bubble with the
/// explanation, スキップ and 次へ. Laid over a page's root Grid by <see cref="Services.GuideService"/>.
/// </summary>
public class TourOverlay : Grid, IDrawable
{
    readonly GraphicsView _dim = new();
    readonly Border _bubble;
    readonly Label _counter = new() { FontSize = 12 };
    readonly Label _title = new() { FontFamily = "ZenMaruBold", FontSize = 18 };
    readonly Label _body = new() { FontSize = 15, LineBreakMode = LineBreakMode.WordWrap };
    readonly Button _next = new();
    readonly Button _skip;
    Rect _hole;
    TaskCompletionSource<TourChoice>? _choice;

    public TourOverlay()
    {
        _dim.Drawable = this;
        _dim.BackgroundColor = Colors.Transparent;
        AutomationProperties.SetIsInAccessibleTree(_dim, false);
        // Swallow taps on the dimmed page so nothing underneath runs during the tour.
        _dim.GestureRecognizers.Add(new TapGestureRecognizer());

        _counter.SetDynamicResource(Label.TextColorProperty, "InkMuted");
        _title.SetDynamicResource(Label.TextColorProperty, "Ink");
        _body.SetDynamicResource(Label.TextColorProperty, "Ink");
        SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.Level1);

        _skip = new Button { Text = "スキップ", Style = (Style)Application.Current!.Resources["TextButton"], HorizontalOptions = LayoutOptions.Start };
        SemanticProperties.SetHint(_skip, "使い方を閉じます");
        _skip.Clicked += (_, _) => _choice?.TrySetResult(TourChoice.Skip);
        _next.HeightRequest = -1;
        _next.MinimumHeightRequest = 48;
        _next.Padding = new Thickness(24, 0);
        _next.HorizontalOptions = LayoutOptions.End;
        _next.Clicked += (_, _) => _choice?.TrySetResult(TourChoice.Next);

        var buttons = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)] };
        buttons.Add(_skip, 0);
        buttons.Add(_next, 1);

        _bubble = new Border
        {
            Padding = new Thickness(20, 16, 16, 12),
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(TourLayout.Margin, 0),
            Shadow = new Shadow { Brush = new SolidColorBrush(ThemeTokens.Get("ShadowLavender")), Opacity = 0.3f, Radius = 24, Offset = new Point(0, 8) },
            Content = new VerticalStackLayout { Spacing = 8, Children = { _counter, _title, _body, buttons } },
        };
        _bubble.SetDynamicResource(BackgroundColorProperty, "Surface");

        Children.Add(_dim);
        Children.Add(_bubble);
    }

    /// <summary>Shows one step and waits for 次へ or スキップ.</summary>
    public Task<TourChoice> ShowAsync(Rect target, int index, int count, string title, string body)
    {
        _counter.Text = $"使い方　{index + 1} / {count}";
        _title.Text = title;
        _body.Text = body;
        _next.Text = index == count - 1 ? "はじめる" : "次へ";

        var screen = new Size(Width, Height);
        _hole = TourLayout.Highlight(target, screen);
        // Measure the bubble alone (its Margin is horizontal only) and place it with TranslationY.
        var bubbleHeight = _bubble.Measure(Width, double.PositiveInfinity).Height;
        _bubble.TranslationY = TourLayout.BubbleTop(_hole, screen, bubbleHeight);
        _dim.Invalidate();
        Opacity = 1;
        InputTransparent = false;

        SemanticScreenReader.Default.Announce($"{_counter.Text}。{title}。{body}");
        _choice = new TaskCompletionSource<TourChoice>();
        return _choice.Task;
    }

    /// <summary>Hides the overlay while the next step's screen opens, so the old bubble cannot be tapped.</summary>
    public void Hide()
    {
        // Opacity rather than IsVisible, so the overlay keeps its size and position for measuring the next target.
        Opacity = 0;
        InputTransparent = true;
        _choice = null;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var hole = new RectF((float)_hole.X, (float)_hole.Y, (float)_hole.Width, (float)_hole.Height);
        var path = new PathF();
        path.AppendRectangle(dirtyRect);
        path.AppendRoundedRectangle(hole, 20);
        canvas.FillColor = ThemeTokens.Get("TourScrim");
        canvas.FillPath(path, WindingMode.EvenOdd);

        canvas.StrokeColor = ThemeTokens.Get("Accent");
        canvas.StrokeSize = 3;
        canvas.DrawRoundedRectangle(hole, 20);
    }
}
