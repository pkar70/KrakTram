using System;
using System.Linq;


namespace KrakTram
{

    public class BliskiStop
    {
        public string sNazwa { get; set; }
        public string sDane { get; set; }
        public int iOdl { get; set; }
    }

    public sealed partial class Setup : Windows.UI.Xaml.Controls.Page
    {
        public Setup()
        {
            this.InitializeComponent();
        }

        //private static Windows.Data.Xml.Dom.XmlDocument oXmlPlaces = new Windows.Data.Xml.Dom.XmlDocument();
        //private System.Threading.Tasks.Task<Windows.Foundation.Point> moTask = null;
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
            p.k.SetSettingsInt("maxOdl", uiMaxOdlSld.Value);
            p.k.SetSettingsInt("walkSpeed", uiWalkSpeedSld.Value);
            p.k.SetSettingsInt("alsoNext", uiAlsoNextSld.Value);
            p.k.SetSettingsInt("gpsPrec", uiGPSPrecSld.Value);
            //p.k.SetSettingsString("favPlaces", oXmlPlaces.GetXml());

            //if (uiAlsoBus.IsOn && !p.k.GetSettingsBool("settingsAlsoBus"))
            //{ // było do doczytania przystanków autobusowych
            //}
            p.k.SetSettingsBool("settingsAlsoBus", uiAlsoBus.IsOn);
            p.k.SetSettingsBool("androAutoTram", uiAndroAutoTram.IsOn);
            this.Frame.GoBack();
        }

        private async void bLoadStops_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // na reload stops? bo reszta funkcjonalnosci jest w mainpage
            // Me.Frame.Navigate(GetType(ListaPrzystankow))
            uiReloadStop.IsEnabled = false;
            await App.CheckLoadStopList(true);
            uiReloadStop.IsEnabled = true;
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

            // poniewaz dla Uno i tak nie ma precyzji GPS, mozna to usunac z ekranu robiąc miejsce
            //if (!p.k.GetPlatform("uwp"))
            //{
            //    uiGPSPrecSld.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //    uiGPSPrecTxt.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //}

            uiMaxOdlSld.Value = p.k.GetSettingsInt("maxOdl", 1000);
            uiWalkSpeedSld.Value = p.k.GetSettingsInt("walkSpeed", 4);
            uiAlsoNextSld.Value = p.k.GetSettingsInt("alsoNext", 5);
            // Android: 100 m, bo <100 jest Accuracy.High, a ≥ 100 to juz bedzie .Medium
            uiGPSPrecSld.Value = p.k.GetSettingsInt("gpsPrec", p.k.GetPlatform(75,100,75,75,75));

            uiPositionLat.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiPositionLong.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiPositionName.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiPositionButt.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            uiPositionLat.Text = App.mdLat.ToString();
            uiPositionLong.Text = App.mdLong.ToString();
            uiAlsoBus.IsOn = p.k.GetSettingsBool("settingsAlsoBus");
            uiAndroAutoTram.IsOn = p.k.GetSettingsBool("androAutoTram");
            if (!p.k.GetPlatform("uwp")) uiAndroAutoTram.Visibility = Windows.UI.Xaml.Visibility.Visible;

