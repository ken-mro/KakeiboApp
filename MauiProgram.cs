using CommunityToolkit.Maui;
using KakeiboApp.Repository;
using KakeiboApp.Services;
using KakeiboApp.ViewModels;
using KakeiboApp.Views;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Biometric;
using Syncfusion.Maui.Core.Hosting;

namespace KakeiboApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionCore()
                .ConfigureFonts(fonts =>
                {
                    // Zen Maru Gothic (SIL Open Font License, Resources/Fonts/licenses).
                    fonts.AddFont("ZenMaruGothic-Regular.ttf", "ZenMaru");
                    fonts.AddFont("ZenMaruGothic-Medium.ttf", "ZenMaruMedium");
                    fonts.AddFont("ZenMaruGothic-Bold.ttf", "ZenMaruBold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
#if ANDROID
            FixButtonFocusingBehavior();
#endif
            builder.Services.AddSingleton<AppShell>();

            builder.Services.AddSingleton<IBiometric>(BiometricAuthenticationService.Default);
            builder.Services.AddSingleton(Preferences.Default);
            builder.Services.AddSingleton<SettingPreferences>();

            builder.Services.AddSingleton<EntryService>();
            builder.Services.AddSingleton<GuideService>();

            builder.Services.AddSingleton<IMonthlyIncomeDataRepository, MonthlyIncomeDataRepository>();
            builder.Services.AddSingleton<IMonthlySavingDataRepository, MonthlySavingDataRepository>();
            builder.Services.AddSingleton<IMonthlyFixedCostDataRepository, MonthlyFixedCostDataRepository>();
            builder.Services.AddSingleton<IMonthlyBudgetDataRepository, MonthlyBudgetDataRepository>();
            builder.Services.AddSingleton<ISpecialExpenseDataRepository, SpecialExpenseDataRepository>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainPageViewModel>();

            builder.Services.AddSingleton<RegisterPage>();
            builder.Services.AddSingleton<RegisterPageViewModel>();

            builder.Services.AddSingleton<DetailPage>();
            builder.Services.AddSingleton<DetailPageViewModel>();

            builder.Services.AddSingleton<SettingPage>();
            builder.Services.AddSingleton<SettingPageViewModel>();

            builder.Services.AddSingleton<SpecialExpenseDetailPage>();
            builder.Services.AddSingleton<SpecialExpenseDetailPageViewModel>();

            builder.Services.AddSingleton<ISpendingItemRepository, SpendingItemRepository>();
            builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();

            return builder.Build();
        }

#if ANDROID
        // A button tapped while an editor has focus only takes the focus on the first tap.
        // Registered once here so every Button gets it, instead of once per page/popup instance.
        static void FixButtonFocusingBehavior()
        {
            //https://github.com/dotnet/maui/issues/23901#issuecomment-2429507942
            Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("UnfocusFix", (h, b) =>
            {
                h.PlatformView.FocusableInTouchMode = true;
            });

            Microsoft.Maui.Handlers.ButtonHandler.Mapper.PrependToMapping(nameof(Button.IsFocused), (h, b) =>
            {
                if (b.IsFocused)
                {
                    b.Clicked();
                    b.IsFocused = false;
                }
            });
        }
#endif
    }
}
