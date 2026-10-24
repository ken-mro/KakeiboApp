using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using KakeiboApp.Models;
using KakeiboApp.Services;
using KakeiboApp.ViewModels.Entries;
using KakeiboApp.Views.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Sheets;

public partial class EntrySheet : Popup
{
    readonly EntrySheetViewModel _vm;

    public EntrySheet(EntrySheetViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        // The popup itself is clear: the rounded sheet and the tab bar under it are drawn by the XAML.
        BackgroundColor = Colors.Transparent;
        vm.CloseAsync = CloseCleanlyAsync;
        // The tab bar under the sheet works: a tab closes the sheet, asking first if something is typed and not saved.
        tabBar.Current = FloatingTabBar.ShownRoute;
        tabBar.BeforeSwitch = async _ =>
        {
            if (vm.HasUnsavedInput && !await Shell.Current.DisplayAlertAsync(
                    "入力中の内容は保存されていません", "シートを閉じて移動しますか？", "移動する", "やめる"))
            {
                return false;
            }
            await CloseCleanlyAsync();
            return true;
        };
        vm.ShowField = field => _ = ShowFieldAsync(field);
        // 内容 → 金額 with the keyboard's 次へ; 金額's 完了 puts the keyboard away, uncovering the buttons.
        nameEntry.Completed += (_, _) => amountEntry.Focus();
        amountEntry.Completed += (_, _) => _ = HideKeyboardAsync();
        // A tap outside the text fields, or opening the 種類 dropdown, also puts the keyboard away.
        // On the whole sheet, the title too: the keyboard covers the lower part, so the top is where to tap.
        root.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => _ = HideKeyboardAsync()) });
        categoryPicker.DropdownOpening += (_, _) => _ = HideKeyboardAsync();

        if (vm.Kind == EntryKind.SavingBalance) AsShortSheet();

        if (!vm.CanDelete)
        {
            Grid.SetColumn(saveButton, 0);
            Grid.SetColumnSpan(saveButton, 2);
        }

#if ANDROID
        // The back key closes the sheet the same way; while the keyboard is up, Android's back puts it away first.
        var back = new BackKey(() => _ = CloseCleanlyAsync());
        Opened += (_, _) => (Platform.CurrentActivity as AndroidX.Activity.ComponentActivity)?.OnBackPressedDispatcher.AddCallback(back);
        Closed += (_, _) => back.Remove();
