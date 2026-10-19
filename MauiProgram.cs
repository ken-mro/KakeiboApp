using CommunityToolkit.Maui;
using KakeiboApp.Repository;
using KakeiboApp.Services;
using KakeiboApp.ViewModels;
using KakeiboApp.ViewModels.Entries;
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
                .UseMauiCommunityToolkit(options =>
                {
                    // Sheets span the full width at the bottom of the screen.
                    options.SetPopupDefaults(new DefaultPopupSettings { Margin = 0, Padding = 0 });
                })
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
            RemoveInputUnderlines();
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
            builder.Services.AddSingleton<ISpendingItemRepository, SpendingItemRepository>();
            builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();

            builder.Services.AddSingleton<EntryService>();
            builder.Services.AddSingleton<GuideService>();
            builder.Services.AddSingleton(new BackupService(Constants.DataBasePath));

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainPageViewModel>();

            // One list page class serves the 生活費 and 特別出費 tabs and the lists opened from the home.
            builder.Services.AddTransient<EntryListViewModel>();
            builder.Services.AddTransient<EntryListPage>();
            builder.Services.AddSingleton<SpendingListPage>();
            builder.Services.AddSingleton<SpecialExpenseListPage>();

            builder.Services.AddSingleton<SettingPage>();
            builder.Services.AddSingleton<SettingPageViewModel>();

            return builder.Build();
        }

#if ANDROID
        // The design draws inputs as rounded fields, so drop Android's underline.
        static void RemoveInputUnderlines()
        {
            var transparent = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => h.PlatformView.BackgroundTintList = transparent);
            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => h.PlatformView.BackgroundTintList = transparent);
            Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => h.PlatformView.BackgroundTintList = transparent);
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => h.PlatformView.BackgroundTintList = transparent);
        }
#endif
    }
}
