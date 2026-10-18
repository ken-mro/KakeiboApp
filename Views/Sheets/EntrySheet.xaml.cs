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
        // 内容 → 金額 with the keyboard's 次へ; 金額's 完了 puts the keyboard away, uncovering the buttons.
        nameEntry.Completed += (_, _) => amountEntry.Focus();
        foreach (var input in Inputs) input.Focused += (_, _) => _ = RevealFocusedAsync();
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
        foreach (var input in Inputs)
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

    InputView[] Inputs => [nameEntry, amountEntry, fromEntry, memoEditor];

    /// <summary>Room left between a focused field and the top of the keyboard.</summary>
    const double KeyboardGap = 16;

    /// <summary>
    /// The keyboard comes up over the sheet, which does not move. Only when the focused field (the memo, near the
    /// bottom) would be under the keyboard, the fields scroll up inside the sheet just far enough to show it.
    /// </summary>
    async Task RevealFocusedAsync()
    {
#if ANDROID
        // The keyboard is still opening when the field gets the focus.
        await Task.Delay(250);
        if (_keyboardPx <= 0 || Handler?.PlatformView is not Android.Views.View sheet) return;
        var focused = Inputs.FirstOrDefault(i => i.IsFocused);
        if (focused?.Handler?.PlatformView is not Android.Views.View field) return;

        var at = new int[2];
        field.GetLocationInWindow(at);
        var keyboardTop = sheet.RootView!.Height - _keyboardPx;
        var density = DeviceDisplay.Current.MainDisplayInfo.Density;
        var hidden = (at[1] + field.Height - keyboardTop) / density + KeyboardGap;
        if (hidden > 0) await scroll.ScrollToAsync(0, scroll.ScrollY + hidden, true);
#else
        await Task.CompletedTask;
#endif
    }

#if ANDROID
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.PlatformView is not Android.Views.View view) return;
        // So HideKeyboardAsync can park the focus on the sheet itself.
        view.Focusable = true;
        view.FocusableInTouchMode = true;

        // Follow the keyboard: room under the fields for it while it is up, so the memo can be scrolled above it.
        if (view.ViewTreeObserver is not { } observer) return;
        observer.GlobalLayout += (_, _) =>
        {
            var insets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(view);
            var ime = insets?.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.Ime())?.Bottom ?? 0;
            if (ime == _keyboardPx) return;
            _keyboardPx = ime;
            form.Padding = new Thickness(0, 0, 0, ime / DeviceDisplay.Current.MainDisplayInfo.Density);
            if (ime > 0) _ = RevealFocusedAsync();
        };
    }

    int _keyboardPx;

    /// <summary>
    /// While the sheet is open, the keyboard comes up over it instead of resizing the screen (the app's setting
    /// elsewhere, App.xaml.cs), so nothing on the sheet moves.
    /// </summary>
    static void KeyboardOverScreen(bool over)
    {
        var mode = over ? Android.Views.SoftInput.AdjustNothing : Android.Views.SoftInput.AdjustResize;
        Platform.CurrentActivity?.Window?.SetSoftInputMode(mode);
    }
#endif

    /// <summary>Shows the sheet and returns once it is closed. Read <see cref="EntrySheetViewModel.Outcome"/> afterwards.</summary>
    public static async Task<SheetOutcome> ShowAsync(EntrySheetViewModel vm)
    {
        var page = Shell.Current.CurrentPage;
#if ANDROID
        KeyboardOverScreen(true);
#endif
        try
        {
            await page.ShowPopupAsync(new EntrySheet(vm), new PopupOptions
            {
                Shape = new RoundRectangle { CornerRadius = new CornerRadius(32, 32, 0, 0), StrokeThickness = 0 },
                Shadow = null,
                PageOverlayColor = ThemeTokens.Get("Scrim"),
            });
        }
        finally
        {
#if ANDROID
            KeyboardOverScreen(false);
#endif
        }
        return vm.Outcome;
    }
}
