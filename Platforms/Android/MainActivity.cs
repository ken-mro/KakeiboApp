using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace KakeiboApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // After a key event (keyboard Enter/Delete) the window leaves touch mode. When a popup then
            // closes, focus returns to Shell's page container and Android draws its default focus
            // highlight over the whole page, leaving it dimmed. Containers never need that highlight.
            if (OperatingSystem.IsAndroidVersionAtLeast(26) && Window?.DecorView.ViewTreeObserver is { } observer)
            {
                observer.GlobalFocusChange += (_, e) =>
                {
                    // Checked again here: the analyzer does not carry the version check into the lambda.
                    if (OperatingSystem.IsAndroidVersionAtLeast(26) && e.NewFocus is ViewGroup container)
                    {
                        container.DefaultFocusHighlightEnabled = false;
                    }
                };
            }
        }
    }
}
