using System.Linq;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Controls;
//using Windows.UI.Xaml.Input;

/* ewentualnie:
 progressbar przy czytaniu kolejnych tabliczek
 wiecej mozliwosci 'isthismoje' oraz 'biggerpermission': Aska, Gibala, etc?

 2019.10.29
  guzik Position (otwieranie panelu override GPS) jest tylko przy Setup z Main, a nie z Odjazdy
  Setup: po zmianie bus/tram odświeżał listę, ale nie uwzględniał nowego ustawienia AlsoBus

 2019.10.26
 gdy podczas startu app, w cache lista_przystanków.Count < 1, to wymuś odczyt

 2019.10.25
 mainpage: buttony Pin/Unpin przy Combo, a nie w BottomAppBar
 mainpage: buttony z lupką (search)
 czytanie danych: przy błędzie pokazuje o który przystanek chodzi
 czytanie danych: DialogBox 'zero kursow' nie jest pokazywany przy wczytywaniu tabliczek pojedynczych, tylko raz (globalnie)
 poprawka: można przejść na tabliczkę przystankową także autobusową (wcześniej tylko tramwajowe)

 migracja do C# - udana
 likwidacja catch(Exception ex) » catch - żeby ograniczyc liczbę Warningów
 częściowo dodane await do DialogBoxów - żeby ograniczyc liczbę Warningów

 GITsync

  migracja z Windows.Data.Json do Newtonsoft.Json.Linq

 BUG: 20190822 dlaczego pokazuje 'no tram in next hour' przy każdym przystanku tramwajowym?
 2019.08.04 wczytanie przystanków tram JSON 0 objects - nie Cancel, tylko próbuje wczytac autobusowe

 2019.07.27
 migracja do pkarmodule
 dla IsThisMoje, statystyka opóźnień - na razie tekstowa


 4.1907 (29 VI)
 2019.06.26 odjazdy:kierunek:contextMenu tylko ten, albo usun ten, kierunek z listy

 2019.04.02
 1. viewport w HEAD przy pokazywaniu zmian/reroutes zeby bylo czytelne
 2019.04.09
 1. pokazywanie częściowej listy (co kazde wczytanie tabliczki)
 2019.04.19
 1. zoom mode w historii sieci
 2. zmiana domyślnego 30 dni na 14 dni w ważności cache objazdów

 2019.03.16
 1. przygotowanie: app korzysta z App.GetSettingsInt("treatAsSameStop", 150), ale nie ma ustawiania tego
 2. wyszukiwanie przystanków wedle mask
 3. pamietanie sposobu sortowania
 2019.03.18
 1. progressring przy wczytywaniu trasy
 2019.03.21
 1. strona Zmiany/Reroutes, cache'owalna
 2. poprawka - po refresh trasy nie było refresh (tylko do pliku zapisywalo nowe)


 Wzięte ze Store description:
 v.4.1907
* można filtrować listę odjazdów wedle kierunków ('tylko ten', lub 'bez tego'), dostępne w menu kontekstowym pola kierunku.

v.4.1905
* informacja o objazdach powinna być teraz czytelniejsza
* dane o objazdach odświeżane są po 14 dniach (poprzednio: po 30) - można też wymusić odświeżenie 
* podczas wczytywania tabliczek pokazywane są dane częściowe
* zoom na stronie historii sieci tramwajowej


v.4.1904 (zmiana stylu numerowania wersji)
* można wyszukać przystanek wpisując fragment nazwy
* sortowanie jest zapamiętywane, i używane po kolejnym uruchomieniu aplikacji
* podczas wczytywania danych pokazywany jest ProgressRing  
* strona objazdów/zmian tras

v.3.1: Teraz aplikacja pokazuje także tabliczki dla przystanków autobusowych. Aby włączyć tą funkcjonalność, należy wejść na stronę ustawień.

v.2.1:
a) historia sieci tramwajowej w Krakowie (dostępne z głównej strony aplikacji)
b) licznik przystanków na trasielinii (po pomnożeniu x2 daje przybliżony czas podróży)



*/

namespace KrakTram
{
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private string msStopName = "";

        /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            p.k.SetSettingsInt("selectMode", 0);  // pokazywanie tabliczki: 0: punkt, 1: przystanek id ?
                                                  // KontrolaSzerokosci()

            // zeby nie bylo widac...
            uiBusStopList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiGoBusStop.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            HideAppPins();
            HideSearchButtons();

            await App.LoadFavList();
            uiFavList.ItemsSource = from c in App.oFavour.GetList()
                                    orderby c.Name
                                    select c.Name;

            await App.CheckLoadStopList();
            uiStopList.ItemsSource = from c in App.oStops.GetList("tram")
                                     orderby c.Name
                                     select c.Name;