            if (msRunType != "MAIN") uiOpenPosPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

#if false
        private string ListaBliskichPrzystankowHTMLxslt(Windows.Foundation.Point oPoint)
        {
            string sXml = "<root>";
            int iOdl;
            int iCnt = 0;
            int iMinOdl = 100000;
            string sTmp;

            foreach (Przystanek oNode in App.oStops.GetList("all"))
            {
                iOdl = App.GPSdistanceDwa(oPoint.X, oPoint.Y, oNode.Lat, oNode.Lon);
                if (iOdl < uiMaxOdlSld.Value)
                {
                    sXml = sXml + "<item name='" + oNode.Name;
                    if (p.k.GetSettingsBool("settingsAlsoBus"))
                    {
                        if (oNode.Cat == "bus")
                            sXml = sXml + " (A)";
                        else
                            sXml = sXml + " (T)";
                    }

                    sXml = sXml + "' odl='" + iOdl + "' ";
                    iOdl = (int)(60 * iOdl / (uiWalkSpeedSld.Value * 1000));
                    sXml = sXml + "odlMinut='" + iOdl + "' />";
                    iCnt = iCnt + 1;
                }
                iMinOdl = Math.Min(iMinOdl, iOdl);
            }

            sXml = sXml + "</root>";

            if (iCnt < 1) return "*" + iMinOdl;

                // tester: http://xslttest.appspot.com/
                string sXslt = @"
<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0'>
    <xsl:template match='/'>
        <html>
            <body>
                <ul>
                    <xsl:apply-templates select='root/item'>
                        <xsl:sort data-type='number' select='@odl' />
                    </xsl:apply-templates>
                </ul>
            </body>
        </html>
    </xsl:template>

  <xsl:template match='root/item'>
    <li><b><xsl:value-of select='@name'/></b>, <xsl:value-of select='@odl'/> m (<xsl:value-of select='@odlMinut' /> min)</li>
  </xsl:template>
</xsl:stylesheet>
";

                var oXsltDoc = new Windows.Data.Xml.Dom.XmlDocument();
                oXsltDoc.LoadXml(sXslt);
                var oXP = new Windows.Data.Xml.Xsl.XsltProcessor(oXsltDoc);
                var oXmlDoc = new Windows.Data.Xml.Dom.XmlDocument();
                oXmlDoc.LoadXml(sXml);
                sTmp = oXP.TransformToString(oXmlDoc.DocumentElement);

            return sTmp;
        }
        private string ListaBliskichPrzystankowHTMLlinq(Windows.Foundation.Point oPoint)
        {
            int iOdl;
            //int iCnt = 0;
            int iMinOdl = 100000;

            System.Collections.ObjectModel.Collection<Przystanek> oItemy = new System.Collections.ObjectModel.Collection<Przystanek>();

            foreach (Przystanek oNode in App.oStops.GetList("all"))
            {
                iOdl = App.GPSdistanceDwa(oPoint.X, oPoint.Y, oNode.Lat, oNode.Lon);
                if (iOdl < uiMaxOdlSld.Value)
                {
                    Przystanek oNew = new Przystanek();
                    oNew.Name = oNode.Name;

                    oNew.iSumDelay = iOdl;
                    if (p.k.GetSettingsBool("settingsAlsoBus"))
                    {
                        if (oNode.Cat == "bus")
                            oNew.Name = oNew.Name + " (A)";
                        else
                            oNew.Name += " (T)";
                    }
                    oNew.iMaxDelay = (int)(60 * iOdl / (uiWalkSpeedSld.Value * 1000));
                    oItemy.Add(oNew);
                }
                iMinOdl = Math.Min(iMinOdl, iOdl);
            }

            if (oItemy.Count < 1) return "*" + iMinOdl;

            string sTmpLBPHL = "<html><body><ul>";
            foreach (Przystanek oNode in from c in oItemy orderby c.iSumDelay select c)
            {
                sTmpLBPHL = sTmpLBPHL + "<li><b>" + oNode.Name + "</b>, " + oNode.iSumDelay + " m (" + oNode.iMaxDelay + " min)</li>\n";
            }
            sTmpLBPHL += "</ul></body></html>";

            return sTmpLBPHL;

        }
#endif

