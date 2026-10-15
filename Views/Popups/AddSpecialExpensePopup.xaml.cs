using CommunityToolkit.Maui.Views;
using KakeiboApp.CustomDataForm;
using KakeiboApp.ViewModels.Popups;
using Syncfusion.Maui.DataForm;

namespace KakeiboApp.Views.Popups;

public partial class AddSpecialExpensePopup : Popup
{
	public AddSpecialExpensePopup(AddSpecialExpensePopupViewModel vm)
	{
        InitializeComponent();
        vm.DataForm = dataForm;
        dataForm.ItemManager = new DataFormItemManagerEditorExt();
        BindingContext = vm;
    }
}