using System.Linq;
using System;

/* ewentualnie:
 progressbar przy czytaniu kolejnych tabliczek, szczegolnie dla Android? taki półprzezroczysty?
 wiecej mozliwosci 'isthismoje' oraz 'biggerpermission': Aska, Gibala, etc?
 android: nie działa ContextMenuFlyout!
 android: nie ma fontu Times!
 Warning! IsThisMoje chyba nie dziala! Tzn. zwraca windows_phone a nie nazwe Lumia_pkar?

    <ikonka kosza - poprawne pokazywanie! jak jest cos wybrane w Combo>
    <instrukcja: kółeczka doubleclick, click, oraz contextclick?>

Uno STRIP:
* mapka
* szybkie wypełnianie Combo
* contextMenu
* historia sieci

2021.04
* próba uproszczenia Settings przez zrobienie Binding (i sprawdzenie czy to działa na Android) - DZIAŁA
* próba czy można usunąć z Settings\ListView styl (workaround dla Height=0) - dalej NIE DZIAŁA
* próba czy można dać not_win:CommandBar (było na razie StackPanel) - (niby działa, ale z tym paskiem na dole ma problem - zasłania go)

2021.03
* 3.6.0-dev.186
* Uwaga! Android 11.0! [zresztą to wymóg Gogusia, od VIII 2021 new apps mają być 11.0]. Zmiana w Uno 9 XI 2020.

 
2020.10.30
 * przejście na własne Uno oparte o 3.1.6
 *      more fields in Geoposition	| 4061@20.09.17	| 2020.09.22
 * pkModuleShared.cs [..\..\..\_mojeSuby\pkarModule-Uno3-1-6.cs]
 *      ProgRing z tegoż modułu

2020.03.10
 * trasa zwracało pustą, pewnie referer - bo powtórzenie requesta pomaga.

STORE ANDROID 2002.1

2020.02.12
 * [Android] przepięcie na Uno.945 z moimi dodatkami - w efekcie pkmodule aktualizacja (było: 3092)
 * [Android] splashscreen - ale nieudane, bo zostaje na srodku ekranu. Może za długo trwa ładowanie mainpage?
            wycofuje sie z tego (<!-- --> w 
            H:\Home\PIOTR\VStudio\_Vs2017\KrakTram\KrakTram_Uno\KrakTram_Uno.Droid\Resources\values\Styles.xml)

2020.01.09
 * Trasa: gdy wczytana z cache trasa ma zero przystanków, traktuj plik jak nieistniejący
 * Android: usunięcie stylu co miał być splashscreenem (bo to chyba likwiduje ikonkę, protest z GoogleStore)
 * Android: powrót do Uno 3092, bo w 409 nie działa DoubleClick
 * Android: 3092 wymusza pełną własną obsługę Geolocator, GetAppVersion, 

2020.01.07
 * szukanie przystanku wedle maski - gdy Cancel, i Android, to exit sub (bez pokazywania pełnej listy)

STORE WINDOWS   10.2001.1.0
STORE ANDROID   10.2001.1.0

2019.12.31
 * własne tylko Geolocator:RequestAccess, samo pytanie GPSa juz z Uno (mojego zresztą)
 * dla Android, przyjmuje minimalne szerokości hardcoded, zeby ładniej wyglądąło (skoro nie umie skalować)

2019.12.27
 * do mapki: dołączam Pedestrian, Transit (choć i tak chyba nie ma efektów)
 * zoom: 12 na desktop, ale 10 w telefonie, żeby wiecej zmieścic?
 * Odjazdy: uproszczenie XAML czasu odjazdu (bez pola o wysokosci 1)
 * MainPage, skalowanie: gdy włączone są także autobusy, szerokość pola jest dla "244", a gdy tylko tramwaje - dla "50"
    
2019.12.26
 * Odjazdy:DoubleTap na linii - przeskok do odjazdów danej linii (skrót, z ominięciem ContextMenu), to działa także na Android
 * Odjazdy: tam, gdzie ContextMenu był do tekstu, a mógł być do piętro wyżej (Grid), przeniosłem do grid
 * Trasa: nowe dla Android, skoro skrót z DoubleClick działa
 * Trasa: DoubleTap na przystanku przeskakuje do tabliczki przystanku (skrót)
 * Historia, oraz WedleMapy: nie wedle IsThisMoje, ale wedle GetBool z default isthismoje (ustawiane po wpisaniu punktu o nazwie "pkarinit")

2019.12.23
 * podłączenie nowej kompilacji Uno (z 2.1.0-dev.408) - GetAppVersion, OpenBrowser w pkmodule
     * dalej nie ma mapy
     * dalej nie ma szerokości pola w Page_Loaded

STORE ANDROID

STORE WINDOWS

2019.12.11
 Nowa strona: WedleMapy.xaml, wybor lokalizacji z okolicy ktorej ma pokazac tabliczki

2019.12.10
 przeniesienie z Uno do wlasnego MyGeolocator - by mozna bylo sledzic. Poprawki i uzupelnienia w kodzie (ale i tak nie dziala)
 
2019.11.25
 MainPage: progressring podczas uruchamiania (szczególnie ważne dla Android)
 MainPage: przy itemsSource=LINQ stosuję LINQ.ToList(), co chyba faktycznie znacznie przyspiesza ładowanie Combo (nie na tyle, by bus także wczytywać)
 Setup:[andro] dodatkowy ToggleSwitch, wypełnianie listy tramwajów
 Odjazdy:WypiszTabele: przy itemsSource=LINQ stosuję LINQ.ToList()

2019.11.11
 MainPage: gdy tylko jedno w Fav, automatyczny select tego
 MainPage: po zapinowaniu, wraca guzik wyszukiwania przystanku
 Reroutes: na start, rządek z webview jest likwidowany, przez to jest cały ekran na listę zmian [UWPonly]
 Reroutes: pasek pomiędzy webview a listą, oraz pomiędzy pozycjami z listy 
 Reroutes: wyszukiwarka
 Odjazdy: przywrócenie Real, i Act prefixów przy odjazdach
 Odjazdy: pkarmode wedle settings (po wpisaniu nazwy punktu), ale z defaultem IsThisMoje

STORE WINDOWS, oraz testowa Android

2019.11.07
 mainpage: zmiana Header combo na "tram", gdy są także autobusy
 mainpage: jesli po Search jest tylko jeden, to go od razu robi Select
 mainpage:Android: nie wypelnia combo przystankow, bo trwa to za dlugo - daje tam '--use search', zeby przyspieszyc pokazanie strony (na Android: dopiero po Loaded)
 setup: lista bliskich przystankow, sortowana wedle odleglosci (przywrocenie funkcjonalnosci ktora byla w XSLT)
 trasa: po wczytaniu trasy (refresh), jak jest to nieudane, to nie wylatuje z bledem 
 trasa: dla Uno, musi byc System.net.httpclient, co oznacza serie problemów z redirect- rozwiazane
 mainpage: szerokość Combo bus i tram są kopiowane z fav (bo to jest szersze...)
    
2019.11.06
  mainpage: pokazywanie numeru wersji

2019.11.05
  setup: przeróbka z HTMLview na ListView (z nadzieją że na Android będzie lepiej wyglądało, nie takie malutkie)
  odjazdy:no_win: dodaje Margin, bo Padding nie działa?

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
        private bool mbAndroAdd = false;
        private bool mbSkalowane = false;

#if false
        private void policzmy()
        {
            // testowanie czasu
            int iSumLen = 0;
            int iCntStop = 0;
            foreach (string sStop in from c in App.oStops.GetList("tram")
                                     orderby c.Name
                                     select c.Name)
            {
                iSumLen += sStop.Length;
                iCntStop++;
            }
        }

        private void wypelnijmy()
        {
            // testowanie czasu
            uiStopList.ItemsSource = from c in App.oStops.GetList("tram")
                                     orderby c.Name
                                     select c.Name;
        }
#endif


        //private void ProgresywnyRing(bool sStart)
        //{

        //    if (sStart)
        //    { double dVal;
        //        dVal = (System.Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2;
        //        if (dVal < 100) dVal = 100; // dla Android - jakby jeszcze nie było
        //        uiProcesuje.Width = dVal;
        //        uiProcesuje.Height = dVal;

        //        uiProcesuje.Visibility = Windows.UI.Xaml.Visibility.Visible;
        //        uiProcesuje.IsActive = true;
        //    }
        //    else
        //    {
        //        uiProcesuje.IsActive = false;
        //        uiProcesuje.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //    }
        //}


        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiVersion.Text = "v. " + p.k.GetAppVers();

            // int i = (int)(System.TimeSpan.FromSeconds(20).TotalMilliseconds / 250.0); // i=80

            p.k.SetSettingsInt("selectMode", 0);  // pokazywanie tabliczki: 0: punkt, 1: przystanek id ?
            KontrolaSzerokosci();

            // zeby nie bylo widac...
            uiBusStopList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiGoBusStop.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            HideAppPins();
            HideSearchButtons();

            p.k.ProgRingInit(true, false);

            p.k.ProgRingShow(true); // ProgresywnyRing(true);

            if (p.k.GetSettingsBool("settingsAlsoBus"))
            {
                uiStopList.Header = "Tram";
                uiBusStopList.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiGoBusStop.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }

            if (!p.k.GetPlatform("uwp"))
            {
                // inicjalizacja dla Androida, gdy nie ma jeszcze danych

            }

            await App.LoadFavList();
            uiFavList.ItemsSource = from c in App.oFavour.GetList()
                                    orderby c.Name
                                    select c.Name;
            if (App.oFavour.GetList().Count == 1)
                uiFavList.SelectedIndex = 0;


            await App.CheckLoadStopList();
            if (p.k.GetSettingsBool("androAutoTram") || p.k.GetPlatform("uwp") )
            {
                uiStopList.ItemsSource = (from c in App.oStops.GetList("tram")
                                          orderby c.Name
                                          select c.Name).ToList();
            }
            else
            {
                mbAndroAdd = true;
                uiStopList.Items.Add(p.k.GetLangString("resUseSearch"));
                uiStopList.SelectedIndex = 0;

            }

            if (p.k.GetSettingsBool("settingsAlsoBus"))
            {
                // przeniesione wyzej.
                //uiStopList.Header = "Tram";
                //uiBusStopList.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //uiGoBusStop.Visibility = Windows.UI.Xaml.Visibility.Visible;

                if ( p.k.GetPlatform("uwp"))
                {
                    uiBusStopList.ItemsSource = (from c in App.oStops.GetList("bus")
                                                orderby c.Name
                                                select c.Name).ToList();
                }
                else
                {
                    mbAndroAdd = true;
                    uiBusStopList.Items.Add(p.k.GetLangString("resUseSearch"));
                    uiBusStopList.SelectedIndex = 0;
                }

            }

            // dla Android nalezy poczekac z ustalaniem szerokosci
            if (!p.k.GetPlatform("uwp"))
                await System.Threading.Tasks.Task.Delay(500);


            p.k.ProgRingShow(false); //ProgresywnyRing(false);

            if (!p.k.GetPlatform("uwp"))
                KontrolaSzerokosci();   // powtarzamy dla Androida - moze juz jest przerysowane...

            uiStopList.Width = System.Math.Max(uiFavList.ActualWidth, 80);  // Max dla Android, bo wtedy chyba NaN
            uiBusStopList.Width = System.Math.Max(uiFavList.ActualWidth, 80); // Max dla Android, bo wtedy chyba NaN

            mbAndroAdd = false;
        }

        private void KontrolaSzerokosci()
        {
        // kontrola szerokosci dla pola lewego (linia, typ)
        int iWidthLine, iWidthTyp, iWidthTime;

            uiTesterTyp.Visibility = Windows.UI.Xaml.Visibility.Visible;
            if ((int)uiTesterTyp.ActualWidth < 10)
            {
                // znaczy android, i mamy nieustalone!
                // niech pozostanie poprzednia wartosc (a nóż byla juz ustawiona poprawnie)
                uiTesterTyp.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiTesterLinia.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiTesterCzas.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                iWidthTyp = 40;
                iWidthLine = 40;
                iWidthTime = 40;
            }
            else
            {
                iWidthTyp = (int)uiTesterTyp.ActualWidth;  // typ
                uiTesterTyp.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                uiTesterLinia.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiTesterLinia.Text = p.k.GetSettingsBool("settingsAlsoBus") ? "244" : "50";
                iWidthLine = (int)uiTesterLinia.ActualWidth;  //linia
                uiTesterLinia.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                uiTesterCzas.Visibility = Windows.UI.Xaml.Visibility.Visible;
                iWidthTime = (int)uiTesterCzas.ActualWidth;  // czas
                uiTesterCzas.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                mbSkalowane = true;
            }

            //'uiTester.FontSize = 9
            //'uiTester.Text = "2014N"
            //'iWidth = uiTester.ActualWidth   'typ
            //'uiTester.FontSize = 20
            //'uiTester.Text = "22 min"
            //'iWidth2 = uiTester.ActualWidth  'linia

            //'uiTester.FontSize = 28
            //'uiTester.FontWeight = Windows.UI.Text.FontWeights.Bold
            //'uiTester.Text = "50"
            //'uiTester.Visibility = Visibility.Collapsed

            p.k.SetSettingsInt("widthCol0", System.Math.Max(iWidthLine, iWidthTyp));
            p.k.SetSettingsInt("widthCol3", iWidthTime);
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

            uiSearchBus.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiPinBus.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (!p.k.GetSettingsBool("settingsAlsoBus"))
                    return;

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

            if (mbAndroAdd) return;
            Windows.UI.Xaml.Controls.ComboBox oCombo = sender as Windows.UI.Xaml.Controls.ComboBox;

            if (oCombo.SelectedItem == null)
                return;

            HideAppPins();
            msStopName = oCombo.SelectedItem.ToString();

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
            uiSearchBus.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void uiPinTram_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiPin_Click(null, null);
            uiSearchTram.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void bGetGPS_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            App.mbGoGPS = true;    // zgodnie z GPS prosze postapic (jak do tej pory)
            App.moOdjazdy.Clear();
            if(!mbSkalowane) KontrolaSzerokosci();  // dla Android 
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
                    if (!mbSkalowane) KontrolaSzerokosci();  // dla Android 
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
                    if (!mbSkalowane) KontrolaSzerokosci();  // dla Android 
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
            string sMask = await p.k.DialogBoxInputAsync("msgEnterName");

            if (string.IsNullOrEmpty(sMask))
            {
                if (!p.k.GetPlatform("uwp")) return;
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

            if(uiStopList.Items.Count == 1)
            {
                uiStopList.SelectedIndex = 0;
                uiSearchTram.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiPinTram.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private async void uiBusStopList_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            string sMask = await p.k.DialogBoxInputAsync("msgEnterName");
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
            if (uiBusStopList.Items.Count == 1)
            {
                uiBusStopList.SelectedIndex = 0;
                uiSearchBus.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiPinBus.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter

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

        private void uiGoMap_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(WedleMapy));
        }

        private void uiHist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Historia));
        }

        private void bSetup_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Setup), "MAIN");
        }

#pragma warning restore IDE0060 // Remove unused parameter

    }
}
