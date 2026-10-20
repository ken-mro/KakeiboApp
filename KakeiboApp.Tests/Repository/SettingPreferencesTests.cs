using KakeiboApp.Repository;

namespace KakeiboApp.Tests.Repository;

public class SettingPreferencesTests
{
    sealed class InMemoryPreferences : IPreferences
    {
        readonly Dictionary<string, object?> _values = [];

        public bool ContainsKey(string key, string? sharedName = null) => _values.ContainsKey(key);
        public void Remove(string key, string? sharedName = null) => _values.Remove(key);
        public void Clear(string? sharedName = null) => _values.Clear();
        public void Set<T>(string key, T value, string? sharedName = null) => _values[key] = value;
        public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
            _values.TryGetValue(key, out var value) ? (T)value! : defaultValue;
    }

    [Fact]
    public void Defaults_AreOffAndSystemTheme()
    {
        var settings = new SettingPreferences(new InMemoryPreferences());

        Assert.False(settings.IsFingerprintEnabled);
        Assert.False(settings.RecordsTotalRemainingToThisMonth);
        Assert.Equal("Unspecified", settings.Theme);
        Assert.False(settings.QuickAddOnLeft);
    }

    [Fact]
    public void Values_AreStoredInPreferences()
    {
        var preferences = new InMemoryPreferences();
        var settings = new SettingPreferences(preferences)
        {
            IsFingerprintEnabled = true,
            RecordsTotalRemainingToThisMonth = true,
            Theme = "Dark",
            QuickAddOnLeft = true,
        };

        var reloaded = new SettingPreferences(preferences);

        Assert.True(reloaded.QuickAddOnLeft);
        Assert.True(reloaded.IsFingerprintEnabled);
        Assert.True(reloaded.RecordsTotalRemainingToThisMonth);
        Assert.Equal("Dark", reloaded.Theme);
    }

    [Fact]
    public void Values_UseTheExistingPreferenceKeys()
    {
        // Changing these keys would silently reset users' settings.
        var preferences = new InMemoryPreferences();
        _ = new SettingPreferences(preferences)
        {
            IsFingerprintEnabled = true,
            RecordsTotalRemainingToThisMonth = true,
            Theme = "Light",
        };

        Assert.True(preferences.Get("IsFingerprintEnabled", false));
        Assert.True(preferences.Get("RecordsTotalRemainingToThisMonth", false));
        Assert.Equal("Light", preferences.Get("AppTheme", string.Empty));
    }

    [Fact]
    public void LastSeenGuideVersion_IsEmptyUntilTheGuideIsSeen()
    {
        // Version 1.x never wrote this key, so updated users start with an empty value and see the guide.
        var preferences = new InMemoryPreferences();
        var settings = new SettingPreferences(preferences);

        Assert.Equal(string.Empty, settings.LastSeenGuideVersion);

        settings.LastSeenGuideVersion = "2.0";

        Assert.Equal("2.0", preferences.Get("LastSeenGuideVersion", string.Empty));
    }
}
