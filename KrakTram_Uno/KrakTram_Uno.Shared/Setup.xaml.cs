using System;
using System.Linq;
using vb14 = VBlib.pkarlibmodule14;
using static p.Extensions;
using System.Collections.Generic;

#if NETFX_CORE 
using pkar.UI.Configs;
#else
//using static p.Extensions;
#endif 

namespace KrakTram
{

    public sealed partial class Setup : Windows.UI.Xaml.Controls.Page
    {
        public Setup()
        {
            this.InitializeComponent();
        }

        private string msRunType;

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {// MAIN lub ODJAZD
            if (e is null)
                msRunType = "MAIN";
            else
                msRunType = e.Parameter.ToString();
        }

        private void bOk_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiMaxOdlSld.SetSettingsInt("maxOdl");
            uiWalkSpeedSld.SetSettingsInt("walkSpeed");
            uiAlsoNextSld.SetSettingsInt("alsoNext");

            vb14.SetSettingsInt("gpsPrec", VBlib.Setup.ConvertGpsPrecFromAndroid((int)uiGPSPrecSld.Value, p.k.GetPlatform("uwp")));

            //uiAlsoBus.SetSettingsBool("settingsAlsoBus");
            //uiAndroAutoTram.SetSettingsBool("androAutoTram");
            uiAndroAutoBus.SetSettingsBool("androAutoBus");
            this.Frame.GoBack();
        }

        private async void bLoadStops_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiReloadStop.IsEnabled = false;
            await App.CheckLoadStopListAsync(true);
            uiReloadStop.IsEnabled = true;
            eMaxOdl_Changed(null,null);
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

            // poniewaz dla Uno i tak nie ma precyzji GPS, mozna to usunac z ekranu robiąc miejsce
            //if (!p.k.GetPlatform("uwp"))
            //{
            //    uiGPSPrecSld.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //    uiGPSPrecTxt.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //}

            uiMaxOdlSld.GetSettingsInt("maxOdl"); //, 1000);
            uiWalkSpeedSld.GetSettingsInt("walkSpeed");//, 4);
            uiAlsoNextSld.GetSettingsInt("alsoNext"); //, 5);
            // Android: 100 m, bo <100 jest Accuracy.High, a ≥ 100 to juz bedzie .Medium
            uiGPSPrecSld.Value = VBlib.Setup.ConvertGpsPrecToAndroid(vb14.GetSettingsInt("gpsPrec", p.k.GetPlatform(75,100,75,75,75)), p.k.GetPlatform("uwp"));

            uiPositionLat.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiPositionLong.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiPositionName.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiPositionButt.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            uiPositionLat.Text = App.mPoint.Latitude.ToString();
            uiPositionLong.Text = App.mPoint.Longitude.ToString();
            //uiAlsoBus.GetSettingsBool("settingsAlsoBus");
            //uiAndroAutoTram.GetSettingsBool("androAutoTram");
            uiAndroAutoBus.GetSettingsBool("androAutoBus");
            if (!p.k.GetPlatform("uwp")) uiAndroAutoBus.Visibility = Windows.UI.Xaml.Visibility.Visible;

            if (msRunType != "MAIN") uiOpenPosPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;


