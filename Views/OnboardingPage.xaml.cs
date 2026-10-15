using KakeiboApp.ViewModels;

namespace KakeiboApp.Views;

public partial class OnboardingPage : ContentPage
{
    readonly OnboardingViewModel _vm;

    public OnboardingPage(OnboardingViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Services.ThemeTokens.ApplySystemBars();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Services.ThemeTokens.ApplySystemBars();
    }

    // The Android back button closes the guide like スキップ, so the version is still recorded.
    protected override bool OnBackButtonPressed()
    {
        _vm.SkipCommand.Execute(null);
        return true;
    }
}
