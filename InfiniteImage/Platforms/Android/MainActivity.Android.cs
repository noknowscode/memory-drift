using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace InfiniteImage.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden,
    Theme = "@style/AppTheme"
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
}
