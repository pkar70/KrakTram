
using System;
using System.IO; // bez tego nie widzi oFile.OpenStreamForWriteAsync()
using System.Linq; // bez tego nie ma .Where

using System.Xml.Serialization;



[XmlType("stop")]
public partial class Przystanek
{
    [XmlAttribute()]
    public string Cat { get; set; }
    [XmlAttribute()]
    public double Lat { get; set; }
    [XmlAttribute()]
    public double Lon { get; set; }
    [XmlAttribute()]
    public string Name { get; set; }
    [XmlAttribute()]
    public string id { get; set; }
    [XmlIgnore]
    public int iSumDelay { get; set; }
    [XmlIgnore]
    public int iMaxDelay { get; set; }
    [XmlIgnore]
    public int iEntriesCount { get; set; }
    [XmlIgnore]
    public int iEntriesTotal { get; set; }
}



public partial class Przystanki
{
    private System.Collections.ObjectModel.Collection<Przystanek> moItemy = new System.Collections.ObjectModel.Collection<Przystanek>();

    private DateTime mdOpoznLastDate = DateTime.Now.AddDays(-5);

    // Private msTyp As String

    // Public Sub New(sType As String)
    // ' jesli nie "b....", to tramwaj
    // sType = sType.ToLower
    // If sType = "" Then
    // msTyp = "t"
    // Else
    // If sType.Substring(0, 1) = "b" Then
    // msTyp = "b"
    // Else
    // msTyp = "t"
    // End If
    // End If
    // End Sub

