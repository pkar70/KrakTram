
using System;
using vb14 = VBlib.pkarlibmodule14;
using static p.Extensions;
using pkar.UI.Extensions;

namespace KrakTram
{

    public sealed partial class Opoznienia : Windows.UI.Xaml.Controls.Page
    {
        private static DateTime mdOpoznLastDate = System.DateTime.Now.AddDays(-5);
        private static VBlib.PrzystankiOpoznione _StopDelays;
        private static pkar.MpkWrap.OpoznieniaStat _StatTram;
        private static pkar.MpkWrap.OpoznieniaStat _StatBus;


        public Opoznienia()
        {
            this.InitializeComponent();
            _StopDelays = new VBlib.PrzystankiOpoznione(App.oStops.GetList("all"));
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.ProgRingInit(true, false);
        }

        private static void PokazDelayStat(pkar.MpkWrap.OpoznieniaStat oStat, Windows.UI.Xaml.Controls.TextBlock uiDelay, Windows.UI.Xaml.Controls.TextBlock uiData)
        {
            if (oStat.itemsCount == 0)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                uiDelay.Text = "--";
                uiData.Text = "--";
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                return;
            }

            uiDelay.Text = oStat.DelayAvg.ToString("####0.#") + " mins";
            string sTmp = $"({oStat.DelaySum}/{oStat.itemsCount}";
            if (oStat.DelayMax > 0)
                sTmp += $", max {oStat.DelayMax}";
            sTmp += ")";
            uiData.Text = sTmp;
        }

        private void PokazTotalDelay()
        {
            pkar.MpkWrap.OpoznieniaStat total = _StatTram.Clone() as pkar.MpkWrap.OpoznieniaStat;
            total.DelaySum += _StatBus.DelaySum;
            total.itemsCount += _StatBus.itemsCount;

            PokazDelayStat(total, uiTotalDelay, uiTotalCount);
        }

        private async void ReloadOpoznienia(bool isBus)
        {
            if (mdOpoznLastDate.AddMinutes(5) > DateTime.Now)
                if (!await this.DialogBoxYNAsync("Niedawno było, na pewno?"))
                    return;

            this.ProgRingShow(true);
            string sRet = await _StopDelays.OpoznieniaFromHttpAsync(isBus);
            this.ProgRingShow(false);

            if (sRet == "") return;

            vb14.ClipPut(sRet);

            // sygnalizacja kiedy bylo ostatnie
            mdOpoznLastDate = DateTime.Now;

            string msg = "";

            if (!isBus)
            {
                _StatTram = _StopDelays.OpoznieniaGetStat(isBus);
                PokazDelayStat(_StatTram, uiTramDelay, uiTramCount);
                msg = msg + "Tram: \n" + _StatTram.DumpAsJSON() + "\n";
            }
            else
            {
                _StatBus = _StopDelays.OpoznieniaGetStat(isBus);
                PokazDelayStat(_StatBus, uiBusDelay, uiBusCount);
                msg = msg + "Bus: \n" + _StatBus.DumpAsJSON() + "\n";
            }

            PokazTotalDelay();
        }

        private void uiTramReload_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ReloadOpoznienia(false);
        }

        private void uiBusReload_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ReloadOpoznienia(true);
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

            foreach (VBlib.PrzystanekOpoznienie oItem in _StopDelays.GetList())
            {
                if (oItem.delays.itemsCount == 0)
                    continue;
                if (oItem.IsBus && !bBus) continue;
                if (!oItem.IsBus && !bTram) continue;

                Windows.UI.Xaml.Shapes.Ellipse oNew = new Windows.UI.Xaml.Shapes.Ellipse();
                oNew.Height = 20;
                oNew.Width = 20;
                double dAvgDelay = 0;

                dAvgDelay = oItem.delays.DelayAvg;

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
                Windows.Devices.Geolocation.Geopoint oPoint = oItem.Geo.ToWinGeopoint();

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
            var oPosition = new Windows.Devices.Geolocation.BasicGeoposition();
            pkar.BasicGeopos.GetKrakowCenter().CopyTo(oPosition);

            uiMapka.Center = new Windows.Devices.Geolocation.Geopoint(oPosition);
            uiMapka.ZoomLevel = 12;
            // Uno tu protestuje Unimplemented, ale przecież to jest strona tylko dla mnie, a więc na UWP jedynie
#pragma warning disable Uno0001 // Uno type or member is not implemented
            uiMapka.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Road;
#pragma warning restore Uno0001 // Uno type or member is not implemented
        }
    }
}

