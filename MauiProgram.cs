using CommunityToolkit.Maui;
using KakeiboApp.Repository;
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
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<AppShell>();

            builder.Services.AddSingleton<IBiometric>(BiometricAuthenticationService.Default);
            builder.Services.AddSingleton(Preferences.Default);
            builder.Services.AddSingleton<SettingPreferences>();

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
    }
}
