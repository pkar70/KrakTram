using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using vb14 = VBlib.pkarlibmodule14;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KrakTram
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WedleMapy : Page
    {
        public WedleMapy()
        {
            this.InitializeComponent();
        }

        private void uiMapka_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Windows.Devices.Geolocation.BasicGeoposition oPosition;
            oPosition = new Windows.Devices.Geolocation.BasicGeoposition();
            oPosition.Latitude = 50.061389;  // współrzędne wedle Wiki
            oPosition.Longitude = 19.938333;
            // Dim oPoint As Windows.Devices.Geolocation.Geopoint
            // oPoint = New Windows.Devices.Geolocation.Geopoint(oPosition)

            uiMapka.Center = new Windows.Devices.Geolocation.Geopoint(oPosition);
            if (p.k.IsFamilyMobile())
                uiMapka.ZoomLevel = 10;
            else
            uiMapka.ZoomLevel = 12;

            uiMapka.PedestrianFeaturesVisible = true;
            uiMapka.TransitFeaturesVisible = true;
            uiMapka.TransitFeaturesEnabled = true; // od 14393, ale że not implemented?

            // Uno Unimplemented
#if NETFX_CORE
            uiMapka.MapServiceToken = VBlib.pswd.GetMapToken(vb14.GetSettingsBool("pkarmode", p.k.IsThisMoje()));
            uiMapka.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Road;
#endif 
        }

        private void uiMapka_Holding(Windows.UI.Xaml.Controls.Maps.MapControl sender, Windows.UI.Xaml.Controls.Maps.MapInputEventArgs args)
        {
            App.mbGoGPS = false;
#if NETFX_CORE
            App.mPoint = args.Location.Position;
            App.mMaxOdl = vb14.GetSettingsInt("maxOdl");
            App.moOdjazdy.Clear();
            this.Frame.Navigate(typeof(Odjazdy));
#endif 
        }
    }
}
