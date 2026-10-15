namespace KakeiboApp.Services;

/// <summary>
/// Copies the Light or Dark value of each colour token in Resources/Styles/Colors.xaml into the plain key
/// (for example InkLight → Ink), so pages can use {DynamicResource Ink} and follow theme changes.
/// </summary>
public static class ThemeTokens
{
    public static readonly string[] Names =
    [
        "Ink", "InkMuted", "Paper", "Surface", "Hero",
        "Accent", "OnAccent", "AccentSoft", "AccentInk",
        "Deficit", "DeficitInk", "DeficitSoft", "Rule", "Track",
        "DeleteSurface", "OnDelete", "Toast", "OnToast", "ToastButton", "OnToastButton", "Scrim",
    ];

    public static void Apply(ResourceDictionary resources, AppTheme theme)
    {
        var suffix = theme == AppTheme.Dark ? "Dark" : "Light";
        foreach (var name in Names)
        {
            if (resources.TryGetValue(name + suffix, out var value))
            {
                resources[name] = value;
            }
        }

        UpdateSystemBars(resources, theme);
    }

    /// <summary>Modal pages reset the status bar; they call this when they appear and disappear.</summary>
    public static void ApplySystemBars()
    {
        if (Application.Current is { } app) UpdateSystemBars(app.Resources, app.RequestedTheme);
    }

    public static Color Get(string name) =>
        Application.Current?.Resources.TryGetValue(name, out var value) == true && value is Color color
            ? color
            : Colors.Transparent;

    static void UpdateSystemBars(ResourceDictionary resources, AppTheme theme)
    {
#if ANDROID || IOS
        if (!resources.TryGetValue("Paper", out var paper) || paper is not Color color) return;
        try
        {
            CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(color);
            CommunityToolkit.Maui.Core.Platform.StatusBar.SetStyle(theme == AppTheme.Dark
                ? CommunityToolkit.Maui.Core.StatusBarStyle.LightContent
                : CommunityToolkit.Maui.Core.StatusBarStyle.DarkContent);
        }
        catch (Exception ex)
        {
            // Before the first window exists there is no activity to colour yet; App.OnStart applies it again.
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
#endif
    }
}
