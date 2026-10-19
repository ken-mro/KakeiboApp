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
        // Popup v2 reads the background before a DynamicResource is resolved, so set the token value here.
        BackgroundColor = ThemeTokens.Get("Surface");
        vm.CloseAsync = () => CloseAsync();
        vm.FocusFirstField = () => (vm.ShowsNameAndNote ? nameEntry : amountEntry).Focus();
        vm.ShowField = field => _ = ShowFieldAsync(field);
        // 内容 → 金額 with the keyboard's 次へ; 金額's 完了 puts the keyboard away, uncovering the buttons.
        nameEntry.Completed += (_, _) => amountEntry.Focus();
        amountEntry.Completed += (_, _) => _ = HideKeyboardAsync();
        // A tap outside the text fields, or opening the 種類 dropdown, also puts the keyboard away.
        // On the whole sheet, the title too: the keyboard covers the lower part, so the top is where to tap.
        root.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => _ = HideKeyboardAsync()) });
        categoryPicker.DropdownOpening += (_, _) => _ = HideKeyboardAsync();

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

#if ANDROID
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.PlatformView is not Android.Views.View view) return;
        // So HideKeyboardAsync can park the focus on the sheet itself.
        view.Focusable = true;
        view.FocusableInTouchMode = true;
    }

    /// <summary>
    /// While the sheet is open, the keyboard comes up over it instead of resizing the screen (the app's setting
    /// elsewhere, App.xaml.cs), as on the registration page before the redesign: the sheet stays put, and only
    /// when the field being typed in (the memo, near the bottom) would be under the keyboard does Android pan
    /// the whole screen up just far enough to show it.
    /// </summary>
    static void KeyboardOverScreen(bool over)
    {
        var mode = over ? Android.Views.SoftInput.AdjustPan : Android.Views.SoftInput.AdjustResize;
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