        private void ListaBliskichPrzystankowListView(Windows.Foundation.Point oPoint)
        {
            int iOdl;
            //int iCnt = 0;
            int iMinOdl = 100000;

            System.Collections.ObjectModel.Collection<BliskiStop > oItemy = new System.Collections.ObjectModel.Collection<BliskiStop>();

            foreach (Przystanek oNode in App.oStops.GetList("all"))
            {
                iOdl = App.GPSdistanceDwa(oPoint.X, oPoint.Y, oNode.Lat, oNode.Lon);
                if (iOdl < uiMaxOdlSld.Value)
                {
                    BliskiStop oNew = new BliskiStop();
                    oNew.sNazwa = oNode.Name;
                    oNew.iOdl = iOdl;

                    if (p.k.GetSettingsBool("settingsAlsoBus"))
                    {
                        if (oNode.Cat == "bus")
                            oNew.sNazwa += " (A)";
                        else
                            oNew.sNazwa += " (T)";
                    }
                    else
                        if (oNode.Cat == "bus") continue;   // dla tramwajów - tylko tramwajowe ma pokazywać

                    oNew.sDane = iOdl + " m (" + (int)(60 * iOdl / (uiWalkSpeedSld.Value * 1000)) + " min)";
                    oItemy.Add(oNew);
                }
                iMinOdl = Math.Min(iMinOdl, iOdl);
            }

            if (oItemy.Count < 1)
            {
                BliskiStop oNew = new BliskiStop();
                string sMsg = p.k.GetLangString("resNearestStop");
                if (iMinOdl < 10000)
                    sMsg = sMsg.Replace("###", iMinOdl.ToString() + " m");
                else
                    sMsg = sMsg.Replace("###", (int)(iMinOdl / 1000) + " km");
                oNew.sNazwa = sMsg;
                oItemy.Add(oNew);
            }

            uiListItems.ItemsSource = from c in oItemy orderby c.iOdl select c;
        }

    private async void eMaxOdl_Changed(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (uiMaxOdlTxt == null)
                return;
            uiMaxOdlTxt.Text = uiMaxOdlSld.Value + " m";

            // oPoint - albo narzucony, albo z GPS
            Windows.Foundation.Point oPoint = new Windows.Foundation.Point();
            if (App.mdLat == 100)
                oPoint = await App.GetCurrentPoint();
            else
            {
                oPoint.X = App.mdLat;
                oPoint.Y = App.mdLong;
            }

            // Dim sHtml As string = "<html><body>"
            // Dim iOdl As Integer
            // For Each oNode In App.oStops.SelectNodes("//stop")
            // iOdl = App.GPSdistanceDwa(oPoint.X, oPoint.Y, oNode.SelectSingleNode("@lat").NodeValue, oNode.SelectSingleNode("@lon").NodeValue)
            // If iOdl < uiMaxOdlSld.Value Then
            // sHtml = sHtml & "<li><b>" & oNode.SelectSingleNode("@name").NodeValue & "</b>, " & iOdl & " metrów"
            // End If
            // Next
            // sHtml = sHtml & "</body></html>"


            ListaBliskichPrzystankowListView(oPoint);
            //string sTmp = ListaBliskichPrzystankowHTMLlinq(oPoint);
            //if(sTmp.Length<1 | sTmp.Substring(0,1) == "*")
            //{
            //    string sMsg = p.k.GetLangString("resNearestStop");
            //    int iMinOdl = 10000;
            //    int.TryParse(sTmp.Substring(1), out iMinOdl);
            //    if (iMinOdl < 10000)
            //        sMsg = sMsg.Replace("###", iMinOdl.ToString() + " m");
            //    else
            //        sMsg = sMsg.Replace("###", (int)(iMinOdl / 1000) + " km");
            //    sTmp = "<html><body><b>" + sMsg + "</b></body></html>";
            //}

            //uiSetupWebView.NavigateToString(sTmp);
        }

        private void uiWalk_Changed(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (uiWalkSpeedTxt == null)
                return;
            uiWalkSpeedTxt.Text = uiWalkSpeedSld.Value + " km/h";
        }

        private void uiNext_Changed(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (uiAlsoNextTxt == null)
                return;
            uiAlsoNextTxt.Text = uiAlsoNextSld.Value + " min";
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

        private async void uiPositCancel_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ShowPositionPanel(false);
        }


        private async void uiPositOk_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // dodawanie nowego entry

