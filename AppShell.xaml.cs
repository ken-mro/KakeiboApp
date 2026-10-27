using KakeiboApp.Views;

namespace KakeiboApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(EntryListPage.Route, typeof(EntryListPage));
        Routing.RegisterRoute(BudgetsPage.Route, typeof(BudgetsPage));
        SetTabVisibility(false);
    }

    /// <summary>Until authentication succeeds only the home tab can be opened.</summary>
    public void SetTabVisibility(bool isVisible)
    {
        list.IsVisible = isVisible;
        specialExpense.IsVisible = isVisible;
        setting.IsVisible = isVisible;
    }
}
