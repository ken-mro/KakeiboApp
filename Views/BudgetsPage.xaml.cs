using KakeiboApp.ViewModels;
using KakeiboApp.Views.Controls;

namespace KakeiboApp.Views;

/// <summary>予算をまとめて決める, opened from the home's 生活費 with the route <see cref="Route"/>.</summary>
public partial class BudgetsPage : ContentPage
{
    public const string Route = "budgets";

    readonly BudgetsViewModel _vm;

    public BudgetsPage(BudgetsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        SystemBarInsets.Apply(this, root);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    /// <summary>The back key asks first, as ‹ does, when there are changes not saved.</summary>
    protected override bool OnBackButtonPressed()
    {
        _ = _vm.BackAsync();
        return true;
    }
}
