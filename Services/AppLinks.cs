namespace KakeiboApp.Services;

/// <summary>
/// The pages 設定 links to. The App Store asks for the privacy policy and a way to contact the developer inside the
/// app as well as on the store page (docs/IOS_RELEASE.md).
/// </summary>
public static class AppLinks
{
    const string Docs = "https://github.com/ken-mro/KakeiboApp/blob/master/docs/";

    public const string Terms = Docs + "TERMS.md";

    public const string Privacy = Docs + "PRIVACY.md";

    public const string Contact = "https://github.com/ken-mro/KakeiboApp/issues";
}
