using System.Windows.Input;
using KakeiboApp.Services;
using KakeiboApp.ViewModels.Entries;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Controls;

/// <summary>
/// 「＋ つける ︿」, used like a key of flick input. Sliding the finger up from it brings in a note for each kind from
/// the edge of the screen on the button's side, above the button, like sticky notes: 収入, 貯金, 固定費, 特別出費 and
/// 生活費, top to bottom. Letting go on a note chooses it; letting go anywhere else puts the notes away and the button
/// is back. A tap (or a screen reader's double tap) brings the notes in to stay, as a menu: tap one to choose it, or
/// the shade, the button or the back key to close. The ︿ on the button, nudged up when the page shows, says the
/// finger can slide up.
/// The button sits in the bottom right corner, or the bottom left one (設定, <see cref="OnLeft"/>).
/// Lays itself over the whole page (the notes and their shade need the room); while closed only the button takes touches.
/// </summary>
public class QuickAddButton : Grid
{
    /// <summary>
    /// A note: what it adds, its label and icon, and its colours (the palette of the kinds, as in the lists): the
    /// soft fill with the ink, and the ink as the fill with white while the finger is on it.
    /// </summary>
    sealed record Note(EntryKind Kind, string Glyph, Color Fill, Color Ink);

    static readonly Note[] Notes =
    [
        new(EntryKind.Income, Icons.Income, Color.FromArgb("#DDF3EA"), Color.FromArgb("#2E7A5E")),
        new(EntryKind.Saving, Icons.Coin, Color.FromArgb("#FFE3EA"), Color.FromArgb("#B23A62")),
        new(EntryKind.FixedCost, Icons.House, Color.FromArgb("#DDEBFF"), Color.FromArgb("#2F62A8")),
        new(EntryKind.SpecialExpense, Icons.Gift, Color.FromArgb("#FFF0C2"), Color.FromArgb("#8A6A00")),
        new(EntryKind.Spending, Icons.Receipt, Color.FromArgb("#ECE4FF"), Color.FromArgb("#6247B0")),
    ];

    const double ButtonHeight = 56;
    const double NoteHeight = 60;
    const double NoteWidth = 200;
    /// <summary>How far the finger has to travel before it is a flick rather than a tap.</summary>
    const double FlickDistance = 28;

    /// <summary>Whether the button sits on the left. Set from 設定; every button on screen moves at once.</summary>
    public static bool OnLeft
    {
        get => _onLeft;
        set
        {
            if (_onLeft == value) return;
            _onLeft = value;
            SideChanged?.Invoke();
        }
    }

    static bool _onLeft;
    static event Action? SideChanged;

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

    readonly BoxView _shade = new() { Opacity = 0, IsVisible = false };
    readonly VerticalStackLayout _notes = new()
    {
        Spacing = 10,
        IsVisible = false,
        VerticalOptions = LayoutOptions.End,
    };
    readonly Dictionary<Note, (View View, Border Paper, IconPath Icon, Label Text)> _noteViews = [];
    readonly Border _fab;
    readonly IconPath _plus;
    /// <summary>The ︿ after つける: the finger can slide up from the button.</summary>
    readonly IconPath _slide;
    Thickness _fabMargin;
    bool _open;
    Note? _highlighted;

    public QuickAddButton()
    {
        _shade.SetDynamicResource(BoxView.ColorProperty, "Scrim");
        var shadeTap = new TapGestureRecognizer();
        shadeTap.Tapped += (_, _) => SetOpen(false);
        _shade.GestureRecognizers.Add(shadeTap);
        AutomationProperties.SetIsInAccessibleTree(_shade, false);
        Children.Add(_shade);

        foreach (var note in Notes)
        {
            var icon = new IconPath { Glyph = note.Glyph, Size = 22, Weight = 2.2, Color = note.Ink, VerticalOptions = LayoutOptions.Center };
            var text = new Label { Text = note.Kind.Name(), FontFamily = "ZenMaruBold", FontSize = 16, TextColor = note.Ink, VerticalOptions = LayoutOptions.Center };
            var paper = new Border
            {
                BackgroundColor = note.Fill,
                HeightRequest = NoteHeight,
                WidthRequest = NoteWidth,
                StrokeThickness = 0,
                Shadow = new Shadow { Brush = new SolidColorBrush(ThemeTokens.Get("ShadowLavender")), Opacity = 0.25f, Radius = 12, Offset = new Point(0, 4) },
                Content = new HorizontalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center, Children = { icon, text } },
            };
            var view = new Pressable
            {
                Body = paper,
                CornerRadius = 20,
                Description = $"{note.Kind.Name()}をつける",
                Command = new Command(() => Choose(note)),
            };
            _noteViews[note] = (view, paper, icon, text);
            _notes.Children.Add(view);
        }
        Children.Add(_notes);

