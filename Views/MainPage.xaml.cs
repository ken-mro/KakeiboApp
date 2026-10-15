using KakeiboApp.Models;
using KakeiboApp.Services;
using KakeiboApp.ViewModels;
using Syncfusion.Maui.Picker;

namespace KakeiboApp.Views;

public partial class MainPage : ContentPage
{
    readonly MainPageViewModel _vm;
    bool _authenticationRequested;

    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
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
