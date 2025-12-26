using p;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration.Pnp;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using vb14 = VBlib.pkarlibmodule14;
using System.Reflection;
using Windows.Devices.Geolocation;


namespace KrakTram
{
    public sealed partial class TrasaNaMapie : Page
    {
        public TrasaNaMapie()
        {
            this.InitializeComponent();
        }

        private List<MapElement> _punkty = null;

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e != null)
                _punkty = e.Parameter as List<MapElement>;
        }

        private void uiMapka_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Uno tu protestuje Unimplemented, ale przecież to jest strona tylko dla mnie, a więc na UWP jedynie
#pragma warning disable Uno0001 // Uno type or member is not implemented
            uiMapka.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Road;
#pragma warning restore Uno0001 // Uno type or member is not implemented
#if NETFX_CORE
            uiMapka.MapServiceToken = VBlib.pswd.GetMapToken(vb14.GetSettingsBool("pkarmode", p.k.IsThisMoje()));
#endif 

        }

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (_punkty is null) return;

            //var LandmarksLayer = new MapElementsLayer
            //{
            //    ZIndex = 1,
            //    MapElements = _punkty
            //};

            // na same geoposy
            var punkty = new List<pkar.BasicGeopos>();
            foreach (var mapElement in _punkty)
            {
                var tempIcon = mapElement as MapIcon;
                punkty.Add(pkar.BasicGeopos.FromObject(tempIcon.Location.Position));
            }
            var corners = pkar.BasicGeopos.GetCornersAndCenter(punkty);
            var cornerNW = corners[0];
            var cornerSE = corners[1];

            System.Diagnostics.Debug.WriteLine(corners[0].DumpAsJson());
            System.Diagnostics.Debug.WriteLine(corners[1].DumpAsJson());

            GeoboundingBox box = new GeoboundingBox(cornerNW.ToWinGeopos(), cornerSE.ToWinGeopos());
            await uiMapka.TrySetViewBoundsAsync(box, new Thickness(10), MapAnimationKind.None);

            uiMapka.MapElements.Clear();

            foreach (var mapElement in _punkty)
            {
                uiMapka.MapElements.Add(mapElement);
            }

        }


    }
}