        _plus = new IconPath { Glyph = Icons.Plus, Size = 20, Weight = 2.6, VerticalOptions = LayoutOptions.Center };
        _plus.SetDynamicResource(IconPath.ColorProperty, "OnAccent");
        var caption = new Label { Text = "つける", FontFamily = "ZenMaruBold", VerticalOptions = LayoutOptions.Center };
        caption.SetDynamicResource(Label.TextColorProperty, "OnAccent");
        _slide = new IconPath { Glyph = Icons.ChevronUp, Size = 14, Weight = 2.6, Opacity = 0.85, VerticalOptions = LayoutOptions.Center };
        _slide.SetDynamicResource(IconPath.ColorProperty, "OnAccent");
        _fab = new Border
        {
            Padding = new Thickness(22, 0),
            HeightRequest = ButtonHeight,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            VerticalOptions = LayoutOptions.End,
            Content = new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Center, Children = { _plus, caption, _slide } },
            Shadow = new Shadow { Brush = new SolidColorBrush(ThemeTokens.Get("ShadowLavender")), Opacity = 0.30f, Radius = 20, Offset = new Point(0, 8) },
        };
        _fab.SetDynamicResource(BackgroundColorProperty, "Accent");
        SemanticProperties.SetDescription(_fab, "つける");
        SemanticProperties.SetHint(_fab, "ダブルタップで、つける種類を選ぶ");
        AutomationProperties.SetIsInAccessibleTree(_slide, false);
        _fab.HandlerChanged += (_, _) => HookTouch();
        Children.Add(_fab);

#if ANDROID
        _back = new BackKey(Close);
