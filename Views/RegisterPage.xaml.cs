using KakeiboApp.ViewModels;

namespace KakeiboApp.Views;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}