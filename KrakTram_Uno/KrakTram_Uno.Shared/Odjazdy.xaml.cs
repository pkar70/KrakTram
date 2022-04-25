using System;
using System.Linq;
using Windows.UI.Xaml.Data;
using vb14 = VBlib.pkarlibmodule14;


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
            await WczytajPokazDaneAsync(false);
        }

        private async void uiGetData_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await WczytajPokazDaneAsync(true);
        }


        private async System.Threading.Tasks.Task WczytajPokazDaneAsync(bool bForce)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                await vb14.DialogBoxResAsync("resErrorNoNetwork");
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
                App.mMaxOdl = vb14.GetSettingsInt("maxOdl");
                // ustaw wspolrzedne
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                uiWorking.Text = "o";
                App.mPoint = await App.GetCurrentPointAsync();
                uiWorking.Text = " ";
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
            else
            {
            }

            App.moOdjazdy.Clear();

            // ustalony jest skąd szukamy przystanków i jak daleko
            await WczytajTabliczkiWOdleglosci(App.mPoint, App.mMaxOdl);

            App.moOdjazdy.OdfiltrujMiedzytabliczkowo();
        }

        private async System.Threading.Tasks.Task WczytajTabliczkiWOdleglosci(Windows.Devices.Geolocation.BasicGeoposition oPos , double dOdl)
        {
            int iWorking = 0;
            int iOdl;

            string sFilter = "tram";
            if (vb14.GetSettingsBool("settingsAlsoBus"))
                sFilter = "all";

            foreach (VBlib.Przystanek oNode in App.oStops.GetList(sFilter))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                uiWorking.Text = ".";
                iOdl = (int)oPos.DistanceTo(oNode.Lat, oNode.Lon);
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
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                                break;
                    }

                    string sErrData = oNode.Name;
                    if (vb14.GetSettingsBool("settingsAlsoBus"))
                    {
                        if (oNode.Cat == "bus")
                            sErrData += " (bus)";
                        else
                            sErrData += " (tram)";
                    }

                    int iId;
                    if(!int.TryParse(oNode.id, out iId)) iId=0;
                    await App.moOdjazdy.WczytajTabliczke(oNode.Cat, sErrData, iId, iOdl, App.mSpeed,
                        vb14.GetSettingsBool("pkarmode", p.k.IsThisMoje()));

                    WypiszTabele(false);  // w trakcie - pokazujemy na raty, zeby cos sie dzialo
                }
            }

        }

        private void WypiszTabele(bool bShowZero)
        {
            if (App.moOdjazdy.Count() < 1)
            {
                if (bShowZero)
                    vb14.DialogBoxRes("resZeroKursow");
                return;
            }

            switch (vb14.GetSettingsInt("sortMode"))
            {
                case 1:  // stop/czas/dir
                        uiListItems.ItemsSource = (from c in App.moOdjazdy.GetItems()
                                                  orderby c.Przyst, c.TimeSec, c.Kier
                                                  where c.bShow == true
                                                  select c).ToList();
                        break;
                case 2:  // dir/stop/czas
                        uiListItems.ItemsSource = (from c in App.moOdjazdy.GetItems()
                                                  orderby c.Kier, c.Przyst, c.TimeSec
                                                  where c.bShow == true
                                                  select c).ToList();
                        break;
                case 3:   // czas/line
                        uiListItems.ItemsSource = (from c in App.moOdjazdy.GetItems()
                                                  orderby c.TimeSec, c.iLinia
                                                  where c.bShow == true
                                                  select c).ToList();
                        break;
                default:
                        uiListItems.ItemsSource = (from c in App.moOdjazdy.GetItems()
                                                  orderby c.iLinia, c.Kier, c.TimeSec
                                                  where c.bShow == true
                                                  select c).ToList();
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
                iMode = vb14.GetSettingsInt("sortMode");

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
                vb14.SetSettingsInt("sortMode", iMode);
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

        private void GoShowStops(VBlib.JedenOdjazd oItem)
        {
            string sParam;
            sParam = $"{oItem.Linia}|{oItem.Kier}|{oItem.Przyst}";
            this.Frame.Navigate(typeof(Trasa), sParam);
        }

        private void uiShowStops_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // sender = grid
            // uiGrid.BorderThickness = 1
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            var oItem = oMFI.DataContext as VBlib.JedenOdjazd;
            GoShowStops(oItem);
        }

        private async void uiRawData_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            var oItem = oMFI.DataContext as VBlib.JedenOdjazd;

            await vb14.DialogBoxAsync(oItem.sRawData);
        }

        private void uiExcludeKier_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            if (oMFI == null)
                return;
            var oItem = oMFI.DataContext as VBlib.JedenOdjazd;
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
            var oItem = oMFI.DataContext as VBlib.JedenOdjazd;
            if (oItem == null)
                return;

            App.moOdjazdy.FiltrWedleKierunku(false, oItem.Kier);
            WypiszTabele(true);
        }

        private void uiLine_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {// shortcut: przeskok do przystanków (dla Windows)
            VBlib.JedenOdjazd oItem;

            var oGrid = sender as Windows.UI.Xaml.Controls.Grid;
            if (oGrid is null)
            {
                var oTBlock = sender as Windows.UI.Xaml.Controls.TextBlock;
                oItem = oTBlock.DataContext as VBlib.JedenOdjazd;
            }
            else
                oItem = oGrid.DataContext as VBlib.JedenOdjazd;

            GoShowStops(oItem);
        }

    }

    public class KonwersjaVisibility : Windows.UI.Xaml.Data.IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            bool bTemp = System.Convert.ToBoolean(value);
            if (bTemp)
            {
                return Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                return Windows.UI.Xaml.Visibility.Collapsed;
            }
        }


        // ' ConvertBack is not implemented for a OneWay binding.
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


}
