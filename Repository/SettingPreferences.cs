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
            ApplyTokens();
        }
    }

    public void SetAppTheme()
    {
        if (App.Current is null) return;
        App.Current.UserAppTheme = _themDictionary[Theme];
        ApplyTokens();
    }

    // Setting UserAppTheme does not always raise RequestedThemeChanged, so recolour the tokens here.
    static void ApplyTokens()
    {
        if (App.Current is null) return;
        Services.ThemeTokens.Apply(App.Current.Resources, App.Current.RequestedTheme);
    }

    private string _recordsTotalRemainingToThisMonth = "RecordsTotalRemainingToThisMonth";

    public bool RecordsTotalRemainingToThisMonth
    {
        get => _defaultPreferences.Get(_recordsTotalRemainingToThisMonth, false);
        set => _defaultPreferences.Set(_recordsTotalRemainingToThisMonth, value);
    }

    private string _quickAddOnLeftKey = "QuickAddOnLeft";

    /// <summary>「＋ つける」を左下に置く。未設定は右下。</summary>
    public bool QuickAddOnLeft
    {
        get => _defaultPreferences.Get(_quickAddOnLeftKey, false);
        set => _defaultPreferences.Set(_quickAddOnLeftKey, value);
    }

    private string _lastSeenGuideVersionKey = "LastSeenGuideVersion";

    /// <summary>
    /// 最後に「使い方」を見たアプリのバージョン。1.x にはない値なので、未設定は空文字。
    /// </summary>
    public string LastSeenGuideVersion
    {
        get => _defaultPreferences.Get(_lastSeenGuideVersionKey, string.Empty);
        set => _defaultPreferences.Set(_lastSeenGuideVersionKey, value);
    }
}