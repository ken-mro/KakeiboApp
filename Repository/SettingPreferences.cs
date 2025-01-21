namespace KakeiboApp.Repository;

public class SettingPreferences
{
	private IPreferences _defaultPreferences;
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
}