using System.Windows.Input;
using KakeiboApp.Services;
using KakeiboApp.ViewModels.Entries;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Controls;

/// <summary>
/// 「＋ つける」, used like a key of flick input. Putting a finger on it opens a ring around that point, cut like
/// baumkuchen: 特別出費 straight up and 生活費 straight left, with a dead gap between them. Flicking up or left and
/// letting go chooses one; letting go anywhere else closes the ring and the button is back. Only a screen reader,
/// which cannot flick, opens the ring to stay, to tap a piece (or outside, to close).
/// Lays itself over the whole page (the ring needs the room); while closed only the button takes touches.
/// </summary>
public class QuickAddButton : Grid, IDrawable
{
    /// <summary>
    /// A piece of the ring: what it adds, its label and icon, its colours (the app's palette, as in the lists),
    /// and the angles it spans (degrees from the finger, 0 = right, 90 = up).
    /// </summary>
    sealed record Piece(EntryKind Kind, string Label, string Glyph, Color Fill, Color Ink, Color Strong, double From, double To)
    {
        public double Middle => (From + To) / 2;
    }

    // Up and left, apart: the 40° between them is neither (28° even for a flick, which may lean 6° off).
    static readonly Piece[] Pieces =
    [
        new(EntryKind.SpecialExpense, "特別出費", Icons.Gift,
            Color.FromArgb("#FFF0C2"), Color.FromArgb("#8A6A00"), Color.FromArgb("#EDBE45"), 65, 115),
        new(EntryKind.Spending, "生活費", Icons.Receipt,
            Color.FromArgb("#ECE4FF"), Color.FromArgb("#6247B0"), Color.FromArgb("#A98DE6"), 155, 205),
    ];

    const double InnerRadius = 46;
    const double OuterRadius = 156;
    /// <summary>How far a flick may lean off a piece's direction and still choose it.</summary>
    const double FlickSlack = 6;
    /// <summary>How far the finger has to travel before it is a flick rather than a tap.</summary>
    const double FlickDistance = 28;

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(QuickAddButton));

    /// <summary>Run with the chosen <see cref="EntryKind"/>; without one, the button opens the sheet itself (see <see cref="Added"/>).</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>The button itself, for the guide to point at.</summary>
    public View Fab => _fab;

    readonly GraphicsView _ring = new() { BackgroundColor = Colors.Transparent };
    readonly Border _fab;
    readonly IconPath _plus;
    readonly Label _caption;
    static readonly Thickness PillPadding = new(22, 0);
    const double ButtonHeight = 56;
    /// <summary>The label and icon of each piece: real views, so they use the app's fonts.</summary>
    readonly Dictionary<Piece, (VerticalStackLayout View, IconPath Icon, Label Text)> _labels = [];
    bool _open;
    Piece? _highlighted;
    /// <summary>The ring's centre: where the finger went down, moved in only as far as the ring needs to fit.</summary>
    Point _origin;
    /// <summary>Where the finger went down, for the flick's direction.</summary>
    Point _down;

    public QuickAddButton()
    {
        _ring.Drawable = this;
        _ring.Opacity = 0;
        _ring.InputTransparent = true;
        var ringTap = new TapGestureRecognizer();
        ringTap.Tapped += (_, e) => OnRingTapped(e.GetPosition(_ring));
        _ring.GestureRecognizers.Add(ringTap);
        AutomationProperties.SetIsInAccessibleTree(_ring, false);
        Children.Add(_ring);

        foreach (var piece in Pieces)
        {
            var icon = new IconPath { Glyph = piece.Glyph, Size = 24, Weight = 2.2, Color = piece.Ink, HorizontalOptions = LayoutOptions.Center };
            var text = new Label { Text = piece.Label, FontFamily = "ZenMaruBold", FontSize = 15, TextColor = piece.Ink, HorizontalOptions = LayoutOptions.Center };
            var view = new VerticalStackLayout
            {
                Spacing = 4,
                InputTransparent = true,
                Opacity = 0,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Children = { icon, text },
            };
            AutomationProperties.SetIsInAccessibleTree(view, false);
            _labels[piece] = (view, icon, text);
            Children.Add(view);
        }

        _plus = new IconPath { Glyph = Icons.Plus, Size = 20, Weight = 2.6, VerticalOptions = LayoutOptions.Center };
        _plus.SetDynamicResource(IconPath.ColorProperty, "OnAccent");
        _caption = new Label { Text = "つける", FontFamily = "ZenMaruBold", VerticalOptions = LayoutOptions.Center };
        _caption.SetDynamicResource(Label.TextColorProperty, "OnAccent");
        _fab = new Border
        {
            Padding = PillPadding,
            HeightRequest = ButtonHeight,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.End,
            Content = new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Center, Children = { _plus, _caption } },
            Shadow = new Shadow { Brush = new SolidColorBrush(ThemeTokens.Get("ShadowLavender")), Opacity = 0.30f, Radius = 20, Offset = new Point(0, 8) },
        };
        _fab.SetDynamicResource(BackgroundColorProperty, "Accent");
        SemanticProperties.SetDescription(_fab, "つける");
        SemanticProperties.SetHint(_fab, "ダブルタップで、生活費か特別出費を選ぶ");
        _fab.HandlerChanged += (_, _) => HookTouch();
        // Over the ring: open, the button is round and sits in the ring's hole.
        Children.Add(_fab);

        // Closed, the page underneath keeps its touches; only the button takes them.
        InputTransparent = true;
        CascadeInputTransparent = false;
    }

    /// <summary>Where the button sits on the page: from the right and bottom edges.</summary>
    public Thickness FabMargin
    {
        get => _fab.Margin;
        set => _fab.Margin = value;
    }

    /// <summary>The finger's own touches: the ring has to open as it goes down, which no MAUI gesture reports.</summary>
    void HookTouch()
    {
#if ANDROID
        if (_fab.Handler?.PlatformView is not Android.Views.View view) return;
        view.Touch += (_, e) =>
        {
            e.Handled = true;
            if (e.Event is not { } m) return;
            // On screen, not in the button: the button moves to the ring's centre as the finger goes down.
            var density = DeviceDisplay.Current.MainDisplayInfo.Density;
            var me = new int[2];
            (Handler?.PlatformView as Android.Views.View)?.GetLocationOnScreen(me);
            var at = new Point((m.RawX - me[0]) / density, (m.RawY - me[1]) / density);
            switch (m.ActionMasked)
            {
                case Android.Views.MotionEventActions.Down: FingerDown(at); break;
                case Android.Views.MotionEventActions.Move: FingerMoved(at); break;
                case Android.Views.MotionEventActions.Up: FingerUp(); break;
                case Android.Views.MotionEventActions.Cancel: Highlight(null); break;
            }
        };
        // A screen reader's double tap is a click, not a touch: it opens the ring from the button's centre.
        view.Click += (_, _) =>
        {
            if (_open) SetOpen(false);
            else Open(new Point(_fab.X + _fab.Width / 2, _fab.Y + _fab.Height / 2));
        };
#else
        if (_fab.GestureRecognizers.Count > 0) return;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (_open) SetOpen(false);
            else Open(new Point(_fab.X + _fab.Width / 2, _fab.Y + _fab.Height / 2));
        };
        _fab.GestureRecognizers.Add(tap);