            //VBlib.Setup.cosik();
        }

        private void ListaBliskichPrzystankowListView(pkar.BasicGeopos oPoint)
        {
            List<VBlib.BliskiStop> oItemy =
                VBlib.Setup.ListaBliskichPrzystankowListView(oPoint, 
                        uiMaxOdlSld.Value, uiWalkSpeedSld.Value, App.oStops.GetList("all"));

            if (uiListItems != null) uiListItems.ItemsSource = from c in oItemy orderby c.iOdl select c;
        }

    private async void eMaxOdl_Changed(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            // oPoint - albo narzucony, albo z GPS
            pkar.BasicGeopos oPoint;
            if (App.mPoint.IsEmpty())
                oPoint = await App.GetCurrentPointAsync();
            else
                oPoint = App.mPoint;

            ListaBliskichPrzystankowListView(oPoint);
        }

        private void ShowPositionPanel(bool bShow)
        {
            if(bShow)
            {
                uiPositionName.Visibility = Windows.UI.Xaml.Visibility.Visible ;
                // uiPositionFavButt.Visibility = Visibility.Collapsed
                // 20171019 - usuwamy wiecej, i jeszcze zmieniamy WebView wielkosc
                uiPositionLat.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiPositionLong.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiPositionButt.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiOpenPosPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiPositionButtCancel.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                uiPositionName.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                // uiPositionFavButt.Visibility = Visibility.Collapsed
                // 20171019 - usuwamy wiecej, i jeszcze zmieniamy WebView wielkosc
                uiPositionLat.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiPositionLong.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiPositionButt.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiOpenPosPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiPositionButtCancel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void uiPositCancel_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ShowPositionPanel(false);
        }


        private void uiPositOk_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // dodawanie nowego entry

            string sTxt = uiPositionName.Text;
            // sTxt = sTxt.Replace("*", "")
            // sTxt = sTxt.Replace("[", "")
            // sTxt = sTxt.Replace("]", "")

            if (pswd.TryInitPkarFav(sTxt))
            {
                App.oFavour.Save(false);
                vb14.SetSettingsBool("pkarmode", true, true); // roaming data
            }
            else
            {
                pkar.BasicGeopos basGeo = pkar.BasicGeopos.GetFromLink(uiPositionLat.Text);
                if (basGeo != null)
                {
                    uiPositionLong.Text = basGeo.Longitude.ToString();
                    uiPositionLat.Text = basGeo.Latitude.ToString();
                }

                if (sTxt.Length < 4)
                {
                    vb14.DialogBoxRes("resErrorNazwaZaKrotka");
                    return;
                }

                if(basGeo is null)
                {
                    double dLat, dLon;

                    if (!double.TryParse(uiPositionLong.Text, out dLon) || !double.TryParse(uiPositionLat.Text, out dLat))
                    {
                        vb14.DialogBoxRes("resBadFloat");
                        return;
                    }
                    basGeo = new pkar.BasicGeopos(dLat, dLon);
                }

                if(!basGeo.IsInsideKrakow())
                {
                    vb14.DialogBoxRes("resErrorPozaKrakowem");
                    return;
                }

                App.mPoint = basGeo;  // ustalamy to jako biezace wspolrzedne
                App.oFavour.Add(sTxt, App.mPoint, (int)uiMaxOdlSld.Value);
            }

            ShowPositionPanel(false);
            eMaxOdl_Changed(null, null);
        }

        private void uiGpsPrec_Changed(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (uiGPSPrecTxt == null)
                return;

            if(p.k.GetPlatform("uwp"))
                uiGPSPrecTxt.Text = uiGPSPrecSld.Value + " m";
            else
            {   // dla Android: skwantowany
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                if (uiGPSPrecSld.Value == 1) uiGPSPrecTxt.Text = "< 100 m";
                if (uiGPSPrecSld.Value == 2) uiGPSPrecTxt.Text = "normal";
                if (uiGPSPrecSld.Value == 3) uiGPSPrecTxt.Text = "> 500 m";
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            // musi od razu, żeby zaraz zaczęło działać (np. przy przestawianiu odleglosci od przystanku)
            vb14.SetSettingsInt("gpsPrec",VBlib.Setup.ConvertGpsPrecFromAndroid((int)uiGPSPrecSld.Value, p.k.GetPlatform("uwp")));
        }

        private void uiOpenPosPanel_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ShowPositionPanel(true);
        }

        //private void uiAlsoBus_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        //{
        //    uiAlsoBus.SetSettingsBool("settingsAlsoBus");
        //    eMaxOdl_Changed(null, null);
        //}
    }

    public class KonwersjaDouble2Text : Windows.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null) return "";
            double dTmp = (double)value;
            string sUnit = (string)parameter;
            if (sUnit == "kph") sUnit = "km/h";
            return dTmp.ToString() + " " + sUnit;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

}

