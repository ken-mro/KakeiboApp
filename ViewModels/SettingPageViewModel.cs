using CommunityToolkit.Mvvm.ComponentModel;
using KakeiboApp.Repository;

namespace KakeiboApp.ViewModels;

public partial class SettingPageViewModel : BaseViewModel
{
    private readonly SettingPreferences _settingPreferences;
    public SettingPageViewModel(SettingPreferences settingPreferences)
    {
        _settingPreferences = settingPreferences;
    }

    public bool IsFingerprintEnabled
    {
        get => _settingPreferences.IsFingerprintEnabled;
        set
        {
            _settingPreferences.IsFingerprintEnabled = value;
            OnPropertyChanged();
        }
    }
}
