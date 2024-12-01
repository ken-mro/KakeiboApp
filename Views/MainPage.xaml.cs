using KakeiboApp.ViewModels;

namespace KakeiboApp.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        datepicker.IsOpen = true;
    }
}
