namespace KakeiboApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        SetTabVisibility(false);
    }

    public void SetTabVisibility(bool isVisible)
    {
        register.IsVisible = isVisible;
        detail.IsVisible = isVisible;
        specialExpense.IsVisible = isVisible;
        setting.IsVisible = isVisible;
    }
}
