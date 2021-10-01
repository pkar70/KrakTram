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


namespace KrakTram
{

    public class JedenStop
    {// musi byc public, bo inaczej serializer nie dziala
        public string Linia { get; set; }
        public string Przyst { get; set; }
        public int iMin { get; set; }
        public string sMin { get; set; }
        public int Num { get; set; }
    }
 
    public sealed partial class Trasa : Windows.UI.Xaml.Controls.Page
    {
        public Trasa()
        {
            this.InitializeComponent();
        }

        private string msLinia = "";
        private string msKier = "";
        private string msStop = "";



        private System.Collections.ObjectModel.Collection<JedenStop> moStopsy;

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
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
        }

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiTitle.Text = p.k.GetLangString("resTrasa") + " " + msLinia;

            p.k.ProgRingInit(true, false);

            if (!await TryLoadTrasaCache(msLinia))
            {
                p.k.ProgRingShow(true);
                //double dVal;
                //dVal = Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth) / 2;
                //uiProcesuje.Width = dVal;
                //uiProcesuje.Height = dVal;
                //uiProcesuje.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //uiProcesuje.IsActive = true;

                await WczytajTrase(msLinia);

                p.k.ProgRingShow(false);
                //uiProcesuje.IsActive = false;
                //uiProcesuje.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
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
                System.Xml.XmlReader oXmlReader = System.Xml.XmlReader.Create(oStream);
                moStopsy = oSer.Deserialize(oXmlReader) as System.Collections.ObjectModel.Collection<JedenStop>;
                oXmlReader.Dispose();
            }
            catch 
            {
                return false;
            }

            if (moStopsy.Count < 1) return false;   // co z tego ze jest plik jak nie ma trasy?

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
            if (!p.k.NetIsIPavailable(false))
            {
                await p.k.DialogBoxResAsync("resErrorNoNetwork");
                return false;
            }

            System.Net.Http.HttpClientHandler oHCH = new System.Net.Http.HttpClientHandler();
            oHCH.AllowAutoRedirect = false;
            oHCH.CookieContainer = new System.Net.CookieContainer();
            oHCH.UseCookies = true;

            System.Net.Http.HttpClient oHttp = new System.Net.Http.HttpClient(oHCH);

            string sPage = "";

            bool bError = false;

            // oHttp.Timeout = TimeSpan.FromSeconds(8)
            // timeout jest tylko w System.Net.http, ale tam nie działa ("The HTTP redirect request failed"), 302
            // a z drugiej strony, dla Uno musi byc System.Net a nie Windows :)
            Uri oUri = new Uri("http://rozklady.mpk.krakow.pl/?lang=PL&linia=" + sLinia); // http://rozklady.mpk.krakow.pl/?lang=PL&linia=50
            // oHttp.DefaultRequestHeaders.Add("Referer", )
            try
            {
                System.Net.Http.HttpResponseMessage oHttResp = await oHttp.GetAsync(oUri);
                if(oHttResp.StatusCode == System.Net.HttpStatusCode.Found )
                {
                    oHttResp = await oHttp.GetAsync(oHttResp.RequestMessage.RequestUri );
                }
                // sPage = await oHttp.GetStringAsync(oUri);

                if (oHttResp.IsSuccessStatusCode)
                    sPage = await oHttResp.Content.ReadAsStringAsync();
            }
            catch 
            {
                bError = true;
            }

            if(string.IsNullOrEmpty(sPage))
            {
                System.Net.Http.HttpResponseMessage oHttResp = await oHttp.GetAsync(oUri);
                if (oHttResp.StatusCode == System.Net.HttpStatusCode.Found)
                {
                    oHttResp = await oHttp.GetAsync(oHttResp.RequestMessage.RequestUri);
                }
                // sPage = await oHttp.GetStringAsync(oUri);

                if (oHttResp.IsSuccessStatusCode)
                    sPage = await oHttResp.Content.ReadAsStringAsync();

            }


            oHttp.Dispose();
            oHCH.Dispose();

            if (bError)
            {
                await p.k.DialogBoxResAsync("resErrorGetHttp");
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


        private void GoPrzystanek(JedenStop oItem)
        {
            if (oItem == null) return;
            ShowTabliczka(oItem.Przyst);
        }


        private void uiGoPrzystanek_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Windows.UI.Xaml.Controls.MenuFlyoutItem oMFI = sender as Windows.UI.Xaml.Controls.MenuFlyoutItem;
            if (oMFI == null) return;
            GoPrzystanek(oMFI.DataContext as JedenStop);
        }

        private async void uiRefresh_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            uiReload.IsEnabled = false;
            if (!(await WczytajTrase(msLinia))) return;

            if (moStopsy is null || moStopsy.Count < 1) // podwojne zabezpieczenie :)
                return;

            uiListStops.ItemsSource = from c in moStopsy
                                      orderby c.iMin
                                      select c;
        }

        //private void uiGridMenu_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        //{ // symulacja dla Android
        //    var oGrid = sender as Windows.UI.Xaml.Controls.Grid;
        //    oGrid.ContextFlyout.ShowAt(oGrid);
        //}
        private void uiGoTabliczka_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        { //shortcut do tabliczki (dla Windows)
            var oGrid = sender as Windows.UI.Xaml.Controls.Grid;
            if (oGrid == null) return;
            GoPrzystanek(oGrid.DataContext as JedenStop);
        }
    }
}