            string sTxt = uiPositionName.Text;
            // sTxt = sTxt.Replace("*", "")
            // sTxt = sTxt.Replace("[", "")
            // sTxt = sTxt.Replace("]", "")

            if (sTxt.Length < 4)
            {
                await p.k.DialogBoxRes("resErrorNazwaZaKrotka");
                return;
            }

            double dLat, dLon;
            if (!double.TryParse(uiPositionLong.Text, out dLon) || !double.TryParse(uiPositionLat.Text, out dLat))
            {
                await p.k.DialogBoxRes("resBadFloat");
                return;
            }

            if ((sTxt ?? "") == "pkarinit")
                App.oFavour.InitPkar();
            else
            {
                if (dLon < 19 | dLon > 21 | dLat < 49 | dLat > 51)
                {
                    await p.k.DialogBoxRes("resErrorPozaKrakowem");
                    return;
                }

                App.oFavour.Add(sTxt, dLat, dLon, (int)uiMaxOdlSld.Value);
                App.mdLat = dLat;    // i ustalamy to jako biezace wspolrzedne
                App.mdLong = dLon;
            }

            ShowPositionPanel(false);
        }
#if false
        // Private Async Sub uiPosition_Changed(sender As Object, e As SelectionChangedEventArgs) Handles uiPosition.SelectionChanged

        // If uiPositionFavButt Is Nothing Then Exit Sub
        // uiPositionLat.Visibility = Visibility.Collapsed
        // uiPositionLong.Visibility = Visibility.Collapsed
        // uiPositionButt.Visibility = Visibility.Collapsed
        // uiPositionFavButt.Visibility = Visibility.Collapsed
        // uiPositionName.Visibility = Visibility.Collapsed
        // Setup_SizeChanged(sender, Nothing) ' wielkosc WebView

        // Dim iTmp As Integer = uiPosition.SelectedIndex
        // If iTmp < 0 Then Exit Sub

        // Dim sTxt As String = uiPosition.Items.ElementAt(iTmp)

        // If sTxt = ResourceLoader.GetForCurrentView().GetString("resSettPosItemGPS") Then
        // ' uzywaj GPS
        // App.mdLat = 100
        // uiGPSPrecSld.Visibility = Visibility.Visible
        // uiGPSPrecTxt.Visibility = Visibility.Visible
        // eMaxOdl_Changed(Nothing, Nothing)
        // Exit Sub
        // End If

        // If sTxt = ResourceLoader.GetForCurrentView().GetString("resSettPosItemEnter") Then

        // uiPositionLat.Visibility = Visibility.Visible
        // uiPositionLong.Visibility = Visibility.Visible
        // uiPositionButt.Visibility = Visibility.Visible
        // uiPositionName.Visibility = Visibility.Visible
        // Setup_SizeChanged(sender, Nothing) ' wielkosc WebView

        // ' wstaw aktualne wspolrzedne
        // Dim oPoint As Point = Await App.GetCurrentPoint
        // uiPositionLat.Text = oPoint.X
        // uiPositionLong.Text = oPoint.Y
        // App.mdLat = oPoint.X
        // App.mdLong = oPoint.Y


        // Exit Sub
        // End If

        // If uiPosition.SelectedValue.Substring(0, 1) = "*" Then
        // ' nowiutki swiezutki
        // App.mdLat = uiPositionLat.Text
        // App.mdLong = uiPositionLong.Text
        // Exit Sub
        // End If

        // to bedzie jakis Favourite
        // Try
        // Dim oItem As IXmlNode = oXmlPlaces.DocumentElement.SelectSingleNode("//place[@name='" & sTxt & "']")
        // App.mdLat = oItem.SelectSingleNode("@lat").NodeValue
        // App.mdLong = oItem.SelectSingleNode("@long").NodeValue
        // uiMaxOdlSld.Value = oItem.SelectSingleNode("@maxOdl").NodeValue

        // uiPositionFavButt.Visibility = Visibility.Visible
        // uiGPSPrecSld.Visibility = Visibility.Collapsed
        // uiGPSPrecTxt.Visibility = Visibility.Collapsed

        // eMaxOdl_Changed(Nothing, Nothing)
        // Catch ex As Exception

        // End Try

        // End Sub

        // <CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId:="e")>
        // Private Sub uiAddRemove_Click(sender As Object, e As RoutedEventArgs)
        // ' w nowej wersji - to tylko kasowanie
        // End Sub

        // Private Sub uiLat_Changed(sender As Object, e As TextChangedEventArgs) Handles uiPositionLat.TextChanged
        // If Not Double.TryParse(uiPositionLat.Text, App.mdLat) Then
        // App.mdLat = 0
        // End If
        // End Sub

        // Private Sub uiLong_Changed(sender As Object, e As TextChangedEventArgs) Handles uiPositionLong.TextChanged
        // If Not Double.TryParse(uiPositionLong.Text, App.mdLong) Then
        // App.mdLong = 0
        // End If
        // End Sub

        // Private Sub bFavButton_Click(sender As Object, e As RoutedEventArgs) Handles uiPositionFavButt.Click
        // Dim iTmp As Integer = uiPosition.SelectedIndex
        // Dim sTxt As String = uiPosition.Items.ElementAt(iTmp)

        // oXmlPlaces.DocumentElement.RemoveChild(
        // oXmlPlaces.DocumentElement.SelectSingleNode("//place[@name='" & sTxt & "']"))
        // App.SetSettingsString("favPlaces", oXmlPlaces.GetXml, True)
        // ' select [locator] - ale moze niech sie samo zmieni...

        // uiPositionFavButt.Visibility = Visibility.Collapsed

        // ReloadFavPlaces()
        // uiPosition.SelectedIndex = 0


        // End Sub

        // Private Sub ReloadFavPlaces()
        // Dim sTxt As String
        // sTxt = App.GetSettingsString("favPlaces", "<places></places>")
        // 'sTxt = "<places></places>"
        // Dim bError As Boolean = False
        // Try
        // oXmlPlaces.LoadXml(sTxt)
        // Catch ex As Exception
        // bError = True
        // End Try
        // If bError Then App.DialogBox("ERROR loading favourites list")

        // ' dwa podstawowe
        // uiPosition.Items.Clear
        // uiPosition.Items.Add(ResourceLoader.GetForCurrentView().GetString("resSettPosItemGPS"))
        // uiPosition.Items.Add(ResourceLoader.GetForCurrentView().GetString("resSettPosItemEnter"))

        // For Each oPlace As IXmlNode In oXmlPlaces.DocumentElement.SelectNodes("//place")
        // uiPosition.Items.Add(oPlace.SelectSingleNode("@name").NodeValue)
        // Next

        // End Sub

        // Private Sub Setup_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        // ' poprzednio ręcznie, teraz samo niby (via Stretch)
        // 'If uiSetupWebView Is Nothing Then Exit Sub
        // 'uiSetupWebView.Height = uiPage.ActualHeight - uiSetup_Grid.ActualHeight - 20    ' zgaduje, tu sie powinno odjac poczatek rysowania (Y.top)
        // End Sub
#endif 
        private void uiGpsPrec_Changed(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (uiGPSPrecTxt == null)
                return;
            uiGPSPrecTxt.Text = uiGPSPrecSld.Value + " m";

            // musi od razu, żeby zaraz zaczęło działać (np. przy przestawianiu odleglosci od przystanku)
            p.k.SetSettingsInt("gpsPrec", uiGPSPrecSld.Value);
        }

        private void uiOpenPosPanel_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ShowPositionPanel(true);
        }

        private void uiAlsoBus_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            p.k.SetSettingsBool("settingsAlsoBus", uiAlsoBus.IsOn);
            eMaxOdl_Changed(null, null);
        }
    }

}
