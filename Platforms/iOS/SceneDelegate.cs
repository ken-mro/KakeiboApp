using Foundation;

namespace KakeiboApp
{
    // iOS 27 shows no window for an app without scenes; MAUI's scene delegate creates it (Info.plist UIApplicationSceneManifest).
    [Register("SceneDelegate")]
    public class SceneDelegate : MauiUISceneDelegate
    {
    }
}
