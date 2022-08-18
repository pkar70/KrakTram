
using System.Linq;
using Windows.UI.Xaml.Controls;
using vb14 = VBlib.pkarlibmodule14;
using static p.Extensions;

namespace KrakTram
{
    public sealed partial class Zmiany : Windows.UI.Xaml.Controls.Page
    {

        private VBlib.Zmiany oVbLib = new VBlib.Zmiany(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path);

        public Zmiany()
        {
            this.InitializeComponent();
        }

        private void uiClose_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.GoBack();
        }

        private async void uiSearch_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            string sMask = await vb14.DialogBoxInputResAsync("msgSearchReroute");

            if (string.IsNullOrEmpty(sMask))
            {
                uiLista.ItemsSource = oVbLib.moItemy;
            }
            else
            {
                sMask = sMask.ToLower();
                uiLista.ItemsSource = from c in oVbLib.moItemy
                                      where c.sInfo.ToLower().Contains(sMask)
                                      select c;
            }

            if (uiLista.Items.Count == 1)
                PokazObjazd((uiLista.Items.ElementAt(0) as VBlib.JednaInfo).sInfo);
        }

    

    private async void uiRefresh_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiReload.IsEnabled = false;
            await WczytajZmianyAsync();
            uiLista.ItemsSource = oVbLib.moItemy; // from c in moLista;
        }

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

            this.ProgRingInit(true, false);

            if (!TryLoadCache())
            {
                this.ProgRingShow(true);
                await WczytajZmianyAsync();
                this.ProgRingShow(false);
            }

#if !__ANDROID__
            // uiRowInfo.Height = new Windows.UI.Xaml.GridLength { GridUnitType= Windows.UI.Xaml.GridUnitType.Pixel,  }
            uiRowInfo.MaxHeight = 10;
#endif 
            if (oVbLib.moItemy.Count < 1)
                return;
            uiLista.ItemsSource = oVbLib.moItemy; //  From c In moLista ' Order By c.iMin

        }


        private bool TryLoadCache()
        {
            string sRet = oVbLib.TryLoadCache();
            if(sRet == "") return false;

            uiFileDate.Text = sRet;
            uiReload.IsEnabled = true;

            return true;
        }

        private async System.Threading.Tasks.Task<bool> WczytajZmianyAsync()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                await vb14.DialogBoxResAsync("resErrorNoNetwork");
                return false;
            }

            string sRet = await oVbLib.WczytajZmiany();
            if(sRet != "")
            {
                await vb14.DialogBoxAsync(sRet);
                return false;
            }


            uiFileDate.Text = "";
            uiReload.IsEnabled = false;
            oVbLib.Save();
            return true;
        }

        private void PokazObjazd(string sHtml)
        {
#if !__ANDROID__
            uiRowInfo.MaxHeight = 1000;
#endif 

            sHtml = @"<html>
                <head><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
                <body>" + sHtml + "</body></html>";

            uiWebView.NavigateToString(sHtml);

        }
        private void uiPokaz_Click(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            VBlib.JednaInfo oItem = (sender as Windows.UI.Xaml.Controls.Grid).DataContext as VBlib.JednaInfo;

            string sHtml = $"<p>{oItem.sInfo}<p><a href='{oItem.sLink}'>{vb14.GetLangString("resZmianyLink")}</a>";
            PokazObjazd(sHtml);
        }

        private void uiWebNavStart(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri == null)
                return;

            args.Cancel = true;

            Windows.System.Launcher.LaunchUriAsync(args.Uri);
        }

    }
}
