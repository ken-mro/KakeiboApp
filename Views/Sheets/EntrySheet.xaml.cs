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
        vm.FocusFirstField = () => (vm.ShowsNameAndNote ? nameEntry : amountEntry).Focus();
        vm.ShowField = field => _ = ShowFieldAsync(field);
        // 内容 → 金額 with the keyboard's 次へ; 金額's 完了 puts the keyboard away, bringing the buttons back.
        nameEntry.Completed += (_, _) => amountEntry.Focus();
        amountEntry.Completed += (_, _) => _ = HideKeyboardAsync();
        // A tap outside the text fields, or on a kind tile, also puts the keyboard away.
        form.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => _ = HideKeyboardAsync()) });
        foreach (var option in vm.Categories)
        {
            option.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(CategoryOption.IsSelected) && option.IsSelected) _ = HideKeyboardAsync();
            };
        }

        if (!vm.CanContinue && !vm.CanDelete)
        {
            Grid.SetColumn(saveButton, 0);
            Grid.SetColumnSpan(saveButton, 2);
        }

        FitScrollArea(0);

        Opened += async (_, _) =>
        {
            // A sheet that is only an amount (切り崩し, 予算) opens the number pad right away. The others start
            // with 日にち and 種類, which are tapped, so the keyboard waits until a text field is tapped.
            if (!vm.ShowsNameAndNote && !vm.ShowsDate)
            {
                await Task.Delay(150);
                amountEntry.Focus();
            }
        };
    }

    /// <summary>Puts the keyboard away from whichever text field has it.</summary>
    async Task HideKeyboardAsync()
    {
        foreach (var input in new InputView[] { nameEntry, amountEntry, fromEntry, memoEditor })
        {
            if (!input.IsFocused) continue;
            try
            {
                await input.HideSoftInputAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            input.Unfocus();
        }
#if ANDROID
        // Android hands the focus to the next text field, which then shows its cursor; give it to the sheet instead.
        (Handler?.PlatformView as Android.Views.View)?.RequestFocus();
#endif
    }

    /// <summary>After 登録 found a problem: the keyboard away and the first field to fix in view.</summary>
    async Task ShowFieldAsync(SheetField field)
    {
        await HideKeyboardAsync();
        View target = field switch
        {
            SheetField.Date => dateField,
            SheetField.Category => categoryField,
            _ => amountField,
        };
        await scroll.ScrollToAsync(target, ScrollToPosition.MakeVisible, true);
    }

    // Leave room above the sheet so it never covers the whole screen, also at 200% text size and with the
    // keyboard up. The handle, title and buttons take about 240, the fields scroll inside the rest; while the
    // keyboard is up the buttons are hidden, so the fields also get theirs.
    void FitScrollArea(double keyboardHeight)
    {
        var display = DeviceDisplay.Current.MainDisplayInfo;
        if (display.Density <= 0) return;
        var screen = display.Height / display.Density;
        var keyboardUp = keyboardHeight >= 1;
        buttons.IsVisible = !keyboardUp;
        var chrome = keyboardUp ? 140 : 240;
        scroll.MaximumHeightRequest = Math.Max(120, Math.Min(screen * 0.9, screen - keyboardHeight - 48) - chrome);
    }

#if ANDROID
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.PlatformView is not Android.Views.View view) return;
        // So HideKeyboardAsync can park the focus on the sheet itself.
        view.Focusable = true;
        view.FocusableInTouchMode = true;

        // Fit the fields above the keyboard while it is shown, and hide the buttons until it goes away.
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
