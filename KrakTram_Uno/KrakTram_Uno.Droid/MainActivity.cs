using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using Android.Views;

namespace KrakTram.Droid
{
    [Activity(
            MainLauncher = true,
            ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize,
            WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden
        )]
    public class MainActivity : Windows.UI.Xaml.ApplicationActivity
    {
        // https://android.jlelse.eu/the-complete-android-splash-screen-guide-c7db82bce565
        // że niby Main.cs to załatwi?
        //protected override void OnCreate(Bundle savedInstanceState )
        //{
        //    Android.Support.V7.App.AppCompatActivity.SetTheme(R.style.AppTheme);
        //    base.OnCreate(savedInstanceState);
        //}
    }
}

