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

            builder.Services.AddSingleton<IMonthlyBudgetDataRepository, MonthlyBudgetDataRepository>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainPageViewModel>();

            builder.Services.AddSingleton<RegisterPage>();
            builder.Services.AddSingleton<RegisterPageViewModel>();

            builder.Services.AddSingleton<DetailPage>();
            builder.Services.AddSingleton<DetailPageViewModel>();

            builder.Services.AddSingleton<ISpendingItemRepository, SpendingItemRepository>();
            builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();

            return builder.Build();
        }
    }
}
