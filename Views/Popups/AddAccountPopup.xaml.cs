using CommunityToolkit.Maui.Views;
using KakeiboApp.CustomDataForm;
using KakeiboApp.ViewModels.Popups;

namespace KakeiboApp.Views.Popups;

public partial class AddAccountPopup : Popup
{
    public AddAccountPopup(AddAccountPopupViewModel vm)
	{
		InitializeComponent();
        vm.DataForm = dataForm;
        dataForm.ItemManager = new DataFormItemManagerEditorExt();
        BindingContext = vm;
    }
}