    // Add
    private void Add(string sCat, double dLatTtss, double dLonTtss, string sName, string sId)
    {
        var oNew = new Przystanek();
        oNew.Cat = sCat;
        oNew.id = sId;
        oNew.Name = sName;
        oNew.Lat = dLatTtss / 3600000.0;
        oNew.Lon = dLonTtss / 3600000.0;
        moItemy.Add(oNew); // błąd resource; dodawanie pustego ("" oraz 0) też powoduje error
    }
    // Delete
    // New
    private async System.Threading.Tasks.Task Save()
    {
        Windows.Storage.StorageFile oFile = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync("stops1.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting);

        if (oFile == null)
        {
            await p.k.DialogBoxAsync("ERROR cannot create stops1.xml file?");
            return;
        }

        XmlSerializer oSer = new XmlSerializer(typeof(System.Collections.ObjectModel.Collection<Przystanek>));
        try
        {
            System.IO.Stream oStream = await oFile.OpenStreamForWriteAsync();
            oSer.Serialize(oStream, moItemy);
            oStream.Dispose();   // == fclose
        }
        catch
        {
            await p.k.DialogBoxAsync("ERROR cannot serialize stops?");
        }
    }

    // Load
    private async System.Threading.Tasks.Task<bool> Load()
    {
        // ret=false gdy nie jest wczytane

        Windows.Storage.StorageFile oObj = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("stops1.xml") as Windows.Storage.StorageFile;
        if (oObj == null)
            return false;
        Windows.Storage.StorageFile oFile = oObj as Windows.Storage.StorageFile;

        try
        {
            XmlSerializer oSer = new XmlSerializer(typeof(System.Collections.ObjectModel.Collection<Przystanek>));
            Stream oStream = await oFile.OpenStreamForReadAsync();
            System.Xml.XmlReader oXmlReader = System.Xml.XmlReader.Create(oStream);
            moItemy = oSer.Deserialize(oXmlReader) as System.Collections.ObjectModel.Collection<Przystanek>;
            oXmlReader.Dispose();

            return true;
        }
        catch
        {
            await p.k.DialogBoxAsync("ERROR reading stops file?");
            return false;
        }
    }


#if false
    private string ImportWindowsJSON(string sPage)
    {
        Windows.Data.Json.JsonObject oJson = null;
        try
        {
            oJson = Windows.Data.Json.JsonObject.Parse(sPage);
        }
        catch
        {
            return "ERROR: JSON parsing error";
        }

        var oJsonStops = new Windows.Data.Json.JsonArray();
        try
        {
            oJsonStops = oJson.GetNamedArray("stops");
        }
        catch
        {
            return "ERROR: JSON \"stops\" array missing";
        }

        if (oJsonStops.Count == 0)
            return "ERROR: JSON 0 obiektów";

        try
        {
            foreach (Windows.Data.Json.IJsonValue oVal in oJsonStops)
            {
                string sName;
                string sCat;
                string sShortName;
                sName = oVal.GetObject().GetNamedString("name");
                sCat = oVal.GetObject().GetNamedString("category");
                sShortName = oVal.GetObject().GetNamedString("shortName");
                double dLat;
                double dLon;
                dLat = oVal.GetObject().GetNamedNumber("latitude");
                dLon = oVal.GetObject().GetNamedNumber("longitude");
                if (sName.Length > 2)
                    Add(sCat, dLat, dLon, sName, sShortName);
            }
        }
        catch
        {
            return "ERROR: at JSON converting";
        }

        return "";
    }
#endif
    private string ImportNewtonsoftJSON(string sPage)
    {
        Newtonsoft.Json.Linq.JObject oJson = null;
        try
        {
            oJson = Newtonsoft.Json.Linq.JObject.Parse(sPage);
        }
        catch
        {
            return "ERROR: JSON parsing error";
        }

        Newtonsoft.Json.Linq.JArray oJsonStops = new Newtonsoft.Json.Linq.JArray();
        try
        {
            oJsonStops = (Newtonsoft.Json.Linq.JArray)oJson["stops"];
        }
        catch
        {
            return "ERROR: JSON \"stops\" array missing";
        }

        if (oJsonStops.Count == 0)
            return "ERROR: JSON 0 obiektów";

        try
        {
            foreach (Newtonsoft.Json.Linq.JObject oVal in oJsonStops)
            {
                string sName;
                string sCat;
                string sShortName;
                sName = (string)oVal["name"];
                sCat = (string)oVal["category"];
                sShortName = (string)oVal["shortName"];
                double dLat;
                double dLon;
                dLat = (int)oVal["latitude"];
                dLon = (int)oVal["longitude"];
                if (sName.Length > 2)
                    Add(sCat, dLat, dLon, sName, sShortName);
            }
        }
        catch
        {
            return "ERROR: at JSON converting";
        }

        return "";

    }

    private async System.Threading.Tasks.Task<string> ImportMain(string sUrl)
    {
        System.Net.Http.HttpClient oHttp = new System.Net.Http.HttpClient();
        string sTmp = "";
        oHttp.Timeout = TimeSpan.FromSeconds(10);

        try
        {
            sTmp = await oHttp.GetStringAsync(sUrl);
        }
        catch 
        {
            oHttp.Dispose();
            return "resErrorGetHttp";
        }

        oHttp.Dispose();
        // {"stops": [
        // {
        // "category": "tram",
        // "id": "6350927454370005230",
        // "latitude": 180367133,
        // "longitude": 72043450,
        // "name": "Os.Piastów",
        // "shortName": "378"
        // },

        if (sTmp.IndexOf("\"stops\"") < 0)
            return "resErrorBadTTSSstops";

        return ImportNewtonsoftJSON(sTmp);
    }

    private async System.Threading.Tasks.Task<bool> Import()
    {
        // ret=false gdy nieudane wczytanie z sieci

        if (!p.k.NetIsIPavailable(false)) //  System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            await p.k.DialogBoxResAsync("resErrorNoNetwork");
            return false;
        }

        // kiedys to bylo po testach, teraz - przed wczytaniem
        System.Collections.ObjectModel.Collection<Przystanek> oItemyOld = moItemy;  // czy to skopiuje zawartosc?

        moItemy.Clear();

        string sRetMsg = await ImportMain("http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000");

        if (!string.IsNullOrEmpty(sRetMsg))
        {
            if ((sRetMsg.Substring(0, 3) ?? "") == "res")
                await p.k.DialogBoxResAsync(sRetMsg);
            else
                await p.k.DialogBoxAsync(sRetMsg);
        }

        if (p.k.GetSettingsBool("settingsAlsoBus") | true)  // wczytujemy zawsze - bo mozna potem włączyć...
        {
            sRetMsg = await ImportMain("http://91.223.13.70/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000");

            if (!string.IsNullOrEmpty(sRetMsg))
            {
                if ((sRetMsg.Substring(0, 3) ?? "") == "res")
                    await p.k.DialogBoxResAsync(sRetMsg);
                else
                    await p.k.DialogBoxAsync(sRetMsg);
            }
        }

        await Save();    // teoretycznie mogloby byc bez Await, zeby sobie w tle robil Save
        int iLastLoad = 0;
        if (!int.TryParse(DateTime.Now.ToString("yyMMdd"), out iLastLoad)) iLastLoad = 0;
        p.k.SetSettingsInt("LastLoadStops", iLastLoad);

        if (p.k.GetSettingsBool("pkarmode"))
            await Compare(oItemyOld, moItemy);

        return true;
    }

