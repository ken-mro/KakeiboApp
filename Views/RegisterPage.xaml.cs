using KakeiboApp.CustomDataForm;
using KakeiboApp.ViewModels;
using Microsoft.Maui.Handlers;

namespace KakeiboApp.Views;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterPageViewModel vm)
	{
		InitializeComponent();
        dataForm.ItemManager = new DataFormItemManagerEditorExt();
        vm.DataForm = dataForm;
        BindingContext = vm;
        FixButtonFocusingBehavior();
    }

    private void FixButtonFocusingBehavior()
    {
        //https://github.com/dotnet/maui/issues/23901#issuecomment-2429507942
        ButtonHandler.Mapper.AppendToMapping("UnfocusFix", (h, b) =>
        {
            h.PlatformView.FocusableInTouchMode = true;
        });

        ButtonHandler.Mapper.PrependToMapping(nameof(Button.IsFocused), (h, b) =>
        {
            if (b.IsFocused)
            {
                b.Clicked();
            }
        });
    }
}