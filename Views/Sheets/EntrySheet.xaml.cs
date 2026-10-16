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
        categoryGrid.ChildAdded += (_, e) =>
        {
            var index = categoryGrid.Children.IndexOf((IView)e.Element);
            while (categoryGrid.RowDefinitions.Count <= index / 3) categoryGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Grid.SetRow((BindableObject)e.Element, index / 3);
            Grid.SetColumn((BindableObject)e.Element, index % 3);
        };
        BindingContext = _vm = vm;
        // Popup v2 reads the background before a DynamicResource is resolved, so set the token value here.
        BackgroundColor = ThemeTokens.Get("Surface");
        vm.CloseAsync = () => CloseAsync();
        vm.FocusAmount = () => amountEntry.Focus();

        if (!vm.CanContinue && !vm.CanDelete)
        {
            Grid.SetColumn(saveButton, 0);
            Grid.SetColumnSpan(saveButton, 2);
        }

        FitScrollArea(0);

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

    // Leave room above the sheet so it never covers the whole screen, also at 200% text size and with the
    // keyboard up: the handle, title and buttons take about 240, the fields scroll inside the rest.
    void FitScrollArea(double keyboardHeight)
    {
        var display = DeviceDisplay.Current.MainDisplayInfo;
        if (display.Density <= 0) return;
        var screen = display.Height / display.Density;
        scroll.MaximumHeightRequest = Math.Max(120, Math.Min(screen * 0.9, screen - keyboardHeight - 48) - 240);
    }

#if ANDROID
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.PlatformView is not Android.Views.View view) return;

        // Shrink the fields while the keyboard is shown, so the title and the buttons stay on screen.
        if (view.ViewTreeObserver is not { } observer) return;
        observer.GlobalLayout += (_, _) =>
        {
            var insets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(view);
            var ime = insets?.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.Ime())?.Bottom ?? 0;
            var keyboard = ime / DeviceDisplay.Current.MainDisplayInfo.Density;
            if (Math.Abs(keyboard - _keyboardHeight) < 1) return;
            _keyboardHeight = keyboard;
            FitScrollArea(keyboard);
        };
    }

    double _keyboardHeight;
#endif

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