    public async System.Threading.Tasks.Task LoadOrImport(bool bForceLoad)
    {
        int iHowOld;
        try // 20171108: czasem przy starcie wylatuje, może tu?
        {
            int iCurrDate;
            if (!int.TryParse(DateTime.Now.ToString("yyMMdd"), out iCurrDate)) iCurrDate = 0;
            iHowOld = iCurrDate - p.k.GetSettingsInt("LastLoadStops");
        }
        catch 
        {
            iHowOld = 99;
        }

        bool bReaded = false;
        if (!bForceLoad)
            bReaded = await Load();  // True gdy udane wczytanie; nie ma sensu czytac gdy wymuszamy import

        //int iCos;
        //iCos = moItemy.Count;
        //iCos = GetList().Count;
        //iCos = GetList("bus").Count;
        //iCos = GetList("all").Count;


        // 2019.10.26: gdy lista pusta, to jednak wczytaj...
        if (bReaded && moItemy.Count < 1) bReaded = false;      // jak jest puste w ogole
        if (bReaded && GetList().Count < 1) bReaded = false;    // jak są puste tramwaje (a autobusy są)

        if (!bForceLoad)
        {
            if (bReaded && iHowOld < 30)
                return;
        }

        await Import();
    }

    public Przystanek GetItem(string sName, string sCat = "tram")
    {
        foreach (Przystanek oItem in moItemy)
        {
            if ((oItem.Name ?? "") == (sName ?? "") && (oItem.Cat ?? "") == (sCat ?? ""))
                return oItem;
        }
        return null;
    }

    public System.Collections.Generic.List<Przystanek> GetList(string sCat = "tram")
    {
        switch (sCat)
        {
            case "all":
                return moItemy.ToList<Przystanek>();
            case "bus":
                // Return From c In moItemy Where sCat = "bus"
                return moItemy.Where(s => (s.Cat == "bus")).ToList<Przystanek>(); //<Przystanek>;
            default:
                return moItemy.Where(s => (s.Cat == "tram")).ToList<Przystanek>();
        }
    }

    private static async System.Threading.Tasks.Task Compare(System.Collections.ObjectModel.Collection<Przystanek> oOld, System.Collections.ObjectModel.Collection<Przystanek> oNew)
    {
        string sDiffsDel = "";

        foreach (Przystanek oItemOld in oOld)
        {
            bool bDalej = false;
            foreach (Przystanek oItemNew in oNew)
            {
                if ((oItemNew.Name ?? "") == (oItemOld.Name ?? ""))
                {
                    bDalej = true;
                    break;
                }
            }

            if (!bDalej)
                sDiffsDel = sDiffsDel + oItemOld.Name + "\n";
        }

        string sDiffsNew = "";

        foreach (Przystanek oItemNew in oNew)
        {
            bool bNowe = true;
            foreach (Przystanek oItemOld in oOld)
            {
                if ((oItemNew.Name ?? "") == (oItemOld.Name ?? ""))
                {
                    bNowe = false;
                    break;
                }
            }

            if (bNowe)
                sDiffsNew = sDiffsNew + oItemNew.Name + "\n";
        }

        if (!string.IsNullOrEmpty(sDiffsNew))
            sDiffsNew = "Nowe:" + "\n" + sDiffsNew;
        if (!string.IsNullOrEmpty(sDiffsDel))
            sDiffsDel = "Usunięte:" + "\n" + sDiffsDel;

        if (!string.IsNullOrEmpty(sDiffsNew) || !string.IsNullOrEmpty(sDiffsDel))
            await p.k.DialogBoxAsync(p.k.GetLangString("resChangesInStopList") + "\n" + sDiffsDel + "\n" + sDiffsNew);
    }


