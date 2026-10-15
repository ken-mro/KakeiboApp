using KakeiboApp.Models;
using KakeiboApp.ViewModels;

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
        datepicker.SelectedDate = SharedProperty.Instance.SelectedDate;
        datepicker.IsOpen = true;
    }

    async void OnMonthPicked(object? sender, Syncfusion.Maui.Picker.DatePickerSelectionChangedEventArgs e)
    {
        if (!datepicker.IsOpen || e.NewValue is not DateTime picked) return;
        await _vm.SelectMonthAsync(picked);
    }
}