#endif
    }

    void FingerDown(Point at)
    {
        _down = at;
        Open(at);
    }

    void FingerMoved(Point at)
    {
        double dx = at.X - _down.X, dy = _down.Y - at.Y;
        // Back near the centre, nothing is chosen, as in flick input.
        var piece = Math.Sqrt(dx * dx + dy * dy) < FlickDistance ? null : FlickedTo(dx, dy);
        if (!ReferenceEquals(piece, _highlighted)) Highlight(piece);
    }

    /// <summary>Letting go chooses the piece the finger flicked to; anywhere else, the ring closes and the button is back.</summary>
    void FingerUp()
    {
        if (_highlighted is { } chosen) Choose(chosen);
        else SetOpen(false);
    }

    void Open(Point finger)
    {
        // Centred on the finger, unless the ring would run off the right or bottom edge (it is drawn over the tab bar).
        var maxX = Width - 8 - OuterRadius * Math.Cos(Pieces[0].From * Math.PI / 180);
        var maxY = Height - 8 + OuterRadius * Math.Sin(Pieces[1].To * Math.PI / 180);
        _origin = new Point(Math.Min(finger.X, maxX), Math.Min(finger.Y, maxY));
        PlaceLabels();
        SetOpen(true);
    }

    /// <summary>Each label in the middle of its piece.</summary>
    void PlaceLabels()
    {
        foreach (var (piece, (view, _, _)) in _labels)
        {
            var mid = At(_origin, (InnerRadius + OuterRadius) / 2, piece.Middle);
            var size = view.Measure(double.PositiveInfinity, double.PositiveInfinity);
            view.TranslationX = mid.X - size.Width / 2;
            view.TranslationY = mid.Y - size.Height / 2;
        }
    }

    void SetOpen(bool open)
    {
        if (_open == open) return;
        _open = open;
        InputTransparent = !open;
        _ring.InputTransparent = !open;
        Highlight(null);
        // Shown at once as the finger goes down, like flick input's guide; faded out on closing.
        var duration = open ? 0u : 120u;
        _ = _ring.FadeToAsync(open ? 1 : 0, duration);
        foreach (var (view, _, _) in _labels.Values) _ = view.FadeToAsync(open ? 1 : 0, duration);
        _ = _plus.RotateToAsync(open ? 45 : 0, 120);
        ShapeButton(open);
    }

    /// <summary>
    /// Open, the button is a round ＋ (the × to close) in the middle of the ring; closed, the 「＋ つける」 pill in
    /// its place. It keeps its place in the layout and is moved with a translation, which the touch ignores.
    /// </summary>
    void ShapeButton(bool round)
    {
        if (!round)
        {
            _caption.IsVisible = true;
            _fab.Padding = PillPadding;
            _fab.WidthRequest = -1;
            _fab.TranslationX = _fab.TranslationY = 0;
            return;
        }
        _caption.IsVisible = false;
        _fab.Padding = 0;
        _fab.WidthRequest = ButtonHeight;
        // Laid out at the bottom right, so its centre is a fixed point once it is round.
        var home = new Point(Width - _fab.Margin.Right - ButtonHeight / 2, Height - _fab.Margin.Bottom - ButtonHeight / 2);
        _fab.TranslationX = _origin.X - home.X;
        _fab.TranslationY = _origin.Y - home.Y;
    }

    /// <summary>Closes the ring, without choosing; for a page that is left or a tour that starts.</summary>
    public void Close() => SetOpen(false);

    void Highlight(Piece? piece)
    {
        _highlighted = piece;
        foreach (var (p, (_, icon, text)) in _labels)
        {
            var color = ReferenceEquals(p, piece) ? Colors.White : p.Ink;
            icon.Color = color;
            text.TextColor = color;
        }
        _ring.Invalidate();
    }

    void OnRingTapped(Point? at)
    {
        if (at is not { } p)
        {
            SetOpen(false);
            return;
        }
        if (TappedOn(p.X - _origin.X, _origin.Y - p.Y) is { } piece) Choose(piece);
        else SetOpen(false);
    }

    static double AngleOf(double dx, double dy)
    {
        var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
        return angle < 0 ? angle + 360 : angle;
    }

    /// <summary>The piece a tap at (dx, dy) from the centre, y up, lands on; the gap and the middle are neither.</summary>
    static Piece? TappedOn(double dx, double dy)
    {
        var r = Math.Sqrt(dx * dx + dy * dy);
        if (r < InnerRadius || r > OuterRadius + 12) return null;
        var angle = AngleOf(dx, dy);
        return Pieces.FirstOrDefault(p => angle >= p.From && angle <= p.To);
    }

    /// <summary>The piece a flick in the direction (dx, dy), y up, points at; between the two, none.</summary>
    static Piece? FlickedTo(double dx, double dy)
    {
        var angle = AngleOf(dx, dy);
        return Pieces.FirstOrDefault(p => angle >= p.From - FlickSlack && angle <= p.To + FlickSlack);
    }

    /// <summary>Raised after an entry was added from the sheet this button opened itself (no <see cref="Command"/>).</summary>
    public event EventHandler? Added;

    void Choose(Piece piece)
    {
        Highlight(piece);
        SetOpen(false);
        if (Command is not null)
        {
            if (Command.CanExecute(piece.Kind)) Command.Execute(piece.Kind);
            return;
        }
        _ = AddAsync(piece.Kind);
    }

    async Task AddAsync(EntryKind kind)
    {
        if (await Sheets.EntrySheet.AddAsync(kind) == SheetOutcome.Saved) Added?.Invoke(this, EventArgs.Empty);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (!_open) return;
        canvas.FillColor = ThemeTokens.Get("Scrim");
        canvas.FillRectangle(dirtyRect);

        foreach (var piece in Pieces)
        {
            canvas.FillColor = ReferenceEquals(piece, _highlighted) ? piece.Strong : piece.Fill;
            canvas.FillPath(Slice(_origin, piece));
        }
        // The edges: both arcs around the same centre, and the radial lines from it.
        canvas.StrokeColor = ThemeTokens.Get("Surface");
        canvas.StrokeSize = 3;
        canvas.StrokeLineJoin = LineJoin.Round;
        foreach (var piece in Pieces) canvas.DrawPath(Slice(_origin, piece));
    }

    /// <summary>The ring between the two radii over the piece's angles: two concentric arcs joined by radial lines.</summary>
    static PathF Slice(Point c, Piece piece)
    {
        const int steps = 48;
        var path = new PathF();
        path.MoveTo(At(c, OuterRadius, piece.From));
        for (var i = 1; i <= steps; i++) path.LineTo(At(c, OuterRadius, piece.From + (piece.To - piece.From) * i / steps));
        path.LineTo(At(c, InnerRadius, piece.To));
        for (var i = steps - 1; i >= 0; i--) path.LineTo(At(c, InnerRadius, piece.From + (piece.To - piece.From) * i / steps));
        path.Close();
        return path;
    }

    static PointF At(Point c, double radius, double degrees)
    {
        var a = degrees * Math.PI / 180;
        return new PointF((float)(c.X + radius * Math.Cos(a)), (float)(c.Y - radius * Math.Sin(a)));
    }
}
