namespace KakeiboApp.Repository;

public class SettingPreferences
{
	private IPreferences _defaultPreferences;
    private Dictionary<string, AppTheme> _themDictionary = new Dictionary<string, AppTheme>
    {
        { AppTheme.Unspecified.ToString(), AppTheme.Unspecified },
        { AppTheme.Light.ToString(), AppTheme.Light },
        { AppTheme.Dark.ToString(), AppTheme.Dark }
    };

    public SettingPreferences(IPreferences preferences)
	{
        _defaultPreferences = preferences;
    }

    private string _isFingerprintEnabledKey = "IsFingerprintEnabled";

    public bool IsFingerprintEnabled  
    {
        get => _defaultPreferences.Get(_isFingerprintEnabledKey, false);
        set => _defaultPreferences.Set(_isFingerprintEnabledKey, value);
    }

    private string _themeKey = "AppTheme";
    private string _defaultTheme = AppTheme.Unspecified.ToString();

    // Using string instead of AppTheme because IPreferences does not support enum.
    public string Theme
    {
        get => _defaultPreferences.Get(_themeKey, _defaultTheme);
        set
        {
            _defaultPreferences.Set(_themeKey, value);
            if (App.Current is null) return;
            App.Current.UserAppTheme = _themDictionary[value];
        }
    }

    public void SetAppTheme()
    {
        if (App.Current is null) return;
        App.Current.UserAppTheme = _themDictionary[Theme];
    }

    private string _recordsTotalRemainingToThisMonth = "RecordsTotalRemainingToThisMonth";

    public bool RecordsTotalRemainingToThisMonth
    {
        get => _defaultPreferences.Get(_recordsTotalRemainingToThisMonth, false);
        set => _defaultPreferences.Set(_recordsTotalRemainingToThisMonth, value);
    }
}