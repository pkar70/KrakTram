using System.Linq;
using Windows.UI.Xaml.Controls;
using vb14 = VBlib.pkarlibmodule14;
using static p.Extensions;
//using pkar.MpkMain;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Foundation;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Text;
using System.Globalization;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KrakTram
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScheduledTrasa : Page
    {
        public ScheduledTrasa()
        {
            this.InitializeComponent();
            this.ProgRingInit(true, false);
        }

        private string _tripId = "";
        private string _stopName = "";
        private bool _isBus = false;


        private System.Collections.Generic.List<pkar.MpkWrap.DalszyStop> _lista;

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e != null)
            {
                string temp = e.Parameter.ToString();
                if(temp.Contains("|BUS"))
                {
                    _isBus = true;
                    temp = temp.Replace("|BUS", "");
                }
                var splited = temp.Split('|');
                _tripId = splited[0];
                _stopName = splited[1];
            }
        }

        private void uiClose_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.GoBack();
        }

        private void uiGoPrzystanek_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oFE = sender as Windows.UI.Xaml.FrameworkElement;
            var oItem = oFE?.DataContext as pkar.MpkWrap.DalszyStop;
            if (oItem is null) return;
            ShowTabliczka(VBlib.Trasa.NazwaBezSlupka(oItem.shortName));
        }

        private void ShowTabliczka(string shortName)
        {
            foreach (pkar.MpkWrap.Przystanek oStop in App.oStops.GetList("all"))
            {
                if (oStop.id == shortName)
                {
                    App.mbGoGPS = false;
                    App.mMaxOdl = vb14.GetSettingsInt("treatAsSameStop");
                    App.mPoint = oStop.Geo;
                    App.moOdjazdy.Clear();
                    this.Navigate(typeof(Odjazdy));
                }
            }
        }

        public static string BoldTime = "";

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            //uiTitle.Text = vb14.GetLangString("resScheduled");

            this.ProgRingShow(true);
            var trasa = new pkar.MpkWrap.DalszaTrip();
            var lista = await trasa.GetTrasa(_isBus, _tripId);
            this.ProgRingShow(false);

            _lista = new List<pkar.MpkWrap.DalszyStop>();

            bool bDoCopy = false;
            foreach (var oItem in lista)
            {
                if (oItem.name == _stopName) bDoCopy = true;
                if (bDoCopy) _lista.Add(oItem);
            }

            if(_lista.Count<1)
            {
                _lista = null;
#if !__ANDROID__
                uiShowOnMap.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
#endif 
            }
            else
            {
#if !__ANDROID__
                uiShowOnMap.Visibility = Windows.UI.Xaml.Visibility.Visible;
#endif 

                // pierwszy to czas start, wylicz z niego maxTime
                DateTime dt;
                try
                {
                    dt = DateTime.ParseExact(lista[0].actualTime, "HH:mm", CultureInfo.InvariantCulture);
                }
                catch
                {
                    dt = DateTime.Now;
                }
                string maxTime = dt.AddMinutes(20).ToString("HH:mm");


                // i 20 minut od niego
                foreach (var oItem in _lista)
                {
                    if (oItem.actualTime.CompareTo(maxTime) > 0) break;
                    BoldTime = oItem.actualTime;
                }

            }


            uiListStops.ItemsSource = _lista;

        }

        private void uiShowOnMap_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // przygotuj listę punktów dla mapy - przystanki z czasem

            var _punkty = new List<MapElement>();
            foreach (var item in _lista)
            {
                var loc = stopek2geopoint(pkar.MpkWrap.Linia.NazwaBezSlupka(item.name), _isBus);
                if (loc is null) continue;

                MapIcon stopek = new MapIcon
                {
                    Location = loc,
                    NormalizedAnchorPoint = new Point(0.5, 0.5),
                    ZIndex = 0,
                    Title = item.name + " " + item.actualTime
                };

                _punkty.Add(stopek);
            }

            this.Frame.Navigate(typeof(TrasaNaMapie), _punkty);

        }

        private Windows.Devices.Geolocation.Geopoint stopek2geopoint(string stopName, bool isbus)
        {
            var stopek = App.oStops.Find(c => (c.Name == stopName) && (c.IsBus == isbus));
            if (stopek is null) return null;
            return stopek.Geo.ToWinGeopoint();
        }

    }

    public class KonwersjaCzasNaBold : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            string temp= System.Convert.ToString(value);
            if (temp.CompareTo(ScheduledTrasa.BoldTime) == 0)
                return FontWeights.Bold;

            return FontWeights.Normal;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}