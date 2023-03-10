using System.Linq;
using System;
using vb14 = VBlib.pkarlibmodule14;
using static p.Extensions;


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
        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

            uiVersion.ShowAppVers(); // .Text = "v. " + p.k.GetAppVers();

            // int i = (int)(System.TimeSpan.FromSeconds(20).TotalMilliseconds / 250.0); // i=80

            vb14.SetSettingsInt("selectMode", 0);  // pokazywanie tabliczki: 0: punkt, 1: przystanek id ?
            KontrolaSzerokosci();

            // zeby nie bylo widac...
            uiBusStopList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiGoBusStop.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            HideAppPins();
            HideSearchButtons();

            this.ProgRingInit(true, false);

            this.ProgRingShow(true); // ProgresywnyRing(true);

                uiStopList.Header = "Tram";
                uiBusStopList.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiGoBusStop.Visibility = Windows.UI.Xaml.Visibility.Visible;

            // await LoadFavListAsync(); - gdy było razem z importem z XML (2022.04), teraz już usuwam ten import (2022.08)
            // a jeszcze wcześniej było wczytywanie ze zmiennej (też XML)
            App.oFavour.Load();

            ShowFavourCombo();

            await App.CheckLoadStopListAsync();
            if (vb14.GetSettingsBool("androAutoTram") || p.k.GetPlatform("uwp"))
            {
                uiStopList.ItemsSource = (from c in App.oStops.GetList("tram")
                                          orderby c.Name
                                          select c.Name).ToList();
            }
            else
            {
                mbAndroAdd = true;
                uiStopList.Items.Add(vb14.GetLangString("resUseSearch"));
                uiStopList.SelectedIndex = 0;

            }


                if (p.k.GetPlatform("uwp"))
                {
                    uiBusStopList.ItemsSource = (from c in App.oStops.GetList("bus")
                                                 orderby c.Name
                                                 select c.Name).ToList();
                }
                else
                {
                    mbAndroAdd = true;
                    uiBusStopList.Items.Add(vb14.GetLangString("resUseSearch"));
                    uiBusStopList.SelectedIndex = 0;
                }


            // dla Android nalezy poczekac z ustalaniem szerokosci
            if (!p.k.GetPlatform("uwp"))
                await System.Threading.Tasks.Task.Delay(500);

            this.ProgRingShow(false); //ProgresywnyRing(false);

            if (!p.k.GetPlatform("uwp"))
                KontrolaSzerokosci();   // powtarzamy dla Androida - moze juz jest przerysowane...

            uiStopList.Width = System.Math.Max(uiFavList.ActualWidth, 80);  // Max dla Android, bo wtedy chyba NaN
            uiBusStopList.Width = System.Math.Max(uiFavList.ActualWidth, 80); // Max dla Android, bo wtedy chyba NaN

            mbAndroAdd = false;

        }

        private void ShowFavourCombo()
        {
            uiFavList.ItemsSource = from c in App.oFavour.GetList()
                                    orderby c.Name
                                    select c.Name;
            if (App.oFavour.GetList().Count == 1)
                uiFavList.SelectedIndex = 0;
        }


        public static int widthCol0, widthCol3;

        private void KontrolaSzerokosci()
        {

            int iWidthLine, iWidthTyp, iWidthTime;

            uiTesterTyp.Visibility = Windows.UI.Xaml.Visibility.Visible;
            if ((int)uiTesterTyp.ActualWidth < 10)
            {
                // znaczy android, i mamy nieustalone!
                // niech pozostanie poprzednia wartosc (a nuż byla juz ustawiona poprawnie)
                uiTesterTyp.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiTesterLinia.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiTesterCzas.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                iWidthTyp = 40;
                iWidthLine = 60;
                iWidthTime = 40;
            }
            else
            {
                iWidthTyp = (int)uiTesterTyp.ActualWidth;  // typ
                uiTesterTyp.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                uiTesterLinia.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiTesterLinia.Text = "244";
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

            widthCol0 = System.Math.Max(iWidthLine, iWidthTyp);
            widthCol3 = iWidthTime;
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
            //uiPinBus.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //if (!vb14.GetSettingsBool("settingsAlsoBus"))
            //        return;

            if (uiPinBus.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                uiSearchBus.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void uiFavList_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            HideAppPins();
            uiUnPin.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void uiUnPin_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // usun z Fav
            string sName = uiFavList.SelectedItem.ToString();
            App.oFavour.Del(sName);
            App.oFavour.Save(false);
            ShowFavourCombo();

            // HideAppPins()
            uiUnPin.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }


        private void Pin_Stop(bool isBus)
        {
            HideAppPins();

            if (string.IsNullOrEmpty(msStopName))
                return;

            // dodaj do Fav
            // Dim sName As String = uiStopList.SelectedItem
           pkar.MpkWrap.Przystanek oPrzyst = App.oStops.GetItem(msStopName, isBus);
            if (oPrzyst == null)
                return;

            App.oFavour.Add(msStopName, oPrzyst.Geo, 150);  // odl 150, zeby byl tram/bus
            App.oFavour.Save(false);

            msStopName = ""; // powtorka buttonu nie zadziała

            ShowFavourCombo();
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
            Pin_Stop(true);
            uiSearchBus.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void uiPinTram_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Pin_Stop(false);
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
            foreach (VBlib.FavStop oStop in App.oFavour.GetList())
            {
                if (oStop.Name == sStop)
                {
                    App.mbGoGPS = false;
                    App.mMaxOdl = oStop.maxOdl;
                    App.mPoint = oStop.Geo;
                    App.moOdjazdy.Clear();
                    if (!mbSkalowane) KontrolaSzerokosci();  // dla Android 
                    this.Navigate(typeof(Odjazdy));
                }
            }
        }

        private void GoStop(string sName, bool isBus)
        {
            string sCat = "tram";
            if (isBus) sCat = "bus";

            foreach (pkar.MpkWrap.Przystanek oStop in App.oStops.GetList(sCat))
            {
                if (oStop.Name == sName)
                {
                    App.mbGoGPS = false;
                    App.mMaxOdl = vb14.GetSettingsInt("treatAsSameStop");
                    App.mPoint = oStop.Geo;
                    //App.msCat = oStop.IsBus;
                    App.moOdjazdy.Clear();
                    if (!mbSkalowane) KontrolaSzerokosci();  // dla Android 
                    this.Navigate(typeof(Odjazdy));
                }
            }
        }

        private void uiGoStop_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (uiStopList.SelectedValue == null)
                return;
            // KontrolaSzerokosci()
            string sStop = uiStopList.SelectedValue.ToString();
            GoStop(sStop, false);
        }

        private void uiGoBusStop_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (uiBusStopList.SelectedValue == null)
                return;
            // KontrolaSzerokosci()
            string sStop = uiBusStopList.SelectedValue.ToString();
            GoStop(sStop, true);
        }

        private async void uiStopList_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            string sMask = await vb14.DialogBoxInputResAsync("msgEnterName");

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
            string sMask = await vb14.DialogBoxInputResAsync("msgEnterName");
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
#pragma warning disable IDE1006 // Naming Styles
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
            this.Navigate(typeof(Zmiany));
        }

        private void uiGoMap_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Navigate(typeof(WedleMapy));
        }

        private void uiHist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Navigate(typeof(Historia));
        }

        private void bSetup_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Setup), "MAIN");
        }

#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE1006 // Naming Styles

    }
}
