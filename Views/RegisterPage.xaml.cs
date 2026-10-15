using KakeiboApp.CustomDataForm;
using KakeiboApp.ViewModels;

namespace KakeiboApp.Views;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterPageViewModel vm)
	{
		InitializeComponent();
        dataForm.ItemManager = new DataFormItemManagerEditorExt();
        vm.DataForm = dataForm;
        vm.RegisterButton = registerButton;
        BindingContext = vm;
    }
}