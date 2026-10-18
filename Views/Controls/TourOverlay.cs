using KakeiboApp.Services;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Controls;

public enum TourChoice { Next, Back, Skip }

/// <summary>
/// Dims the page except one rounded hole around the part being explained and the tab bar, and shows a bubble
/// with the explanation, スキップ, 戻る and 次へ in a fixed place above the tab bar (<see cref="TourLayout"/>).
/// Laid over a page's root Grid by <see cref="Services.GuideService"/>.
/// </summary>
public class TourOverlay : Grid, IDrawable
{
    readonly GraphicsView _dim = new();
    readonly Border _bubble;
    readonly Label _counter = new() { FontSize = 12, VerticalOptions = LayoutOptions.Center };
    readonly Border _sample;
    readonly Label _title = new() { FontFamily = "ZenMaruBold", FontSize = 18 };
    readonly Label _body = new() { FontSize = 15, LineBreakMode = LineBreakMode.WordWrap };
    readonly Button _next = new();
    readonly Button _back;
    readonly Button _skip;
    Rect _hole;
    Rect _dimmed;
    TaskCompletionSource<TourChoice>? _choice;
    IReadOnlyList<(string Title, string Body)> _texts = [];
    double _bubbleHeight;

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

        // 戻る: back to the previous page of the tour, which may be on another tab.
        _back = new Button { Text = "戻る", Style = (Style)Application.Current!.Resources["SmallOutlineButton"], MinimumHeightRequest = 48 };
        SemanticProperties.SetHint(_back, "前の説明に戻ります");
        _back.Clicked += (_, _) => _choice?.TrySetResult(TourChoice.Back);

        var buttons = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto)], ColumnSpacing = 8 };
        buttons.Add(_skip, 0);
        buttons.Add(_back, 1);
        buttons.Add(_next, 2);

        _bubble = new Border
        {
            Padding = new Thickness(20, 16, 16, 12),
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(TourLayout.Margin, 0),
            Shadow = new Shadow { Brush = new SolidColorBrush(ThemeTokens.Get("ShadowLavender")), Opacity = 0.3f, Radius = 24, Offset = new Point(0, 8) },
        };
        // The buttons stay at the bottom when a short text leaves the bubble taller than it needs.
        var content = new Grid { RowSpacing = 8, RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)] };
        // 「見本」: the amounts on the page are TourSample's, not the user's.
        var sampleText = new Label { Text = TourSample.Notice, FontFamily = "ZenMaruBold", FontSize = 12, VerticalOptions = LayoutOptions.Center };
        sampleText.SetDynamicResource(Label.TextColorProperty, "AccentInk");
        _sample = new Border
        {
            Padding = new Thickness(10, 3),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = sampleText,
            HorizontalOptions = LayoutOptions.End,
        };
        _sample.SetDynamicResource(BackgroundColorProperty, "AccentSoft");
        var header = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 8 };
        header.Add(_counter, 0);
        header.Add(_sample, 1);
        content.Add(header, 0, 0);
        content.Add(_title, 0, 1);
        content.Add(_body, 0, 2);
        content.Add(buttons, 0, 4);
        _bubble.Content = content;
        _bubble.SetDynamicResource(BackgroundColorProperty, "Surface");

        Children.Add(_dim);
        Children.Add(_bubble);
    }

    /// <summary>
    /// Every text the tour may show. The bubble is made as tall as the longest one, so its top edge stays in the
    /// same place on every step too.
    /// </summary>
    public void Prepare(IEnumerable<(string Title, string Body)> texts)
    {
        _texts = texts.ToList();
        _bubbleHeight = 0;
    }

    double MeasureBubble(string counter, string next)
    {
        _bubble.HeightRequest = -1;
        _counter.Text = counter;
        _next.Text = next;
        _back.IsVisible = true;
        _sample.IsVisible = true;
        var height = 0d;
        foreach (var (title, body) in _texts)
        {
            _title.Text = title;
            _body.Text = body;
            // Measure the bubble alone (its Margin is horizontal only).
            height = Math.Max(height, _bubble.Measure(Width, double.PositiveInfinity).Height);
        }
        return height;
    }

    /// <summary>Shows one step and waits for 次へ, 戻る or スキップ. <paramref name="target"/> is empty when nothing is highlighted.</summary>
    public Task<TourChoice> ShowAsync(Rect target, int index, int count, string guideName, string lastButton, string title, string body, bool showsSample, bool aboveAddButton)
    {
        var counter = $"{guideName}　{index + 1} / {count}";
        if (_bubbleHeight <= 0) _bubbleHeight = MeasureBubble(counter, lastButton);
        _counter.Text = counter;
        _sample.IsVisible = showsSample;
        _title.Text = title;
        _body.Text = body;
        _next.Text = index == count - 1 ? lastButton : "次へ";
        _back.IsVisible = index > 0;

        var screen = new Size(Width, Height);
        // Fixed height, placed with TranslationY.
        var bubbleHeight = Math.Max(_bubbleHeight, _bubble.Measure(Width, double.PositiveInfinity).Height);
        _bubble.HeightRequest = bubbleHeight;
        var bubbleTop = TourLayout.BubbleTop(screen, bubbleHeight, aboveAddButton);
        _bubble.TranslationY = bubbleTop;
        _dimmed = TourLayout.Dimmed(screen);
        _hole = target.Width > 0 && target.Height > 0 ? TourLayout.Highlight(target, screen) : Rect.Zero;
        _dim.Invalidate();
        Opacity = 1;
        InputTransparent = false;

        SemanticScreenReader.Default.Announce($"{_counter.Text}。{(showsSample ? TourSample.Notice + "。" : string.Empty)}{title}。{body}");
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
        path.AppendRectangle(new RectF((float)_dimmed.X, (float)_dimmed.Y, (float)_dimmed.Width, (float)_dimmed.Height));
        if (!hole.IsEmpty) path.AppendRoundedRectangle(hole, 20);
        canvas.FillColor = ThemeTokens.Get("TourScrim");
        canvas.FillPath(path, WindingMode.EvenOdd);
        if (hole.IsEmpty) return;

        canvas.StrokeColor = ThemeTokens.Get("Accent");
        canvas.StrokeSize = 3;
        canvas.DrawRoundedRectangle(hole, 20);
    }
}
