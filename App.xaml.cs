using KakeiboApp.Repository;

namespace KakeiboApp;

public partial class App : Application
{
    public App()
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Constants.SYNCFUSIOHN_LICENSE_KEY);
        InitializeComponent();

        MainPage = new AppShell();
    }
}
