using CommunityToolkit.Maui.Views;
using KakeiboApp.ViewModels.Popups;

namespace KakeiboApp.Views.Popups;

public partial class AddAccountPopup : Popup
{
    public AddAccountPopup(AddAccountPopupViewModel vm)
	{
		InitializeComponent();
        vm.popup = this;
        BindingContext = vm;
    }
}