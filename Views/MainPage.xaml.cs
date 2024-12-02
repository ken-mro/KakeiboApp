using KakeiboApp.ViewModels;

namespace KakeiboApp.Views;

public partial class MainPage : ContentPage
{
    readonly MainPageViewModel _vm;
    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (_vm.IsAuthenticated) return;

        await Task.Delay(100);
        await _vm.AuthenticationRequest();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        datepicker.IsOpen = true;
    }
}
