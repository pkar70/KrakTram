using System;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KrakTram
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Opoznienia : Windows.UI.Xaml.Controls.Page
    {
        public Opoznienia()
        {
            this.InitializeComponent();
        }

        private double mdDelayMinsTram = 0;
        private int miDelayCntTram = 0;
        private double mdDelayMinsBus = 0;
        private int miDelayCntBus = 0;

        private void Procesuje(bool bShow)
        {
            if (bShow)
            {
                double dVal;
                dVal = Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth) / 2;
                uiProcesuje.Width = dVal;
                uiProcesuje.Height = dVal;
                uiProcesuje.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiProcesuje.IsActive = true;
                uiMapka.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                uiProcesuje.IsActive = false;
                uiProcesuje.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiMapka.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private void PokazDelayStat(int iDelay, int iCount, int iMaxDelay, Windows.UI.Xaml.Controls.TextBlock uiDelay, Windows.UI.Xaml.Controls.TextBlock uiData)
        {
            if (iCount == 0)
            {
                uiDelay.Text = "--";
                uiData.Text = "--";
                return;
            }

            double dDelay = iDelay / (double)iCount;
            uiDelay.Text = dDelay.ToString("####0.#") + " mins";
            string sTmp = "(" + iDelay + "/" + iCount;
            if (iMaxDelay > 0)
                sTmp = sTmp + ", max " + iMaxDelay;
            sTmp = sTmp + ")";
            uiData.Text = sTmp;
        }

        private void PokazTotalDelay()
        {
            PokazDelayStat((int)(mdDelayMinsTram + mdDelayMinsBus), miDelayCntTram + miDelayCntBus, -1, uiTotalDelay, uiTotalCount);
        }

        private async void ReloadOpoznienia(int iTyp)
        {
            int iDelay = 0;
            int iCount = 0;
            int iMaxDelay = 0;

            Procesuje(true);
            bool bRet = await App.oStops.OpoznieniaFromHttpAsync(iTyp);
            if (bRet)
                bRet = App.oStops.OpoznieniaGetStat(iTyp, ref iDelay, ref iCount, ref iMaxDelay);
            Procesuje(false);
            if (!bRet)
                return;

            if (iTyp == 1) // tram
            {
                mdDelayMinsTram = iDelay;
                miDelayCntTram = iCount;
                PokazDelayStat((int)mdDelayMinsTram, miDelayCntTram, iMaxDelay, uiTramDelay, uiTramCount);
            }
            else // bus
            {
                mdDelayMinsBus = iDelay;
                miDelayCntBus = iCount;
                PokazDelayStat((int)mdDelayMinsBus, miDelayCntBus, iMaxDelay, uiBusDelay, uiBusCount);
            }
            PokazTotalDelay();
        }

        private void uiTramReload_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ReloadOpoznienia(1);
        }

        private void uiBusReload_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ReloadOpoznienia(2);
        }

        private void uiTotalReshow_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // 4. info: aktualna średnia, liczba wpisów, liczba wpisów z minutami (a nie rozkładowe)
            // 5. pokazywanie na mapie, kółka z kolorami
            App.oStops.OpoznieniaDoMapy(uiTramCB.IsChecked.GetValueOrDefault(false), uiBusCB.IsChecked.GetValueOrDefault(false), uiMapka);
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
            // Uno tu protestuje Unimplemented, ale przecież to jest strona tylko dla mnie, a więc na UWP jedynie
            uiMapka.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Road;
        }
    }
}
