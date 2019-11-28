using System;
//using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using Windows.Foundation;
//using Windows.Foundation.Collections;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Controls;
//using Windows.UI.Xaml.Controls.Primitives;
//using Windows.UI.Xaml.Data;
//using Windows.UI.Xaml.Input;
//using Windows.UI.Xaml.Media;
//using Windows.UI.Xaml.Navigation;

//using System;
//using System.IO;
//using System.Threading.Tasks;
//using Microsoft.VisualBasic;

public partial class JednaInfo
{
    public string sLinie { get; set; }
    public string sCzas { get; set; }
    public string sTytul { get; set; }
    public string sInfo { get; set; }
}

namespace KrakTram
{
    public sealed partial class Zmiany : Windows.UI.Xaml.Controls.Page
    {
        private System.Collections.ObjectModel.Collection<JednaInfo> moLista;

        private const string FILENAME = "zmiany.xml";

        /// <summary>
        /// An empty page that can be used on its own or navigated to within a Frame.
        /// </summary>

        public Zmiany()
        {
            this.InitializeComponent();
        }

        private void uiClose_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private async void uiSearch_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            string sMask = await p.k.DialogBoxInput("msgSearchReroute");

            if (string.IsNullOrEmpty(sMask))
            {
                uiLista.ItemsSource = moLista;
            }
            else
            {
                sMask = sMask.ToLower();
                uiLista.ItemsSource = from c in moLista
                                      where c.sInfo.ToLower().Contains(sMask)
                                      select c;
            }

            if (uiLista.Items.Count == 1)
                PokazObjazd((uiLista.Items.ElementAt(0) as JednaInfo).sInfo);
        }

    

    private async void uiRefresh_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiReload.IsEnabled = false;
            await WczytajTrase();
            uiLista.ItemsSource = moLista; // from c in moLista;
        }

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!await TryLoadCache())
            {
                double dVal;
                dVal = Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth) / 2;
                uiProcesuje.Width = dVal;
                uiProcesuje.Height = dVal;
                uiProcesuje.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiProcesuje.IsActive = true;

                await WczytajTrase();

                uiProcesuje.IsActive = false;
                uiProcesuje.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

#if !__ANDROID__
            // uiRowInfo.Height = new Windows.UI.Xaml.GridLength { GridUnitType= Windows.UI.Xaml.GridUnitType.Pixel,  }
            uiRowInfo.MaxHeight = 10;
#endif 
            if (moLista.Count < 1)
                return;
            uiLista.ItemsSource = moLista; //  From c In moLista ' Order By c.iMin

        }


        private async System.Threading.Tasks.Task<bool> TryLoadCache()
        {
            Windows.Storage.StorageFile oObj;
            oObj = (await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(FILENAME)) as Windows.Storage.StorageFile;
            if (oObj == null)
                return false;

            Windows.Storage.StorageFile oFile = oObj as Windows.Storage.StorageFile;

            if (oFile.DateCreated.AddDays(14) < DateTime.Now)
                return false;    // za stare
            uiFileDate.Text = oFile.DateCreated.ToString("dd/MM/yyyy");

            System.Xml.Serialization.XmlSerializer oSer;
            oSer = new System.Xml.Serialization.XmlSerializer(typeof(System.Collections.ObjectModel.Collection<JednaInfo>));
            Stream oStream = await oFile.OpenStreamForReadAsync();
            try
            {
                System.Xml.XmlReader oXmlReader = System.Xml.XmlReader.Create(oStream);
                moLista = oSer.Deserialize(oXmlReader) as System.Collections.ObjectModel.Collection<JednaInfo>;
                oXmlReader.Dispose();
            }
            catch
            {
                return false;
            }

            uiReload.IsEnabled = true;

            return true;
        }
        private async System.Threading.Tasks.Task Save()
        {
            Windows.Storage.StorageFile oFile;
            oFile = (await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(FILENAME)) as Windows.Storage.StorageFile;
            if (oFile != null)
                await oFile.DeleteAsync();
            // bez tego kasowania create timestamp jest stary!

            oFile = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync(FILENAME, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            if (oFile == null)
                return;

            System.Xml.Serialization.XmlSerializer oSer;
            oSer = new System.Xml.Serialization.XmlSerializer(typeof(System.Collections.ObjectModel.Collection<JednaInfo>));
            Stream oStream = await oFile.OpenStreamForWriteAsync();
            oSer.Serialize(oStream, moLista);
            oStream.Dispose();   // == fclose
        }

        private async System.Threading.Tasks.Task<bool> WczytajTrase()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                await p.k.DialogBoxRes("resErrorNoNetwork");
                return false;
            }

            System.Net.Http.HttpClient oHttp = new System.Net.Http.HttpClient();

            string sPage = "";

            bool bError = false;

            // oHttp.Timeout = TimeSpan.FromSeconds(8)
            // timeout jest tylko w System.Net.http, ale tam nie działa ("The HTTP redirect request failed")

            try
            {
                sPage = await oHttp.GetStringAsync(new Uri("http://kmkrakow.pl/"));
            }
            catch 
            {
                bError = true;
            }
            oHttp.Dispose();

            if (bError)
            {
                await p.k.DialogBoxRes("resErrorGetHttp");
                return false;
            }

            moLista = new System.Collections.ObjectModel.Collection<JednaInfo>();

            int iInd;
            iInd = sPage.IndexOf("<div class=\"linie");
            while (iInd > 0)
            {
                var oNew = new JednaInfo();

                sPage = sPage.Substring(iInd);
                iInd = sPage.IndexOf("</");
                oNew.sLinie = p.k.RemoveHtmlTags(sPage.Substring(0, iInd));

                iInd = sPage.IndexOf("<div class=\"przedz");
                sPage = sPage.Substring(iInd);
                iInd = sPage.IndexOf("</");
                oNew.sCzas = p.k.RemoveHtmlTags(sPage.Substring(0, iInd));

                iInd = sPage.IndexOf("<h2 class=\"tyt");
                sPage = sPage.Substring(iInd);
                iInd = sPage.IndexOf("</");
                oNew.sTytul = p.k.RemoveHtmlTags(sPage.Substring(0, iInd));

                iInd = sPage.IndexOf("<div class=\"hide");
                sPage = sPage.Substring(iInd);
                iInd = sPage.IndexOf("</div");
                oNew.sInfo = sPage.Substring(0, iInd) + "</div>";

                moLista.Add(oNew);

                iInd = sPage.IndexOf("<div class=\"linie");
            }

            uiFileDate.Text = "";
            uiReload.IsEnabled = false;
            await Save();
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
            JednaInfo oItem = (sender as Windows.UI.Xaml.Controls.Grid).DataContext as JednaInfo;

            string sHtml = oItem.sInfo;
            PokazObjazd(sHtml);
        }

    }
}
