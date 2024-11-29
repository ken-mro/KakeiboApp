using KakeiboApp.ViewModels;

namespace KakeiboApp.Views;

public partial class DetailPage : ContentPage
{
	public DetailPage(DetailPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}