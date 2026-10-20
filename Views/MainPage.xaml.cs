using KakeiboApp.Models;
using KakeiboApp.Services;
using KakeiboApp.ViewModels;
using KakeiboApp.Views.Controls;
using Syncfusion.Maui.Picker;

namespace KakeiboApp.Views;

public partial class MainPage : ContentPage, ITourHost
{
    public Grid TourRoot => root;

    public async Task SetTouringAsync(bool touring)
    {
        _vm.IsTouring = touring;
        if (!touring) quickAdd.Opacity = 1;
        // The tour opens 今月使えるお金 and 生活費 to show their rows; the home starts with them closed again.
        _vm.IsUsableOpen = _vm.IsSpendingOpen = false;
        // An empty month shows example amounts during the tour, and its own again afterwards.
        await _vm.LoadAsync();
        // The tour scrolls down to the lower cards; start again from the top afterwards.
        // Not awaited: on a page that is not shown the scroll may never report that it finished.
        if (!touring) _ = scroll.ScrollToAsync(0, 0, false);
    }

    public bool ShowsSample => _vm.ShowsSample;

    public async Task<View?> PrepareTourTargetAsync(string target)
    {
        // The floating add button would lie over a card outlined below it (a bubble moved to the top); it is shown
        // on the overview and on its own page only.
        quickAdd.Opacity = target is GuideTour.OverviewTarget or TourLayout.AddButtonTarget ? 1 : 0;
        // The overview is about the whole home: show it from the top and highlight nothing.
        if (target == GuideTour.OverviewTarget)
        {
            await scroll.ScrollToAsync(0, 0, false);
            return null;
        }

        View? view = target switch
        {
            "summary" => heroSummary,
            "pace" => paceCard,
            "usable" => usableCard,
            "spending" => spendingCard,
            "savings" => savingsLink,
            "special" => specialLink,
            "add" => quickAdd.Fab,
            _ => null,
        };
        if (view is null || !view.IsVisible) return null;
        // The steps about 今月使えるお金 and 生活費 are about their rows, which show once the card is open.
        if (view == usableCard) _vm.IsUsableOpen = true;
        if (view == spendingCard) _vm.IsSpendingOpen = true;
        if (view == usableCard || view == spendingCard) await Task.Delay(100);

        // The add button floats over the page; everything else is in the scroll view, scrolled toward the top as
        // far as the page itself scrolls, so a card low on the page stays where it is seen without the tour.
        // A part inside the hero card scrolls the card itself to the top, so the card's top edge stays in view.
        if (view != quickAdd.Fab) await TourGeometry.ScrollNearTopAsync(scroll, view == heroSummary ? heroCard : view);
        return view;
    }

    public Rect TourBoundsOf(View target, View root)
    {
        var bounds = TourGeometry.BoundsIn(target, root);
        if (target != heroSummary) return bounds;
        // The card's own edges above and beside the summary, so the outline looks like the card rather than a box
        // squeezed inside it.
        var card = TourGeometry.BoundsIn(heroCard, root);
        var gap = heroSummary.Parent is VerticalStackLayout stack ? stack.Spacing : 0;
        var bottom = bounds.Bottom + Math.Max(0, gap / 2 - TourLayout.HighlightPadding);
        return new Rect(card.X, card.Y, card.Width, bottom - card.Y);
    }

    readonly MainPageViewModel _vm;
    bool _authenticationRequested;

    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        SystemBarInsets.Apply(this, root);
    }

    // OnAppearing rather than OnNavigatedTo: Shell does not raise NavigatedTo for its first page here.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.IsUnlocked)
        {
            await _vm.LoadAsync();
            return;
        }

        if (_authenticationRequested) return;
        _authenticationRequested = true;
        await Task.Delay(100);
        await _vm.AuthenticationRequest();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Undo.Dismiss();
        quickAdd.Close();
    }

    void OnMonthTapped(object? sender, EventArgs e)
    {
        if (!_vm.IsUnlocked) return;
        StyleMonthPicker();
        datepicker.SelectedDate = SharedProperty.Instance.SelectedDate;
        datepicker.IsOpen = true;
    }

    // The picker's header and columns are not in the visual tree, so DynamicResource does not reach them;
    // take the current token values each time it opens.
    void StyleMonthPicker()
    {
        var surface = new SolidColorBrush(ThemeTokens.Get("Surface"));
        PickerTextStyle Text(string color, string font, double size) =>
            new() { TextColor = ThemeTokens.Get(color), FontFamily = font, FontSize = size };

        datepicker.Background = surface;
        datepicker.HeaderView = new PickerHeaderView
        {
            Height = 56,
            Text = "年月をえらぶ",
            Background = surface,
            DividerColor = ThemeTokens.Get("Rule"),
            TextStyle = Text("Ink", "ZenMaruBold", 18),
        };
        datepicker.ColumnHeaderView = new DatePickerColumnHeaderView
        {
            Height = 40,
            YearHeaderText = "年",
            MonthHeaderText = "月",
            Background = surface,
            DividerColor = ThemeTokens.Get("Rule"),
            TextStyle = Text("InkMuted", "ZenMaruMedium", 14),
        };
        datepicker.TextStyle = Text("Ink", "ZenMaru", 16);
        datepicker.SelectedTextStyle = Text("OnAccent", "ZenMaruBold", 18);
        datepicker.SelectionView = new PickerSelectionView { Background = new SolidColorBrush(ThemeTokens.Get("Accent")), CornerRadius = 22 };
    }

    async void OnMonthPicked(object? sender, Syncfusion.Maui.Picker.DatePickerSelectionChangedEventArgs e)
    {
        if (!datepicker.IsOpen || e.NewValue is not DateTime picked) return;
        await _vm.SelectMonthAsync(picked);
    }
}
