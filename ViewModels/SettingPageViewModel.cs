using CommunityToolkit.Mvvm.ComponentModel;
using KakeiboApp.Repository;
using System.Collections.ObjectModel;

namespace KakeiboApp.ViewModels;

public partial class SettingPageViewModel : BaseViewModel
{
    private Dictionary<string, AppTheme> _themDictionary = new Dictionary<string, AppTheme>
    {
        { "�V�X�e��", AppTheme.Unspecified },
        { "���C�g", AppTheme.Light },
        { "�_�[�N", AppTheme.Dark }
    };

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

    public string SelectedTheme
    {
        get => _themDictionary.FirstOrDefault(x => x.Value.ToString().Equals(_settingPreferences.Theme)).Key;
        set
        {
            _settingPreferences.Theme = _themDictionary[value].ToString();
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    ObservableCollection<string> _themes = ["�V�X�e��", "���C�g", "�_�[�N"];

    public bool RecordsTotalRemainingToThisMonth
    {
        get => _settingPreferences.RecordsTotalRemainingToThisMonth;
        set
        {
            _settingPreferences.RecordsTotalRemainingToThisMonth = value;
            OnPropertyChanged();
        }
    }
}
