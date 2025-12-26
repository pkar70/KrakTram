using System;
using System.Linq;
using Windows.UI.Xaml.Data;
using vb14 = VBlib.pkarlibmodule14;
using pkar.UI.Extensions;
using Windows.UI.Xaml.Documents;
using System.Collections.Specialized;
using VBlib;


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
            this.ProgRingInit(true, false);
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
                await this.MsgBoxAsync("res:resErrorNoNetwork");
                return;
            }

            uiGoTtssBar.IsEnabled = false;

            this.ProgRingShow(true);
            if (App.moOdjazdy.Count() == 0 || bForce)
                await CzytanieTabliczekAsync();
            this.ProgRingShow(false);

            WypiszTabele(true);

            uiGoTtssBar.IsEnabled = true;
        }

        private async System.Threading.Tasks.Task CzytanieTabliczekAsync()
        {
            if (App.mbGoGPS)
            {
                // wedle GPS
                App.mMaxOdl = vb14.GetSettingsInt("maxOdl");
                // ustaw wspolrzedne
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                uiWorking.Text = "o";
                this.ProgRingSetText("GPS");
                App.mPoint = await App.GetCurrentPointAsync();
                uiWorking.Text = " ";
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
            else
            {
            }

            App.moOdjazdy.Clear();

            // ustalony jest skąd szukamy przystanków i jak daleko
            await WczytajTabliczkiWOdleglosciAsync(App.mPoint, App.mMaxOdl);

            App.moOdjazdy.OdfiltrujMiedzytabliczkowo();
        }

        private async System.Threading.Tasks.Task WczytajTabliczkiWOdleglosciAsync(pkar.BasicGeopos oPos , double dOdl)
        {
            int iWorking = 0;
            int iOdl;

            string sFilter = "all";

            foreach (pkar.MpkWrap.Przystanek oNode in App.oStops.GetList(sFilter))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                uiWorking.Text = ".";
                iOdl = (int)oPos.DistanceTo(oNode.Geo);
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
                        if (oNode.IsBus)
                            sErrData += " (bus)";
                        else
                            sErrData += " (tram)";

                    this.ProgRingSetText(oNode.Name + " " + (oNode.IsBus?"B":"T"));

                    await App.moOdjazdy.WczytajTabliczke(oNode.IsBus, sErrData, oNode, iOdl, App.mSpeed,
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
                    this.MsgBox("res:resZeroKursow");
                return;
            }

            switch (vb14.GetSettingsInt("sortMode"))
            {
                case 1:  // stop/czas/dir
                        uiListItems.ItemsSource = (from c in App.moOdjazdy.GetItems()
                                                  orderby c.Odjazd.Przyst, c.Odjazd.TimeSec, c.Odjazd.Kier
                                                  where c.bShow == true
                                                  select c).ToList();
                        break;
                case 2:  // dir/stop/czas
                        uiListItems.ItemsSource = (from c in App.moOdjazdy.GetItems()
                                                  orderby c.Odjazd.Kier, c.Odjazd.Przyst, c.Odjazd.TimeSec
                                                  where c.bShow == true
                                                  select c).ToList();
                        break;
                case 3:   // czas/line
                        uiListItems.ItemsSource = (from c in App.moOdjazdy.GetItems()
                                                  orderby c.Odjazd.TimeSec, c.Odjazd.iLinia
                                                  where c.bShow == true
                                                  select c).ToList();
                        break;
                default:
                        uiListItems.ItemsSource = (from c in App.moOdjazdy.GetItems()
                                                  orderby c.Odjazd.iLinia, c.Odjazd.Kier, c.Odjazd.TimeSec
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

        private void uiModelInfo_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oFI = sender as Windows.UI.Xaml.FrameworkElement;
            var oItem = oFI?.DataContext as VBlib.JedenOdjazd;
            if (oItem is null) return;

            oItem.vehicleInfoUri.OpenBrowser();
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

        private void GoShowStops(pkar.MpkWrap.Odjazd oItem)
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
            var oItem = oMFI?.DataContext as VBlib.JedenOdjazd;
            GoShowStops(oItem.Odjazd);
        }

        private async void uiDelayStats_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            var oItem = oMFI?.DataContext as VBlib.JedenOdjazd;
            if (oItem is null) return;

            var statsy = await App.moOdjazdy.GetDelayStats(oItem.isBus, oItem.stopId);
            this.MsgBox(statsy.DumpAsJSON());
        }

        private  void uiRawData_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            var oItem = oMFI?.DataContext as VBlib.JedenOdjazd;
            if (oItem is null) return;

            this.MsgBox(oItem.Odjazd.sRawData);
        }

        private void uiScheduled_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            var oItem = oMFI?.DataContext as VBlib.JedenOdjazd;

            // .vehicleId dla Zbiorkom, .tripId dla TTSS
            string callParam = oItem.Odjazd.vehicleId + "|" + oItem.Odjazd.Przyst;
            if (oItem.Odjazd.Linia.Length > 2) callParam += "|BUS";

            this.Frame.Navigate(typeof(ScheduledTrasa), callParam);
        }

        private void uiExcludeKier_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            var oItem = oMFI?.DataContext as VBlib.JedenOdjazd;
            if (oItem == null)
                return;

            App.moOdjazdy.FiltrWedleKierunku(true, oItem.Odjazd.Kier);
            WypiszTabele(true);
        }

        private void uiOnlyThisKier_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            var oItem = oMFI?.DataContext as VBlib.JedenOdjazd;
            if (oItem == null)
                return;

            App.moOdjazdy.FiltrWedleKierunku(false, oItem.Odjazd.Kier);
            WypiszTabele(true);
        }

        private void uiLine_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {// shortcut: przeskok do przystanków (dla Windows)

            var oFE = sender as Windows.UI.Xaml.FrameworkElement;
            var oItem = oFE.DataContext as VBlib.JedenOdjazd;

            GoShowStops(oItem.Odjazd);
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

    public class KonwersjaHideNoReal : Windows.UI.Xaml.Data.IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            string temp = System.Convert.ToString(value);
            if (temp.Contains("ERR"))
            {
                return Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                return Windows.UI.Xaml.Visibility.Visible;
            }
        }


        // ' ConvertBack is not implemented for a OneWay binding.
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    // statyczne wartości przyjmujemy
    public class KonwersjaWidth : Windows.UI.Xaml.Data.IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {

            int temp = System.Convert.ToInt16(parameter);

            if (temp == 3)
            {
                return MainPage.widthCol3;
            }
            else
            {
                return MainPage.widthCol0;
            }
        }


        // ' ConvertBack is not implemented for a OneWay binding.
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    
    public class KonwersjaInfoUriVisibility : Windows.UI.Xaml.Data.IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            Uri temp = value as Uri;
            if (temp is null)
            {
                return Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                return Windows.UI.Xaml.Visibility.Visible;
            }
        }


        // ' ConvertBack is not implemented for a OneWay binding.
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class KonwersjaInwalidaVisibility : Windows.UI.Xaml.Data.IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            int temp = System.Convert.ToInt32(value);
            if (temp < 1)
            {
                return Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                return Windows.UI.Xaml.Visibility.Visible;
            }
        }


        // ' ConvertBack is not implemented for a OneWay binding.
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class KonwersjaInwalidaOpacity : Windows.UI.Xaml.Data.IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            int temp = System.Convert.ToInt32(value);
            switch(temp)
            {
                case 1: return 0.5;
                case 2: return 1;
            }
            return 0;
        }


        // ' ConvertBack is not implemented for a OneWay binding.
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

}
