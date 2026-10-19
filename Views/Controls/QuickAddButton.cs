using System.Windows.Input;
using KakeiboApp.Services;
using KakeiboApp.ViewModels.Entries;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Controls;

/// <summary>
/// The round ＋ that adds an entry, like a key of flick input. A tap opens a ring around it, cut like baumkuchen,
/// with 特別出費 above and 生活費 to the left; a choice is made by tapping a piece, or straight away by flicking the
/// ＋ up or left. Lays itself over the whole page (the ring needs the room); while closed only the ＋ takes touches.
/// </summary>
public class QuickAddButton : Grid, IDrawable
{
    /// <summary>A piece of the ring: what it adds, its label and the angles it spans (degrees, 0 = right, 90 = up).</summary>
    sealed record Piece(EntryKind Kind, string Label, double From, double To);

    // The quarter above and to the left of the ＋: it sits at the right edge, above the tab bar.
    static readonly Piece[] Pieces =
    [
        new(EntryKind.SpecialExpense, "特別出費", 92, 133),
        new(EntryKind.Spending, "生活費", 137, 178),
    ];

    public const double FabSize = 60;
    const double InnerRadius = 44;
    const double OuterRadius = 132;
    /// <summary>How far the finger has to travel from the ＋ before a flick picks a piece.</summary>
    const double FlickDistance = 28;

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(QuickAddButton));

    /// <summary>Run with the chosen <see cref="EntryKind"/>.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>The ＋ itself, for the guide to point at.</summary>
    public View Fab => _fab;

    readonly GraphicsView _ring = new() { BackgroundColor = Colors.Transparent };
    readonly Border _fab;
    readonly IconPath _plus;
    bool _open;
    Piece? _highlighted;
    double _flickX, _flickY;

    public QuickAddButton()
    {
        _ring.Drawable = this;
        _ring.Opacity = 0;
        _ring.InputTransparent = true;
        var ringTap = new TapGestureRecognizer();
        ringTap.Tapped += (_, e) => OnRingTapped(e.GetPosition(_ring));
        _ring.GestureRecognizers.Add(ringTap);
        AutomationProperties.SetIsInAccessibleTree(_ring, false);

        _plus = new IconPath { Glyph = Icons.Plus, Size = 24, Weight = 2.8 };
        _plus.SetDynamicResource(IconPath.ColorProperty, "OnAccent");
        _fab = new Border
        {
            WidthRequest = FabSize,
            HeightRequest = FabSize,
            StrokeThickness = 0,
            StrokeShape = new Ellipse(),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.End,
            Content = _plus,
            Shadow = new Shadow { Brush = new SolidColorBrush(ThemeTokens.Get("ShadowLavender")), Opacity = 0.30f, Radius = 20, Offset = new Point(0, 8) },
        };
        _fab.SetDynamicResource(BackgroundColorProperty, "Accent");
        SemanticProperties.SetDescription(_fab, "つける");
        SemanticProperties.SetHint(_fab, "ダブルタップで、生活費か特別出費を選ぶ");

        var fabTap = new TapGestureRecognizer();
        fabTap.Tapped += (_, _) => SetOpen(!_open);
        _fab.GestureRecognizers.Add(fabTap);
        var flick = new PanGestureRecognizer();
        flick.PanUpdated += OnFlick;
        _fab.GestureRecognizers.Add(flick);

        // Closed, the page underneath keeps its touches; only the ＋ takes them.
        InputTransparent = true;
        CascadeInputTransparent = false;
        Children.Add(_ring);
        Children.Add(_fab);
    }

    /// <summary>Where the ＋ sits on the page: from the right and bottom edges.</summary>
    public Thickness FabMargin
    {
        get => _fab.Margin;
        set => _fab.Margin = value;
    }

    Point Center => new(Width - _fab.Margin.Right - FabSize / 2, Height - _fab.Margin.Bottom - FabSize / 2);

    void SetOpen(bool open)
    {
        if (_open == open) return;
        _open = open;
        _highlighted = null;
        InputTransparent = !open;
        _ring.InputTransparent = !open;
        _ring.Invalidate();
        _ = _ring.FadeToAsync(open ? 1 : 0, 120);
        _ = _plus.RotateToAsync(open ? 45 : 0, 120);
    }

    /// <summary>Closes the ring, without choosing; for a page that is left or a tour that starts.</summary>
    public void Close() => SetOpen(false);

    void OnFlick(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _flickX = _flickY = 0;
                SetOpen(true);
                break;
            case GestureStatus.Running:
                // Completed reports no distance, so keep the last one.
                _flickX = e.TotalX;
                _flickY = e.TotalY;
                var piece = Math.Sqrt(_flickX * _flickX + _flickY * _flickY) < FlickDistance ? null : PieceAt(_flickX, -_flickY, anyDistance: true);
                if (!ReferenceEquals(piece, _highlighted))
                {
                    _highlighted = piece;
                    _ring.Invalidate();
                }
                break;
            case GestureStatus.Completed:
                // A flick that lands on a piece chooses it; a short one leaves the ring open to tap.
                if (_highlighted is { } chosen) Choose(chosen);
                break;
            case GestureStatus.Canceled:
                _highlighted = null;
                _ring.Invalidate();
                break;
        }
    }

    void OnRingTapped(Point? at)
    {
        if (at is not { } p)
        {
            SetOpen(false);
            return;
        }
        var c = Center;
        if (PieceAt(p.X - c.X, c.Y - p.Y, anyDistance: false) is { } piece) Choose(piece);
        else SetOpen(false);
    }

    /// <summary>
    /// The piece in the direction (dx, dy), y up. A tap (<paramref name="anyDistance"/> false) has to land on a
    /// piece; a flick picks the piece nearest its direction, so straight up is 特別出費 and straight left 生活費.
    /// </summary>
    static Piece? PieceAt(double dx, double dy, bool anyDistance)
    {
        var r = Math.Sqrt(dx * dx + dy * dy);
        var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
        if (angle < 0) angle += 360;
        if (!anyDistance)
        {
            if (r < InnerRadius || r > OuterRadius + 12) return null;
            return Pieces.FirstOrDefault(p => angle >= p.From - 2 && angle <= p.To + 2);
        }
        // Up and left, with some slack toward right and down.
        if (angle < 60 || angle > 210) return null;
        return Pieces.MinBy(p => Math.Abs(angle - (p.From + p.To) / 2));
    }

    void Choose(Piece piece)
    {
        _highlighted = piece;
        _ring.Invalidate();
        SetOpen(false);
        if (Command?.CanExecute(piece.Kind) == true) Command.Execute(piece.Kind);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (!_open) return;
        canvas.FillColor = ThemeTokens.Get("Scrim");
        canvas.FillRectangle(dirtyRect);

        var c = Center;
        foreach (var piece in Pieces)
        {
            var on = ReferenceEquals(piece, _highlighted);
            canvas.FillColor = ThemeTokens.Get(on ? "Accent" : "Surface");
            canvas.FillPath(Slice(c, piece));

            var mid = (piece.From + piece.To) / 2 * Math.PI / 180;
            var r = (InnerRadius + OuterRadius) / 2;
            var x = (float)(c.X + r * Math.Cos(mid));
            var y = (float)(c.Y - r * Math.Sin(mid));
            canvas.FontColor = ThemeTokens.Get(on ? "OnAccent" : "Ink");
            canvas.Font = new Microsoft.Maui.Graphics.Font("ZenMaruBold");
            canvas.FontSize = 15;
            canvas.DrawString(piece.Label, x - 60, y - 12, 120, 24, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }

    /// <summary>The ring between the two radii, over the piece's angles.</summary>
    static PathF Slice(Point c, Piece piece)
    {
        var path = new PathF();
        const int steps = 24;
        for (var i = 0; i <= steps; i++) path.LineTo(At(c, OuterRadius, piece.From + (piece.To - piece.From) * i / steps));
        for (var i = steps; i >= 0; i--) path.LineTo(At(c, InnerRadius, piece.From + (piece.To - piece.From) * i / steps));
        path.Close();
        return path;
    }

    static PointF At(Point c, double radius, double degrees)
    {
        var a = degrees * Math.PI / 180;
        return new PointF((float)(c.X + radius * Math.Cos(a)), (float)(c.Y - radius * Math.Sin(a)));
    }
}
