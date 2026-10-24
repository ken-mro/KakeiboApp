using KakeiboApp.ViewModels;
using KakeiboApp.Views.Controls;

namespace KakeiboApp.Views;

/// <summary>予算を決める, opened from the home's 生活費 with the route <see cref="Route"/>.</summary>
public partial class BudgetsPage : ContentPage
{
    public const string Route = "budgets";

    readonly BudgetsViewModel _vm;
    Entry? _focused;

    public BudgetsPage(BudgetsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        SystemBarInsets.Apply(this, root);
#if ANDROID
        Loaded += (_, _) => WatchKeyboard(true);
        Unloaded += (_, _) => WatchKeyboard(false);
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        // 次へ on every amount but the last, whose key is 完了: Android's 次へ there went on to no text field and
        // brought up the letters.
        var amounts = Amounts();
        foreach (var entry in amounts) entry.ReturnType = ReferenceEquals(entry, amounts[^1]) ? ReturnType.Done : ReturnType.Next;
    }

    /// <summary>The back key asks first, as ‹ does, when there are changes not saved.</summary>
    protected override bool OnBackButtonPressed()
    {
        _ = _vm.BackAsync();
        return true;
    }

    /// <summary>Every amount on the page, top to bottom: the total, then one per kind.</summary>
    List<Entry> Amounts() => [totalEntry, .. lines.Children.OfType<View>().SelectMany(Entries)];

    static IEnumerable<Entry> Entries(IView view) => view switch
    {
        Entry entry => [entry],
        Layout layout => layout.Children.SelectMany(Entries),
        IContentView { PresentedContent: { } content } => Entries(content),
        _ => [],
    };

    void OnAmountFocused(object? sender, FocusEventArgs e)
    {
        _focused = sender as Entry;
        _ = ShowFocusedAsync();
    }

    void OnAmountUnfocused(object? sender, FocusEventArgs e)
    {
        if (ReferenceEquals(_focused, sender)) _focused = null;
    }

    /// <summary>次へ moves on to the next kind; on the last one, 完了 puts the keyboard away.</summary>
    async void OnAmountCompleted(object? sender, EventArgs e)
    {
        if (sender is not Entry entry) return;
        var amounts = Amounts();
        var index = amounts.IndexOf(entry);
        if (index >= 0 && index + 1 < amounts.Count)
        {
            amounts[index + 1].Focus();
            return;
        }
        try
        {
            await entry.HideSoftInputAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
        entry.Unfocus();
    }

    /// <summary>The field being typed in, in view above the figures and the keyboard.</summary>
    async Task ShowFocusedAsync()
    {
        // After the page has made room for the keyboard.
        await Task.Delay(250);
        if (_focused is { } entry) await scroll.ScrollToAsync(entry.Parent as Element ?? entry, ScrollToPosition.MakeVisible, true);
    }

#if ANDROID
    /// <summary>
    /// The page draws under the system bars (<see cref="SystemBarInsets"/>), so the keyboard does not make it smaller:
    /// the lower kinds and the figures were under it. While it is up, the page ends at its top edge instead, the
    /// figures right above it, and 保存 waits until it is down, leaving the room to the kinds.
    /// </summary>
    void WatchKeyboard(bool on)
    {
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        if (decor?.ViewTreeObserver is not { } observer) return;
        if (on) observer.GlobalLayout += OnGlobalLayout;
        else observer.GlobalLayout -= OnGlobalLayout;
    }

    double _keyboard = -1;

    void OnGlobalLayout(object? sender, EventArgs e)
    {
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        if (decor is null) return;
        var insets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(decor);
        if (insets is null) return;
        var density = decor.Resources?.DisplayMetrics?.Density ?? 1;
        var ime = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.Ime())?.Bottom / density ?? 0;
        var bars = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars())?.Bottom / density ?? 0;
        var bottom = Math.Max(ime, bars);
        if (Math.Abs(bottom - _keyboard) < 0.5) return;
        _keyboard = bottom;
        var up = ime > bars;
        root.Padding = root.Padding with { Bottom = bottom };
        saveButton.IsVisible = !up;
        if (up) _ = ShowFocusedAsync();
    }
#endif
}
