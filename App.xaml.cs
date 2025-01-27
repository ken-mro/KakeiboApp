using KakeiboApp.Repository;

namespace KakeiboApp;

public partial class App : Application
{
    public App(AppShell appShell, SettingPreferences settingPreferences)
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Constants.SYNCFUSIOHN_LICENSE_KEY);
        InitializeComponent();

        MainPage = appShell;


        settingPreferences.SetAppTheme();

        if (Current is null) return;
        Current.RequestedThemeChanged += (s, a) =>
        {
            settingPreferences.SetAppTheme();
        };
    }
}
