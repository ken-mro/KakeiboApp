using KakeiboApp.Repository;
using KakeiboApp.Services;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

namespace KakeiboApp;

public partial class App : Microsoft.Maui.Controls.Application
{
    readonly AppShell _appShell;

    public App(AppShell appShell, SettingPreferences settingPreferences)
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Constants.SYNCFUSIOHN_LICENSE_KEY);
        InitializeComponent();

        _appShell = appShell;

        // Sheets slide up from the bottom; resize the window so the keyboard does not cover them.
        On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);

        // Applies the stored theme and its colour tokens, also when the system theme changes.
        settingPreferences.SetAppTheme();
        RequestedThemeChanged += (s, a) => settingPreferences.SetAppTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState) => new(_appShell);

    protected override void OnStart()
    {
        base.OnStart();
        // The status bar can only be coloured once the window exists.
        ThemeTokens.Apply(Resources, RequestedTheme);
    }
}
