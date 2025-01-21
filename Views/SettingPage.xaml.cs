using KakeiboApp.ViewModels;

namespace KakeiboApp.Views;

public partial class SettingPage : ContentPage
{
	public SettingPage(SettingPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}