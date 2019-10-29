using System;
//using System.Collections.Generic;
using System.IO; // bez tego nie widzi oFile.OpenStreamForWriteAsync()
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KrakTram
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Trasa : Windows.UI.Xaml.Controls.Page
    {
        public Trasa()
        {
            this.InitializeComponent();
        }

        private string msLinia = "";
        private string msKier = "";
        private string msStop = "";

        public partial class JedenStop
        {
            public string Linia { get; set; }
            public string Przyst { get; set; }
            public int iMin { get; set; }
            public string sMin { get; set; }
            public int Num { get; set; }
        }

        private System.Collections.ObjectModel.Collection<JedenStop> moStopsy;

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            string[] aParams = e.Parameter.ToString().Split('|');
            if (aParams.GetUpperBound(0) > -1)
                msLinia = aParams[0];
            if (aParams.GetUpperBound(0) > 0)
                msKier = aParams[1];
            if (aParams.GetUpperBound(0) > 1)
                msStop = aParams[2];
        }

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiTitle.Text = p.k.GetLangString("resTrasa") + " " + msLinia;
            if (!await TryLoadTrasaCache(msLinia))
            {
                double dVal;
                dVal = Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth) / 2;
                uiProcesuje.Width = dVal;
                uiProcesuje.Height = dVal;
                uiProcesuje.Visibility = Windows.UI.Xaml.Visibility.Visible;
                uiProcesuje.IsActive = true;

                await WczytajTrase(msLinia);

                uiProcesuje.IsActive = false;
                uiProcesuje.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            if (moStopsy is null || moStopsy.Count < 1)
                return;

            // policz numer przystanku
            int iStopNo = 0;
            if (!string.IsNullOrEmpty(msStop))
            {
                foreach (JedenStop oStop in moStopsy)
                {
                    if ((oStop.Przyst.ToLower() ?? "") == (msStop.ToLower() ?? ""))
                        break;
                    iStopNo = iStopNo + 1;
                }
            }

            // dopisanie numeru przystanku
            foreach (JedenStop oStop in moStopsy)
            {
                if (moStopsy.ElementAt(0).Przyst == msKier)
                    // iNum od 0 .. iStopNo .. max -> iMin= -iCount+iStopNo .. 0 .. iCount-iStopNo
                    oStop.iMin = moStopsy.Count - oStop.Num - (moStopsy.Count - iStopNo);
                else
                    // iNum od 0 .. iStopNo .. max -> iMin= -iStopNo .. 0 .. iCount-iStopNo
                    oStop.iMin = oStop.Num - iStopNo;

                oStop.sMin = oStop.iMin.ToString();
            }

            uiListStops.ItemsSource = from c in moStopsy
                                      orderby c.iMin
                                      select c;
        }

        private async System.Threading.Tasks.Task<bool> TryLoadTrasaCache(string sLinia)
        {
            Windows.Storage.StorageFile oObj;
            oObj = (await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("line" + sLinia + ".xml")) as Windows.Storage.StorageFile;
            if (oObj == null)
                return false;

            if (oObj.DateCreated.AddDays(30) < DateTime.Now)
                return false;    // za stare
            uiFileDate.Text = oObj.DateCreated.ToString("dd/MM/yyyy");

            System.Xml.Serialization.XmlSerializer oSer;
            oSer = new System.Xml.Serialization.XmlSerializer(typeof(System.Collections.ObjectModel.Collection<JedenStop>));
            Stream oStream = await oObj.OpenStreamForReadAsync();
            try
            {
                moStopsy = oSer.Deserialize(oStream) as System.Collections.ObjectModel.Collection<JedenStop>;
            }
            catch 
            {
                return false;
            }

            uiReload.IsEnabled = true;

            return true;
        }

        private async System.Threading.Tasks.Task Save(string sLinia)
        {
            Windows.Storage.StorageFile oFile;
            oFile = (await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("line" + sLinia + ".xml")) as Windows.Storage.StorageFile;
            if (oFile != null)
                await oFile.DeleteAsync();
            // bez tego kasowania create timestamp jest stary!

            oFile = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync("line" + sLinia + ".xml", Windows.Storage.CreationCollisionOption.ReplaceExisting);

            if (oFile == null)
                return;

            System.Xml.Serialization.XmlSerializer oSer;
            oSer = new System.Xml.Serialization.XmlSerializer(typeof(System.Collections.ObjectModel.Collection<JedenStop>));
            Stream oStream = await oFile.OpenStreamForWriteAsync();
            oSer.Serialize(oStream, moStopsy);
            oStream.Dispose();   // == fclose
        }

        private async System.Threading.Tasks.Task<bool> WczytajTrase(string sLinia)
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
                sPage = await oHttp.GetStringAsync(new Uri("http://rozklady.mpk.krakow.pl/?lang=PL&linia=" + sLinia));
            }
            catch 
            {
                bError = true;
            }
            if (bError)
            {
                await p.k.DialogBoxRes("resErrorGetHttp");
                return false;
            }

            int iInd;
            iInd = sPage.IndexOf("Trasa:");
            if (iInd < 10)
                return false;
            iInd = sPage.IndexOf("Przystanki", iInd);
            if (iInd < 10)
                return false;
            sPage = sPage.Substring(iInd);
            iInd = sPage.IndexOf("<table");
            if (iInd < 10)
                return false;
            if (iInd > 250)
                return false;
            sPage = sPage.Substring(iInd);
            iInd = sPage.IndexOf("</table");
            if (iInd < 10)
                return false;
            sPage = sPage.Substring(0, iInd - 1);

            sPage = p.k.RemoveHtmlTags(sPage);
            string[] aArr = sPage.Split('\n'); // Constants.vbLf);
            int iNum = 0;

            moStopsy = new System.Collections.ObjectModel.Collection<JedenStop>();

            foreach (string sLine1 in aArr)
            {
                string sLine = sLine1.Trim();
                if (sLine.Length > 2)
                {
                    JedenStop oNew = new JedenStop();
                    oNew.Linia = sLinia;
                    oNew.Przyst = sLine;
                    oNew.iMin = 0;   // ewentualnie pozniej liczyc czas przejazdu
                    oNew.sMin = "";
                    oNew.Num = iNum;
                    moStopsy.Add(oNew);
                    iNum += 1;
                }
            }

            uiFileDate.Text = "";
            uiReload.IsEnabled = false;
            await Save(sLinia);
            return true;
        }

        private void ShowTabliczka(string sStop)
        {
            foreach (Przystanek oStop in App.oStops.GetList("all"))
            {
                if (oStop.Name == sStop)
                {
                    App.mbGoGPS = false;
                    App.mMaxOdl = p.k.GetSettingsInt("treatAsSameStop", 150);
                    App.mdLat = oStop.Lat;
                    App.mdLong = oStop.Lon;
                    App.moOdjazdy.Clear();
                    this.Frame.Navigate(typeof(Odjazdy));
                }
            }
        }

        private void uiClose_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private void uiGoPrzystanek_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Windows.UI.Xaml.Controls.MenuFlyoutItem oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            if (oMFI == null)
                return;
            JedenStop oItem = oMFI.DataContext as JedenStop;
            if (oItem == null)
                return;
            ShowTabliczka(oItem.Przyst);
        }

        private async void uiRefresh_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiReload.IsEnabled = false;
            await WczytajTrase(msLinia);
            uiListStops.ItemsSource = from c in moStopsy
                                      orderby c.iMin
                                      select c;
        }

    }
}