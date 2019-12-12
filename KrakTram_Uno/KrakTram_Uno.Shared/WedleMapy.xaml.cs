using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
            uiMapka.ZoomLevel = 12;
            if (p.k.IsThisMoje())
                uiMapka.MapServiceToken = "rDu5Hj5dykMMBblRgIaq~AalqbgIUph7UvMnI1WrB8A~AvZXaT3i_qD-UiyF61F4sbXe5ptSp3Wq0JdPF0dcOiAs0ZpAJ7W1QjQ28P5HCXSG";
            else
                uiMapka.MapServiceToken = "oaQmZvvDqQ39JcwdXSjK~TCuV7-3VaLPbJINptVo9gw~AuExUGkiHbYbqMIEVyx3RaKMprPZShlsQEpjGceEQIQM4HY9nYeWD0D19-Yb8OhY";

            // Uno Unimplemented
#if NETFX_CORE
            uiMapka.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Road;
#endif 
        }

        private void uiMapka_Holding(Windows.UI.Xaml.Controls.Maps.MapControl sender, Windows.UI.Xaml.Controls.Maps.MapInputEventArgs args)
        {
            App.mbGoGPS = false;
            App.mdLat = args.Location.Position.Latitude;
            App.mdLong = args.Location.Position.Longitude;
            App.mMaxOdl = p.k.GetSettingsInt("maxOdl", 1000);
            App.moOdjazdy.Clear();
            this.Frame.Navigate(typeof(Odjazdy));
        }
    }
}
