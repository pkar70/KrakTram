
using System.Linq;
using vb14 = VBlib.pkarlibmodule14;


namespace KrakTram
{

    public sealed partial class Trasa : Windows.UI.Xaml.Controls.Page
    {

        private VBlib.Trasa inVb = null;
        private string msLinia = "";

        public Trasa()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            string msKier = "";
            string msStop = "";

            if (e is null)
                msLinia = "50";
            else
            {
                string[] aParams = e.Parameter.ToString().Split('|');
                if (aParams.GetUpperBound(0) > -1)
                    msLinia = aParams[0];
                if (aParams.GetUpperBound(0) > 0)
                    msKier = aParams[1];
                if (aParams.GetUpperBound(0) > 1)
                    msStop = aParams[2];
            }

            inVb = new VBlib.Trasa(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, msLinia, msKier, msStop );
        }


        private async void PrepareTrasa(bool bForceRefresh)
        {
            this.ProgRingInit(true, false);
            string sRet = await inVb.PrepareTrasa(bForceRefresh, p.k.NetIsIPavailable(false));
            this.ProgRingShow(false);

            if (!sRet.StartsWith("OK"))
            {
                vb14.DialogBox(sRet);
                return;
            }

            if(bForceRefresh)
            { // jeśli było wymuszenie wczytania, ale nic nie wczytało, to nie przerysowuj
                if (inVb.moItemy.Count < 1) return;
            }

            sRet = sRet.Replace("OK", ""); // data pliku cache
            uiFileDate.Text = sRet; // data pliku cache
            if (sRet != "") uiReload.IsEnabled = false; // ma sens tylko wtedy gdy jest plik z cache

            uiListStops.ItemsSource = from c in inVb.moItemy
                                      orderby c.iMin
                                      select c;

        }
        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiTitle.Text = vb14.GetLangString("resTrasa") + " " + msLinia;
            PrepareTrasa(false);
        }


        private void ShowTabliczka(string sStop)
        {
            foreach (VBlib.Przystanek oStop in App.oStops.GetList("all"))
            {
                if (oStop.Name == sStop)
                {
                    App.mbGoGPS = false;
                    App.mMaxOdl = vb14.GetSettingsInt("treatAsSameStop");
                    App.mPoint = p.k.NewBasicGeoposition(oStop.Lat, oStop.Lon);
                    App.moOdjazdy.Clear();
                    this.Frame.Navigate(typeof(Odjazdy));
                }
            }
        }

        private void uiClose_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }


        private void GoPrzystanek(VBlib.JedenStop oItem)
        {
            if (oItem == null) return;
            ShowTabliczka(inVb.NazwaBezSlupka(oItem.Przyst));
        }


        private void uiGoPrzystanek_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Windows.UI.Xaml.Controls.MenuFlyoutItem oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            if (oMFI == null) return;
            GoPrzystanek(oMFI.DataContext as VBlib.JedenStop);
        }

        private void uiRefresh_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiReload.IsEnabled = false;
            PrepareTrasa(true);
        }

        private void uiGoTabliczka_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        { //shortcut do tabliczki (dla Windows)
            var oGrid = sender as Windows.UI.Xaml.Controls.Grid;
            if (oGrid == null) return;
            GoPrzystanek(oGrid.DataContext as VBlib.JedenStop);
        }
    }
}