#endif
        // Closed, the page underneath keeps its touches; only the button takes them.
        InputTransparent = true;
        CascadeInputTransparent = false;

        PlaceButton();
        Loaded += (_, _) =>
        {
            SideChanged += PlaceButton;
            PlaceButton();
            _ = NudgeAsync();
        };
        Unloaded += (_, _) => SideChanged -= PlaceButton;
    }

    /// <summary>Where the button sits on the page: from the right (or, on the left, the left) and bottom edges.</summary>
    public Thickness FabMargin
    {
        get => _fabMargin;
        set
        {
            _fabMargin = value;
            PlaceButton();
        }
    }

    /// <summary>The button and the notes on its side; each note is stuck to that edge and rounded at its other end.</summary>
    void PlaceButton()
    {
        var side = Math.Max(_fabMargin.Left, _fabMargin.Right);
        var start = OnLeft ? LayoutOptions.Start : LayoutOptions.End;
        _fab.HorizontalOptions = start;
        _fab.Margin = OnLeft
            ? new Thickness(side, _fabMargin.Top, 0, _fabMargin.Bottom)
            : new Thickness(0, _fabMargin.Top, side, _fabMargin.Bottom);
        // Just above the button, so the finger slides up onto them.
        _notes.HorizontalOptions = start;
        _notes.Margin = new Thickness(0, 0, 0, _fabMargin.Bottom + ButtonHeight + 16);
        foreach (var (view, paper, _, _) in _noteViews.Values)
        {
            view.HorizontalOptions = start;
            paper.StrokeShape = new RoundRectangle { CornerRadius = OnLeft ? new CornerRadius(0, 20, 0, 20) : new CornerRadius(20, 0, 20, 0) };
            paper.Padding = OnLeft ? new Thickness(24, 0, 20, 0) : new Thickness(20, 0, 24, 0);
        }
    }

    /// <summary>The finger's own touches: the notes come in during the flick, which no MAUI gesture reports.</summary>
    void HookTouch()
    {
#if ANDROID
        if (_fab.Handler?.PlatformView is not Android.Views.View view) return;
        Point down = default;
        var moved = false;
        var wasOpen = false;
        view.Touch += (_, e) =>
        {
            e.Handled = true;
            if (e.Event is not { } m) return;
            var density = DeviceDisplay.Current.MainDisplayInfo.Density;
            var me = new int[2];
            (Handler?.PlatformView as Android.Views.View)?.GetLocationOnScreen(me);
            var at = new Point((m.RawX - me[0]) / density, (m.RawY - me[1]) / density);
            switch (m.ActionMasked)
            {
                case Android.Views.MotionEventActions.Down:
                    down = at;
                    moved = false;
                    wasOpen = _open;
                    break;
                case Android.Views.MotionEventActions.Move:
                    double dx = at.X - down.X, dy = at.Y - down.Y;
                    if (Math.Sqrt(dx * dx + dy * dy) >= FlickDistance) moved = true;
                    if (moved && !_open) SetOpen(true);
                    if (_open) Highlight(NoteAt(at));
                    break;
                case Android.Views.MotionEventActions.Up:
                    if (!moved)
                    {
                        // A tap: the notes come in as a menu and stay, to be tapped; a tap on the × puts them away.
                        SetOpen(!wasOpen);
                    }
                    // The end of a slide: on a note, that one; anywhere else, the notes go and the button is back.
                    else if (_open && NoteAt(at) is { } note) Choose(note);
                    else SetOpen(false);
                    break;
                case Android.Views.MotionEventActions.Cancel:
                    SetOpen(false);
                    break;
            }
        };
        // A screen reader's double tap is a click, not a touch: the notes stay, as after a tap.
        view.Click += (_, _) => SetOpen(!_open);
#else
        if (_fab.GestureRecognizers.Count > 0) return;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => SetOpen(!_open);
        _fab.GestureRecognizers.Add(tap);
#endif
    }

    /// <summary>
    /// The note under <paramref name="at"/> (in this view's coordinates), if any. Where it ends up, not where it is
    /// while still sliding in: a quick finger is on its place before the note is.
    /// </summary>
    Note? NoteAt(Point at)
    {
        foreach (var (note, (view, _, _, _)) in _noteViews)
        {
            var bounds = new Rect(_notes.X + view.X, _notes.Y + view.Y, view.Width, view.Height);
            if (bounds.Contains(at)) return note;
        }
        return null;
    }

    /// <summary>The ︿ moves up and back twice, so it is seen to point the way the finger goes.</summary>
    async Task NudgeAsync()
    {
        await Task.Delay(600);
        for (var i = 0; i < 2 && !_open; i++)
        {
            await _slide.TranslateToAsync(0, -5, 180, Easing.CubicOut);
            await _slide.TranslateToAsync(0, 0, 220, Easing.CubicIn);
        }
    }

    void SetOpen(bool open)
    {
        if (_open == open) return;
        _open = open;
        InputTransparent = !open;
        Highlight(null);
        _ = _plus.RotateToAsync(open ? 45 : 0, 120);
        _slide.IsVisible = !open;
        SemanticProperties.SetDescription(_fab, open ? "とじる" : "つける");
#if ANDROID
        if (open) (Platform.CurrentActivity as AndroidX.Activity.ComponentActivity)?.OnBackPressedDispatcher.AddCallback(_back);
        else _back.Remove();
#endif
        if (open) _ = ShowNotesAsync();
        else _ = HideNotesAsync();
    }

    /// <summary>The notes slide in from the button's edge one after another, the one nearest the button first, over a shade.</summary>
    async Task ShowNotesAsync()
    {
        _shade.IsVisible = _notes.IsVisible = true;
        _notes.Opacity = 1;
        _ = _shade.FadeToAsync(1, 120);
        var views = _noteViews.Values.Select(v => v.View).Reverse().ToList();
        var away = (OnLeft ? -1 : 1) * (NoteWidth + 24);
        foreach (var view in views) view.TranslationX = away;
        foreach (var view in views)
        {
            _ = view.TranslateToAsync(0, 0, 160, Easing.CubicOut);
            await Task.Delay(25);
        }
    }

    async Task HideNotesAsync()
    {
        _ = _shade.FadeToAsync(0, 120);
        await _notes.FadeToAsync(0, 120);
        if (_open) return;
        _shade.IsVisible = _notes.IsVisible = false;
    }

    /// <summary>Puts the notes away, without choosing; for a page that is left or a tour that starts.</summary>
    public void Close() => SetOpen(false);

    /// <summary>
    /// The note under the finger, which letting go would choose, turns its ink colour with white letters. A colour
    /// rather than a size: a note grown in place was cut off by the stack around it.
    /// </summary>
    void Highlight(Note? note)
    {
        if (ReferenceEquals(note, _highlighted)) return;
        _highlighted = note;
        foreach (var (n, (_, paper, icon, text)) in _noteViews)
        {
            var on = ReferenceEquals(n, note);
            paper.BackgroundColor = on ? n.Ink : n.Fill;
            icon.Color = on ? Colors.White : n.Ink;
            text.TextColor = on ? Colors.White : n.Ink;
        }
    }

    /// <summary>Raised after an entry was added from the sheet this button opened itself (no <see cref="Command"/>).</summary>
    public event EventHandler? Added;

    void Choose(Note note)
    {
        SetOpen(false);
        if (Command is not null)
        {
            if (Command.CanExecute(note.Kind)) Command.Execute(note.Kind);
            return;
        }
        _ = AddAsync(note.Kind);
    }

    async Task AddAsync(EntryKind kind)
    {
        if (await Sheets.EntrySheet.AddAsync(kind) == SheetOutcome.Saved) Added?.Invoke(this, EventArgs.Empty);
    }

#if ANDROID
    readonly BackKey _back;

    sealed class BackKey(Action onBack) : AndroidX.Activity.OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed() => onBack();
    }
#endif
}