            if (p.k.GetSettingsBool("settingsAlsoBus"))
            {
                uiBusStopList.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiGoBusStop.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiBusStopList.ItemsSource = from c in App.oStops.GetList("bus")
                                            orderby c.Name
                                            select c.Name;
            }
        }

        private void HideAppPins()
        {
            uiUnPin.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiPinTram.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiPinBus.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //uiAppSep.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void HideSearchButtons()
        {
            uiSearchTram.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (uiPinTram.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                uiSearchTram.Visibility = Windows.UI.Xaml.Visibility.Visible;
            if (!p.k.GetSettingsBool("settingsAlsoBus"))
                return;

            uiSearchBus.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (uiPinBus.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                uiSearchBus.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void uiFavList_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            HideAppPins();
            uiUnPin.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private async void uiUnPin_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // usun z Fav
            string sName = uiFavList.SelectedItem.ToString();
            App.oFavour.Del(sName);
            await App.oFavour.Save(false);
            uiFavList.ItemsSource = from c in App.oFavour.GetList()
                                    orderby c.Name
                                    select c.Name;
            // HideAppPins()
            uiUnPin.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }


        private async void uiPin_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(msStopName))
                return;

            // dodaj do Fav
            // Dim sName As String = uiStopList.SelectedItem
            Przystanek oPrzyst = App.oStops.GetItem(msStopName);
            if (oPrzyst == null)
                return;

            App.oFavour.Add(msStopName, oPrzyst.Lat, oPrzyst.Lon, 150);  // odl 150, zeby byl tram/bus
            await App.oFavour.Save(false);

            msStopName = ""; // powtorka buttonu nie zadziała

            uiFavList.ItemsSource = from c in App.oFavour.GetList()
                                    orderby c.Name
                                    select c.Name;
            HideAppPins();
        }


        private void uiStopList_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            Windows.UI.Xaml.Controls.ComboBox oCombo = sender as Windows.UI.Xaml.Controls.ComboBox;
            HideAppPins();

            msStopName = (sender as Windows.UI.Xaml.Controls.ComboBox).SelectedItem.ToString();

            if (oCombo.Name == "uiBusStopList")
            {
                uiSearchBus.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiPinBus.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                uiSearchTram.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiPinTram.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private void uiPinBus_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiPin_Click(null, null);
        }

        private void uiPinTram_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiPin_Click(null, null);
        }

        private void bGetGPS_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            App.mbGoGPS = true;    // zgodnie z GPS prosze postapic (jak do tej pory)
            App.moOdjazdy.Clear();
            this.Frame.Navigate(typeof(Odjazdy));
        }

        private void uiGoFavour_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (uiFavList.SelectedValue == null)
                return;

            string sStop = uiFavList.SelectedValue.ToString();
            foreach (FavStop oStop in App.oFavour.GetList())
            {
                if (oStop.Name == sStop)
                {
                    App.mbGoGPS = false;
                    App.mMaxOdl = oStop.maxOdl;
                    App.mdLat = oStop.Lat;
                    App.mdLong = oStop.Lon;
                    App.moOdjazdy.Clear();
                    this.Frame.Navigate(typeof(Odjazdy));
                }
            }
        }

        private void GoStop(string sName, string sCat)
        {
            foreach (Przystanek oStop in App.oStops.GetList(sCat))
            {
                if (oStop.Name == sName)
                {
                    App.mbGoGPS = false;
                    App.mMaxOdl = p.k.GetSettingsInt("treatAsSameStop", 150);
                    App.mdLat = oStop.Lat;
                    App.mdLong = oStop.Lon;
                    App.msCat = oStop.Cat;
                    App.moOdjazdy.Clear();
                    this.Frame.Navigate(typeof(Odjazdy));
                }
            }
        }

        private void uiGoStop_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (uiStopList.SelectedValue == null)
                return;
            // KontrolaSzerokosci()
            string sStop = uiStopList.SelectedValue.ToString();
            GoStop(sStop, "tram");
        }

        private void uiGoBusStop_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (uiBusStopList.SelectedValue == null)
                return;
            // KontrolaSzerokosci()
            string sStop = uiBusStopList.SelectedValue.ToString();
            GoStop(sStop, "bus");
        }

        private async void uiStopList_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            string sMask = await p.k.DialogBoxInput("msgEnterName");

            if (string.IsNullOrEmpty(sMask))
            {
                sMask = sMask.ToLower();
                uiStopList.ItemsSource = from c in App.oStops.GetList("tram")
                                         orderby c.Name
                                         select c.Name;
            }
            else
            {
                sMask = sMask.ToLower();
                uiStopList.ItemsSource = from c in App.oStops.GetList("tram")
                                         where c.Name.ToLower().Contains(sMask)
                                         orderby c.Name
                                         select c.Name;
            }
        }

        private async void uiBusStopList_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            string sMask = await p.k.DialogBoxInput("msgEnterName");
            if (string.IsNullOrEmpty(sMask))
            {
                sMask = sMask.ToLower();

                uiBusStopList.ItemsSource = from c in App.oStops.GetList("bus")
                                            orderby c.Name
                                            select c.Name;
            }
            else
            {
                sMask = sMask.ToLower();
                uiBusStopList.ItemsSource = from c in App.oStops.GetList("bus")
                                            where c.Name.ToLower().Contains(sMask)
                                            orderby c.Name
                                            select c.Name;
            }
        }

        private void uiSearchBus_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiBusStopList_DoubleTapped(null, null);
        }

        private void uiSearchTram_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiStopList_DoubleTapped(null, null);
        }


        private void uiChanges_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Zmiany));
        }

        private void uiHist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Historia));
        }

        private void bSetup_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Setup), "MAIN");
        }
    }
}
