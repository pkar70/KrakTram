
using System.Linq;
using Windows.UI.Xaml.Controls;
using vb14 = VBlib.pkarlibmodule14;
//using static p.Extensions;
using pkar.UI.Extensions;

namespace KrakTram
{
    public sealed partial class Zmiany : Windows.UI.Xaml.Controls.Page
    {

        private pkar.MpkWrap.Zmiany oNuget = new pkar.MpkWrap.Zmiany(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path);

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
                uiLista.ItemsSource = oNuget.GetList();
            }
            else
            {
                sMask = sMask.ToLower();
                uiLista.ItemsSource = from c in oNuget.GetList()
                                      where c.sInfo.ToLower().Contains(sMask)
                                      select c;
            }

            if (uiLista.Items.Count == 1)
                PokazObjazd((uiLista.Items.ElementAt(0) as pkar.MpkMain.MpkZmiana).sInfo);
        }

    

    private async void uiRefresh_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiReload.IsEnabled = false;

            this.ProgRingShow(true);

            bool bRet = await oNuget.LoadOrImport(true, p.k.NetIsIPavailable(false));
            this.ProgRingShow(false);

            if (!bRet) return;

            uiFileDate.Text = oNuget.GetFileDate().ToString("yyyy.MM.dd");
            uiLista.ItemsSource = oNuget.GetList(); // from c in moLista;
        }

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiReload.IsEnabled = p.k.NetIsIPavailable(false);

            this.ProgRingInit(true, false);

            this.ProgRingShow(true);
            await oNuget.LoadOrImport(false, p.k.NetIsIPavailable(false));
            uiFileDate.Text = oNuget.GetFileDate().ToString("yyyy.MM.dd");

            this.ProgRingShow(false);

#if !__ANDROID__
            // uiRowInfo.Height = new Windows.UI.Xaml.GridLength { GridUnitType= Windows.UI.Xaml.GridUnitType.Pixel,  }
            uiRowInfo.MaxHeight = 10;
#endif 
            if (oNuget.Count() < 1)
                return;
            uiLista.ItemsSource = oNuget.GetList(); //  From c In moLista ' Order By c.iMin

        }

#if false
        private bool TryLoadCache()
        {
            if(!oNuget.Load()) return false;

            uiFileDate.Text = oNuget.GetFileDate().ToString("yyyy.MM.dd");
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

            string sRet = await oNuget.LoadOrImport() oVbLib.WczytajZmiany();
            if(sRet != "")
            {
                await vb14.DialogBoxAsync(sRet);
                return false;
            }


            uiFileDate.Text = "";
            uiReload.IsEnabled = false;
            oNuget.Save();
            return true;
        }
#endif

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
            pkar.MpkMain.MpkZmiana oItem = (sender as Windows.UI.Xaml.Controls.Grid).DataContext as pkar.MpkMain.MpkZmiana;

            string sHtml = $"<p>{oItem.sInfo}<p><a href='{oItem.sLink}'>{vb14.GetLangString("resZmianyLink")}</a>";
            PokazObjazd(sHtml);
        }

        private void uiWebNavStart(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri == null)
                return;

            args.Cancel = true;

            args.Uri.OpenBrowser();
            // Windows.System.Launcher.LaunchUriAsync(args.Uri);
        }

    }
}