    public async System.Threading.Tasks.Task<bool> OpoznieniaFromHttpAsync(int iType)
    {
        // policzenie opóźnień, b0 = bus, b1 = tram
        if (mdOpoznLastDate.AddMinutes(5) > DateTime.Now)
            if (!await p.k.DialogBoxYNAsync("Niedawno było, na pewno?"))
                return false;

        string sCat;
        switch (iType)
        {
            case 1:
                    sCat = "tram";
                    break;
            case 2:
                    sCat = "bus";
                    break;
            default:
                    return false;
        }

        // policz
        string sTxt = "";

        foreach (Przystanek oItem in moItemy)
        {
            // wczytaj dane przystanku
            if ((oItem.Cat ?? "") != (sCat ?? ""))
                continue;

            sTxt = sTxt + oItem.id + "\t" + "Przystanek: " + oItem.Name + "\n";

            int iId = 0;
            if (!int.TryParse(oItem.id, out iId)) iId = 0;
            Newtonsoft.Json.Linq.JObject oJson = await KrakTram.App.WczytajTabliczke(oItem.Cat, oItem.Name, iId);
            bool bError = false;


            Newtonsoft.Json.Linq.JArray oJsonStops = new Newtonsoft.Json.Linq.JArray();

            try
            {
                oJsonStops = (Newtonsoft.Json.Linq.JArray)oJson["actual"];
            }
            catch 
            {
                bError = true;
            }
            if (bError)
            {
                // DialogBox("ERROR: JSON ""actual"" array missing")
                continue;
                // return false;
            }

            oItem.iEntriesCount = 0;
            oItem.iEntriesTotal = oJsonStops.Count;
            oItem.iSumDelay = 0;
            oItem.iMaxDelay = 0;

            if (oJsonStops.Count == 0)
                continue;

            // policz...
            foreach (Newtonsoft.Json.Linq.JObject oVal in oJsonStops)
            {
                oItem.iEntriesTotal += 1;
                // jesli PREDICTED (a nie np. PLANNED), to znaczy że liczymy
                string sPlanTime = "!ERR!";
                string sActTime = "!ERR!";
                string sTypCzasu = "!ERR!";
                string sPattTxt = "!ERR!";
                string sDirect = "!error!";

                try { sPlanTime = (string)oVal["plannedTime"]; } catch { }
                if (string.IsNullOrEmpty(sPlanTime)) sPlanTime = "!ERR!";
                // ewentualnie:
                // sPlanTime &&= "!ERR!"
                // ale to bedzie tylko dla null, a  nie dla empty

                try { sActTime = (string)oVal["actualTime"]; } catch { }
                if (string.IsNullOrEmpty(sActTime)) sActTime = "!ERR!";

                try { sTypCzasu = (string)oVal["status"]; } catch { }
                if (string.IsNullOrEmpty(sTypCzasu)) sTypCzasu = "!ERR!";

                try { sPattTxt = (string)oVal["patternText"]; } catch { }
                if (string.IsNullOrEmpty(sPattTxt)) sPattTxt = "!ERR!";

                try { sDirect = (string)oVal["direction"]; } catch { }
                if (string.IsNullOrEmpty(sDirect)) sDirect = "!error!";

                sTxt = sTxt + oItem.id + "\t" + sPattTxt + "\t" + sDirect + "\t" + sTypCzasu + "\t" + sPlanTime + "\t" + sActTime;

                if (sTypCzasu == "PREDICTED")
                {
                    if (sPlanTime != "!ERR!" && sActTime != "!ERR!")
                    {
                        int iAct = 0;
                        int iPlan = 0;
                        try
                        {
                            int iMin = 0;
                            int iHrs = 0;
                            if(!int.TryParse(sActTime.Substring(0, 2), out iHrs)) iHrs = 0;
                            if (!int.TryParse(sActTime.Substring(3, 2), out iMin)) iMin = 0;
                            iAct = iHrs * 60 + iMin;
                            if (!int.TryParse(sPlanTime.Substring(0, 2), out iHrs)) iHrs = 0;
                            if (int.TryParse(sPlanTime.Substring(3, 2), out iMin)) iMin=0;
                            iPlan = iHrs * 60 + iMin;
                        }
                        catch 
                        {
                        }

                        if (iAct > 0 && iPlan > 0)
                        {
                            int iDelay = iAct - iPlan;
                            sTxt = sTxt + "\t" + iDelay;
                            oItem.iEntriesCount += 1;
                            oItem.iSumDelay = oItem.iSumDelay + iDelay;
                            oItem.iMaxDelay = Math.Max(oItem.iMaxDelay, iDelay);
                        }
                    }
                }

                sTxt = sTxt + "\n";
            }

            sTxt = sTxt + "\n";
        }

        p.k.ClipPut(sTxt);
        // sygnalizacja kiedy bylo ostatnie
        mdOpoznLastDate = DateTime.Now;
        return true;
    }

    public bool OpoznieniaGetStat(int iType, ref int iSumDelay, ref int cItems, ref int iMaxDelay)
    {
        // iType: 1: tram, 2:bus
        string sCat;
        switch (iType)
        {
            case 1:
                    sCat = "tram";
                    break;
            case 2:
                    sCat = "bus";
                    break;
            default:
                    return false;
        }

        iSumDelay = moItemy.Where(s => (s.Cat == sCat)).Sum(s => s.iSumDelay);
        cItems = moItemy.Where(s => (s.Cat == sCat)).Sum(s => s.iEntriesCount);
        iMaxDelay = moItemy.Where(s => (s.Cat == sCat)).Max(s => s.iMaxDelay);

        return true;    // error
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


        foreach (Przystanek oItem in moItemy)
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
}