#endif
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

    /// <summary>
    /// 切り崩し is one amount: where the other sheets start, at the top, but only as tall as its title, amount and buttons,
    /// with the tab bar in its place below. The keyboard comes up under it, the amount and 保存 both in view.
    /// </summary>
    void AsShortSheet()
    {
        // The popup is sized to its content and centred. A clear spacer asks for all the room there is, as the
        // full sheets do, so the frame fills the popup's area and the sheet sits at its top. Taps on it go
        // through to the popup, which closes, as around the other sheets.
        frame.Insert(0, new BoxView { Color = Colors.Transparent, HeightRequest = 10000, InputTransparent = true });
        sheet.VerticalOptions = LayoutOptions.Start;
        root.VerticalOptions = LayoutOptions.Start;
        root.RowDefinitions[2].Height = GridLength.Auto;
        scroll.VerticalOptions = LayoutOptions.Start;
    }

    /// <summary>Puts the keyboard away from whichever text field has it.</summary>
    async Task HideKeyboardAsync()
    {
#if ANDROID
        // The keyboard goes while the field still has the focus, and only once it is down does the sheet take the
        // focus. Moved first, the focus went to the next text field (内容 or メモ after 金額), or to the sheet, and the
        // keyboard turned into its letters for a moment as it went down.
        if (Handler?.PlatformView is Android.Views.View sheetView)
        {
            var keyboard = sheetView.Context?.GetSystemService(Android.Content.Context.InputMethodService)
                as Android.Views.InputMethods.InputMethodManager;
            keyboard?.HideSoftInputFromWindow(sheetView.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
            await KeyboardDownAsync();
            sheetView.RequestFocus();
            return;
        }
#endif
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

    bool _closing;

    /// <summary>
    /// Closes the sheet with the keyboard already down. Closed with the keyboard up, the sheet went at once and
    /// the keyboard was left over the page for a moment before Android put it away.
    /// </summary>
    async Task CloseCleanlyAsync()
    {
        if (_closing) return;
        _closing = true;
        // On Android it returns once the keyboard is down.
        await HideKeyboardAsync();
        await CloseAsync();
    }

#if ANDROID
    /// <summary>Waits while the keyboard slides down, but not forever.</summary>
    static async Task KeyboardDownAsync()
    {
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        for (var waited = 0; decor is not null && waited < 600; waited += 20)
        {
            var insets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(decor);
            if (insets is null || !insets.IsVisible(AndroidX.Core.View.WindowInsetsCompat.Type.Ime())) return;
            await Task.Delay(20);
        }
    }

    sealed class BackKey(Action onBack) : AndroidX.Activity.OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed() => onBack();
    }
#endif

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
        // Focused after the keyboard's 完了 (not in touch mode), Android would grey the whole sheet as the focused view.
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) view.DefaultFocusHighlightEnabled = false;
    }

    /// <summary>
    /// While the sheet is open, the keyboard comes up over it instead of resizing the screen (the app's setting
    /// elsewhere, App.xaml.cs), as on the registration page before the redesign: the sheet stays put, and only
    /// when the field being typed in (the memo, near the bottom) would be under the keyboard does Android pan
    /// the whole screen up just far enough to show it.
    /// </summary>
    static void KeyboardOverScreen(bool over)
    {
        var window = Platform.CurrentActivity?.Window;
        var mode = over ? Android.Views.SoftInput.AdjustPan : Android.Views.SoftInput.AdjustResize;
        if (window is null || window.Attributes?.SoftInputMode == mode) return;
        window.SetSoftInputMode(mode);
    }
#endif

    /// <summary>Shows the sheet and returns once it is closed. Read <see cref="EntrySheetViewModel.Outcome"/> afterwards.</summary>
    /// <summary>The sheet for a new entry of <paramref name="kind"/> in the selected month, from any page.</summary>
    public static async Task<SheetOutcome> AddAsync(EntryKind kind)
    {
        var service = IPlatformApplication.Current!.Services.GetRequiredService<EntryService>();
        var vm = new EntrySheetViewModel(service, kind, SharedProperty.Instance.SelectedDate, null, await service.GetCategoriesAsync());
        return await ShowAsync(vm);
    }

    public static async Task<SheetOutcome> ShowAsync(EntrySheetViewModel vm)
    {
        var page = Shell.Current.CurrentPage;
#if ANDROID
        KeyboardOverScreen(true);
#endif
        var sheet = new EntrySheet(vm);
#if ANDROID
        // The sheet's own window takes the mode as it opens; the page's goes back at once, while the sheet has the
        // focus. Put back after the sheet closed, it was changed on the window just given the focus again, and some
        // keyboards (the Japanese 12-key one) came up for a moment over the page.
        sheet.Opened += (_, _) => KeyboardOverScreen(false);
#endif
        try
        {
            await page.ShowPopupAsync(sheet, new PopupOptions
            {
                // Clear and square: the sheet inside draws its own rounded shape, and the tab bar sits under it.
                Shape = new RoundRectangle { CornerRadius = new CornerRadius(0), StrokeThickness = 0 },
                Shadow = null,
                PageOverlayColor = ThemeTokens.Get("Scrim"),
            });
        }
        finally
        {
#if ANDROID
            // Only if the sheet closed before it opened; otherwise the page's mode is already back.
            KeyboardOverScreen(false);
#endif
        }
        return vm.Outcome;
    }
}
