using Android.App;
using Android.Content.PM;
using Android.OS;
namespace GerenciadorAulas.App;
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
#pragma warning disable CS0672, CS0618
    protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
    {
        if (!AndroidGoogleAuthorization.HandleResult(this, requestCode, resultCode, data)) base.OnActivityResult(requestCode, resultCode, data);
    }
#pragma warning restore CS0672, CS0618
}
