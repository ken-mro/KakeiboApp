using KakeiboApp.Repository;

namespace KakeiboApp;

public partial class App : Application
{
    readonly AppShell _appShell;

    public App(AppShell appShell, SettingPreferences settingPreferences)
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Constants.SYNCFUSIOHN_LICENSE_KEY);
        InitializeComponent();

        _appShell = appShell;


        settingPreferences.SetAppTheme();

        if (Current is null) return;
        Current.RequestedThemeChanged += (s, a) =>
        {
            settingPreferences.SetAppTheme();
        };
    }

    protected override Window CreateWindow(IActivationState? activationState) => new(_appShell);
}
