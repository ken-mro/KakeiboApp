using KakeiboApp.Repository;
using KakeiboApp.ViewModels;
using KakeiboApp.Views;

namespace KakeiboApp.Services;

/// <summary>
/// Shows the in-app guide (使い方) on the first launch and the first launch after an update, and on
/// request from 設定 › 使い方. Closing it, by スキップ or はじめる, records the version that showed it.
/// </summary>
public class GuideService(SettingPreferences settings)
{
    bool _isShowing;

    public static string CurrentVersion => AppInfo.Current.VersionString;

    public async Task ShowIfNeededAsync()
    {
        if (OnboardingPolicy.ShouldShow(settings.LastSeenGuideVersion, CurrentVersion))
        {
            await ShowAsync();
        }
    }

    public async Task ShowAsync()
    {
        if (_isShowing) return;
        _isShowing = true;

        OnboardingPage? page = null;
        page = new OnboardingPage(new OnboardingViewModel(async () =>
        {
            settings.LastSeenGuideVersion = CurrentVersion;
            _isShowing = false;
            await page!.Navigation.PopModalAsync();
        }));
        await Shell.Current.Navigation.PushModalAsync(page);
    }
}
