using System;
using System.Linq;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KrakTram
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Odjazdy : Windows.UI.Xaml.Controls.Page
    {
        public Odjazdy()
        {
            this.InitializeComponent();
        }

        // Private miSortMode As Integer = 0


        private void bSetup_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Setup), "ODJAZD");
        }

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // App.moOdjazdy.Clear() - pietro wyzej to jest zrobione
            await WczytajPokazDane(false);
        }

        private async void uiGetData_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await WczytajPokazDane(true);
        }


        private async System.Threading.Tasks.Task WczytajPokazDane(bool bForce)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                await p.k.DialogBoxRes("resErrorNoNetwork");
                return;
            }

            uiGoTtssBar.IsEnabled = false;

            if (App.moOdjazdy.Count() == 0 || bForce)
                await CzytanieTabliczek();

            WypiszTabele(true);

            uiGoTtssBar.IsEnabled = true;
        }

        private async System.Threading.Tasks.Task CzytanieTabliczek()
        {
            if (App.mbGoGPS)
            {
                // wedle GPS
                App.mMaxOdl = p.k.GetSettingsInt("maxOdl", 1000);
                // ustaw wspolrzedne
                uiWorking.Text = "o";
                Windows.Foundation.Point oPoint = await App.GetCurrentPoint();
                App.mdLat = oPoint.X;
                App.mdLong = oPoint.Y;
                uiWorking.Text = " ";
            }
            else
            {
            }

            App.moOdjazdy.Clear();

            // ustalony jest skąd szukamy przystanków i jak daleko
            await WczytajTabliczkiWOdleglosci(App.mdLat, App.mdLong, App.mMaxOdl);

            App.moOdjazdy.OdfiltrujMiedzytabliczkowo();
        }

        private async System.Threading.Tasks.Task WczytajTabliczkiWOdleglosci(double dLat, double dLon, double dOdl)
        {
            int iWorking = 0;
            int iOdl;

            string sFilter = "tram";
            if (p.k.GetSettingsBool("settingsAlsoBus"))
                sFilter = "all";

            foreach (Przystanek oNode in App.oStops.GetList(sFilter))
            {
                uiWorking.Text = ".";
                iOdl = App.GPSdistanceDwa(dLat, dLon, oNode.Lat, oNode.Lon);
                if (iOdl < dOdl)
                {
                    iWorking += 1;
                    switch (iWorking % 4)
                    {
                        case 1:
                                uiWorking.Text = "/";
                                break;
                        case 2:
                                uiWorking.Text = "-";
                                break;
                        case 3:
                                uiWorking.Text = @"\";
                                break;
                        case 0:
                                uiWorking.Text = "|";
                                break;
                    }

                    string sErrData = oNode.Name;
                    if (p.k.GetSettingsBool("settingsAlsoBus"))
                    {
                        if (oNode.Cat == "bus")
                            sErrData += " (bus)";
                        else
                            sErrData += " (tram)";
                    }

                    int iId;
                    int.TryParse(oNode.id, out iId);
                    await App.moOdjazdy.WczytajTabliczke(oNode.Cat, sErrData, iId, iOdl);

                    WypiszTabele(false);  // w trakcie - pokazujemy na raty, zeby cos sie dzialo
                }
            }

        }

        private void WypiszTabele(bool bShowZero)
        {
            if (App.moOdjazdy.Count() < 1)
            {
                if (bShowZero)
                    p.k.DialogBoxRes("resZeroKursow");
                return;
            }

            switch (p.k.GetSettingsInt("sortMode"))
            {
                case 1:  // stop/czas/dir
                        uiListItems.ItemsSource = from c in App.moOdjazdy.GetItems()
                                                  orderby c.Przyst, c.TimeSec, c.Kier
                                                  where c.bShow == true
                                                  select c;
                        break;
                case 2:  // dir/stop/czas
                        uiListItems.ItemsSource = from c in App.moOdjazdy.GetItems()
                                                  orderby c.Kier, c.Przyst, c.TimeSec
                                                  where c.bShow == true
                                                  select c;
                        break;
                case 3:   // czas/line
                        uiListItems.ItemsSource = from c in App.moOdjazdy.GetItems()
                                                  orderby c.TimeSec, c.iLinia
                                                  where c.bShow == true
                                                  select c;
                        break;
                default:
                        uiListItems.ItemsSource = from c in App.moOdjazdy.GetItems()
                                                  orderby c.iLinia, c.Kier, c.TimeSec
                                                  where c.bShow == true
                                                  select c;
                        break;
            }
        }



        private void SetSortMode(bool bInit, int iMode)
        {
            uiSortKier.IsChecked = false;
            uiSortLine.IsChecked = false;
            uiSortStop.IsChecked = false;
            uiSortCzas.IsChecked = false;

            if (bInit)
                iMode = p.k.GetSettingsInt("sortMode");

            switch (iMode)
            {
                case 0:  // line
                        uiSortLine.IsChecked = true;
                        break;
                case 1:  // stop
                        uiSortStop.IsChecked = true;
                        break;
                case 2:  // kier
                        uiSortKier.IsChecked = true;
                        break;
                default:
                        // czas
                        iMode = 3;
                        uiSortCzas.IsChecked = true;
                        break;
            }

            if (!bInit)
            {
                p.k.SetSettingsInt("sortMode", iMode);
                WypiszTabele(true);
            }
        }

        private void bSortByLine_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SetSortMode(false, 0);
        }

        private void bSortByStop_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SetSortMode(false, 1);
        }

        private void bSortByKier_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SetSortMode(false, 2);
        }

        private void bSortByCzas_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SetSortMode(false, 3);
        }

        private void uiShowStops_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

            // sender = grid
            // uiGrid.BorderThickness = 1
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            var oItem = oMFI.DataContext as JedenOdjazd;

            string sParam;
            sParam = oItem.Linia + "|" + oItem.Kier + "|" + oItem.Przyst;
            this.Frame.Navigate(typeof(Trasa), sParam);
        }

        private async void uiRawData_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            var oItem = oMFI.DataContext as JedenOdjazd;

            await p.k.DialogBox(oItem.sRawData);
        }

        private void uiExcludeKier_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            if (oMFI == null)
                return;
            var oItem = oMFI.DataContext as JedenOdjazd;
            if (oItem == null)
                return;

            App.moOdjazdy.FiltrWedleKierunku(true, oItem.Kier);
            WypiszTabele(true);
        }

        private void uiOnlyThisKier_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            if (oMFI == null)
                return;
            var oItem = oMFI.DataContext as JedenOdjazd;
            if (oItem == null)
                return;

            App.moOdjazdy.FiltrWedleKierunku(false, oItem.Kier);
            WypiszTabele(true);
        }
    }
}
