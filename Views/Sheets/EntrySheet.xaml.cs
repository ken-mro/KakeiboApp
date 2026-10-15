using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using KakeiboApp.Services;
using KakeiboApp.ViewModels.Entries;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Sheets;

public partial class EntrySheet : Popup
{
    readonly EntrySheetViewModel _vm;

    public EntrySheet(EntrySheetViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        vm.CloseAsync = () => CloseAsync();
        vm.FocusAmount = () => amountEntry.Focus();

        if (!vm.CanContinue && !vm.CanDelete)
        {
            Grid.SetColumn(saveButton, 0);
            Grid.SetColumnSpan(saveButton, 2);
        }

        // Leave room above the sheet so it never covers the whole screen, also at 200% text size.
        var display = DeviceDisplay.Current.MainDisplayInfo;
        if (display.Density > 0)
        {
            // The handle, title and buttons take about 220; the fields scroll inside the rest.
            scroll.MaximumHeightRequest = Math.Max(200, display.Height / display.Density * 0.9 - 220);
        }

        Opened += async (_, _) =>
        {
            // Adding starts with the amount, so open the number pad right away.
            if (vm.CanContinue)
            {
                await Task.Delay(150);
                amountEntry.Focus();
            }
        };
    }

    /// <summary>Shows the sheet and returns once it is closed. Read <see cref="EntrySheetViewModel.Outcome"/> afterwards.</summary>
    public static async Task<SheetOutcome> ShowAsync(EntrySheetViewModel vm)
    {
        var page = Shell.Current.CurrentPage;
        await page.ShowPopupAsync(new EntrySheet(vm), new PopupOptions
        {
            Shape = new RoundRectangle { CornerRadius = new CornerRadius(32, 32, 0, 0), StrokeThickness = 0 },
            Shadow = null,
            PageOverlayColor = ThemeTokens.Get("Scrim"),
        });
        return vm.Outcome;
    }
}
