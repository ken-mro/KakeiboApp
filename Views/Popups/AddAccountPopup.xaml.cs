using CommunityToolkit.Maui.Views;
using KakeiboApp.ViewModels.Popups;
using Microsoft.Maui.Handlers;

namespace KakeiboApp.Views.Popups;

public partial class AddAccountPopup : Popup
{
    public AddAccountPopup(AddAccountPopupViewModel vm)
	{
		InitializeComponent();
        vm.popup = this;
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