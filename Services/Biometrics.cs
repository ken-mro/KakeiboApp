namespace KakeiboApp.Services;

/// <summary>
/// What the device unlocks with, for the words and icon of the lock: a fingerprint on Android; Face ID or Touch ID
/// on an iPhone or iPad, as Apple names them.
/// </summary>
public static class Biometrics
{
    /// <summary>「指紋認証」「Face ID」: the setting and the lock screen.</summary>
    public static string Name { get; }

    /// <summary>「指紋」「Face ID」: what the user authenticates with (「指紋で認証します」).</summary>
    public static string Means { get; }

    public static string Glyph { get; }

    public static string SettingCaption => $"起動するときに{Means}で認証します";

    public static string LockTitle => $"{Name}でひらきます";

    public static string LockBody => $"家計簿を見るには、{Means}で認証してください。";

    static Biometrics()
    {
        (Name, Means, Glyph) = ("指紋認証", "指紋", Icons.Fingerprint);
#if IOS || MACCATALYST
        // BiometryType is set once CanEvaluatePolicy has run, enrolled or not.
        using var context = new LocalAuthentication.LAContext();
        context.CanEvaluatePolicy(LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _);
        (Name, Means, Glyph) = context.BiometryType switch
        {
            LocalAuthentication.LABiometryType.FaceId => ("Face ID", "Face ID", Icons.Face),
            LocalAuthentication.LABiometryType.TouchId => ("Touch ID", "Touch ID", Icons.Fingerprint),
            _ => ("生体認証", "生体認証", Icons.Fingerprint),
        };
#endif
    }
}
