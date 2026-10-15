using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Repository;
using KakeiboApp.Services;

namespace KakeiboApp.ViewModels;

/// <summary>
/// A choice of the テーマ setting.
/// </summary>
public partial class ThemeOption(string label, AppTheme theme) : ObservableObject
{
    public string Label { get; } = label;
    public AppTheme Theme { get; } = theme;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    bool _isSelected;

    public string Description => IsSelected ? $"{Label}、選択中" : Label;
}

public partial class SettingPageViewModel : BaseViewModel
{
    private readonly SettingPreferences _settingPreferences;
    private readonly GuideService _guide;

    public SettingPageViewModel(SettingPreferences settingPreferences, GuideService guide)
    {
        _settingPreferences = settingPreferences;
        _guide = guide;
        Themes =
        [
            new ThemeOption("システム", AppTheme.Unspecified),
            new ThemeOption("ライト", AppTheme.Light),
            new ThemeOption("ダーク", AppTheme.Dark),
        ];
        MarkSelectedTheme();
    }

    public IReadOnlyList<ThemeOption> Themes { get; }

    public bool IsFingerprintEnabled
    {
        get => _settingPreferences.IsFingerprintEnabled;
        set
        {
            _settingPreferences.IsFingerprintEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool RecordsTotalRemainingToThisMonth
    {
        get => _settingPreferences.RecordsTotalRemainingToThisMonth;
        set
        {
            _settingPreferences.RecordsTotalRemainingToThisMonth = value;
            OnPropertyChanged();
        }
    }

    public string VersionText => $"バージョン {GuideService.CurrentVersion}";

    [RelayCommand]
    void SelectTheme(ThemeOption option)
    {
        // Stored as the AppTheme name, as before ("Unspecified", "Light", "Dark").
        _settingPreferences.Theme = option.Theme.ToString();
        MarkSelectedTheme();
    }

    void MarkSelectedTheme()
    {
        foreach (var option in Themes)
        {
            option.IsSelected = option.Theme.ToString() == _settingPreferences.Theme;
        }
    }

    [RelayCommand]
    Task OpenGuideAsync() => _guide.ShowAsync();
}
