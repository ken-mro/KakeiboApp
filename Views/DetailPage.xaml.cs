using KakeiboApp.ViewModels;

namespace KakeiboApp.Views;

public partial class DetailPage : ContentPage
{
	public DetailPage(DetailPageViewModel vm)
	{
		InitializeComponent();
        vm.DataGrid = dataGrid;
        BindingContext = vm;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        datepicker.IsOpen = true;
    }
}