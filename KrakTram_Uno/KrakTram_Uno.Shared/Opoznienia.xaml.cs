using System;
using vb14 = VBlib.pkarlibmodule14;



namespace KrakTram
{


    public sealed partial class Opoznienia : Windows.UI.Xaml.Controls.Page
    {
        private static DateTime mdOpoznLastDate = System.DateTime.Now.AddDays(-5);

        public Opoznienia()
        {
            this.InitializeComponent();
        }

        private double mdDelayMinsTram = 0;
        private int miDelayCntTram = 0;
        private double mdDelayMinsBus = 0;
        private int miDelayCntBus = 0;


        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.ProgRingInit(true, false);
        }

        private static void PokazDelayStat(int iDelay, int iCount, int iMaxDelay, Windows.UI.Xaml.Controls.TextBlock uiDelay, Windows.UI.Xaml.Controls.TextBlock uiData)
        {
            if (iCount == 0)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                uiDelay.Text = "--";
                uiData.Text = "--";
#pragma warning restore CA1303 // Do not pass literals as localized parameters
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

            // policzenie opóźnień, b0 = bus, b1 = tram
            if (mdOpoznLastDate.AddMinutes(5) > DateTime.Now)
                if (!await vb14.DialogBoxYNAsync("Niedawno było, na pewno?"))
                    return;

            this.ProgRingShow(true);

            
            string sRet = await App.oStops.OpoznieniaFromHttpAsync(iTyp);
            if (sRet == "")
            {
                vb14.DialogBox(App.oStops.sLastError);
                return;
            }
            p.k.ClipPut(sRet);

            // sygnalizacja kiedy bylo ostatnie
            mdOpoznLastDate = DateTime.Now;
            
            bool bRet = App.oStops.OpoznieniaGetStat(iTyp, ref iDelay, ref iCount, ref iMaxDelay);
            this.ProgRingShow(false);
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

            // ze wzgledu na VBlibek z Przystanki :)
            OpoznieniaDoMapy(uiTramCB.IsChecked.GetValueOrDefault(false), uiBusCB.IsChecked.GetValueOrDefault(false), uiMapka);
        }

        public int OpoznieniaDoMapy(bool bTram, bool bBus, Windows.UI.Xaml.Controls.Maps.MapControl oMapCtrl)
        {
            // iType: 1: tram, 2:bus, 3: wszystko (ale nie 'other')
            if (oMapCtrl == null)
                return 0;

            // https://docs.microsoft.com/en-us/windows/uwp/maps-and-location/display-poi

            int iCnt = 0;

            Windows.UI.Xaml.Media.SolidColorBrush oBrush2min, oBrush3min, oBrush4min, oBrush5min;
            oBrush2min = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Yellow);
            oBrush3min = oBrush2min;
            oBrush4min = oBrush2min;
            oBrush5min = oBrush2min;
            oBrush2min.Opacity = 0.3;
            oBrush3min.Opacity = 0.4;
            oBrush4min.Opacity = 0.5;
            oBrush5min.Opacity = 0.6;
            Windows.UI.Xaml.Media.SolidColorBrush oBrush10min, oBrush20min, oBrushMaxmin;
            oBrush10min = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.OrangeRed);
            oBrush20min = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
            oBrushMaxmin = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.DarkRed);
            oBrush10min.Opacity = 0.5;
            oBrush20min.Opacity = 0.5;
            oBrushMaxmin.Opacity = 0.5;


            foreach (VBlib.Przystanek oItem in App.oStops.GetList("all"))
            {
                if (oItem.iEntriesCount == 0)
                    continue;
                switch (oItem.Cat)
                {
                    case "bus":
                        if (!bBus)
                            continue;
                        break;
                    case "tram":
                        if (!bTram)
                            continue;
                        break;
                    default:
                        continue;
                }

                Windows.UI.Xaml.Shapes.Ellipse oNew = new Windows.UI.Xaml.Shapes.Ellipse();
                oNew.Height = 20;
                oNew.Width = 20;
                double dAvgDelay = 0;

                dAvgDelay = oItem.iSumDelay / (double)oItem.iEntriesCount;

                if (dAvgDelay < 1)
                    continue;

                if (dAvgDelay > 2)
                {
                    if (dAvgDelay > 3)
                    {
                        if (dAvgDelay > 4)
                        {
                            if (dAvgDelay > 5)
                            {
                                if (dAvgDelay > 10)
                                {
                                    if (dAvgDelay > 20)
                                        oNew.Fill = oBrushMaxmin;
                                    else
                                        oNew.Fill = oBrush20min;
                                }
                                else
                                    oNew.Fill = oBrush10min;
                            }
                            else
                                oNew.Fill = oBrush5min;
                        }
                        else
                            oNew.Fill = oBrush4min;
                    }
                    else
                        oNew.Fill = oBrush3min;
                }
                else
                    oNew.Fill = oBrush2min;

                Windows.Devices.Geolocation.BasicGeoposition oPosition;
                oPosition = new Windows.Devices.Geolocation.BasicGeoposition();
                oPosition.Latitude = oItem.Lat;
                oPosition.Longitude = oItem.Lon;
                Windows.Devices.Geolocation.Geopoint oPoint;
                oPoint = new Windows.Devices.Geolocation.Geopoint(oPosition);

                iCnt += 1;
                oMapCtrl.Children.Add(oNew);

                // shared member - ale skąd wie jaka mapa? nie można dwu wyświetlić?
                Windows.UI.Xaml.Controls.Maps.MapControl.SetLocation(oNew, oPoint);
                Windows.UI.Xaml.Controls.Maps.MapControl.SetNormalizedAnchorPoint(oNew, new Windows.Foundation.Point(0.5, 0.5));
            }

            return iCnt;
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
#pragma warning disable Uno0001 // Uno type or member is not implemented
            uiMapka.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Road;
#pragma warning restore Uno0001 // Uno type or member is not implemented
        }
    }
}
