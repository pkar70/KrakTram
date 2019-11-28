using System;
using System.Linq;
using System.Threading.Tasks;
//using Microsoft.VisualBasic;
//using Microsoft.VisualBasic.CompilerServices;

namespace KrakTram
{
    public partial class JedenOdjazd
    {
        public string Linia { get; set; }
        public int iLinia { get; set; }
        public string Typ { get; set; }
        public string Kier { get; set; }
        public string Przyst { get; set; }
        public string Mins { get; set; }
        public string PlanTime { get; set; }
        public string ActTime { get; set; }
        public int Delay { get; set; }
        public int Odl { get; set; }
        public int TimeSec { get; set; }
        public bool bShow { get; set; } = true;
        public int odlMin { get; set; }
        public int uiCol1 { get; set; }
        public int uiCol3 { get; set; }
        public string sPrzystCzas { get; set; }
        public Windows.UI.Xaml.Visibility bPkarMode { get; set; }
        public string sRawData { get; set; }
    }

    public partial class ListaOdjazdow
    {
        private System.Collections.ObjectModel.Collection<JedenOdjazd> moOdjazdy;

        public System.Collections.ObjectModel.Collection<JedenOdjazd> GetItems()
        {
            return moOdjazdy;
        }

        public int Count()
        {
            return moOdjazdy.Count;
        }

        public void Clear()
        {
            if (moOdjazdy == null)
                moOdjazdy = new System.Collections.ObjectModel.Collection<JedenOdjazd>();
            moOdjazdy.Clear();
        }


        // przemigrowane do App, bo wykorzystywane takze w innym miejscu
        // Private Async Function WebPageAsync(sUri As String, bNoRedir As Boolean) As Task(Of String)
        // Dim sTmp As String = ""

        // If Not Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
        // DialogBoxRes("resErrorNoNetwork")
        // Return ""
        // End If

        // Dim oHttp As System.Net.Http.HttpClient
        // If bNoRedir Then
        // Dim oHCH As System.Net.Http.HttpClientHandler = New System.Net.Http.HttpClientHandler
        // oHCH.AllowAutoRedirect = False
        // oHttp = New System.Net.Http.HttpClient(oHCH)
        // Else
        // oHttp = New System.Net.Http.HttpClient()
        // End If

        // Dim sPage As String = ""


        // Dim bError As Boolean = False

        // oHttp.Timeout = TimeSpan.FromSeconds(8)


        // Try
        // sPage = Await oHttp.GetStringAsync(New Uri(sUri))
        // Catch ex As Exception
        // bError = True
        // End Try
        // If bError Then
        // DialogBoxRes("resErrorGetHttp")
        // Return ""
        // End If

        // Return sPage
        // End Function


        private static string VehicleId2VehicleType68(string sTmp)
        {
            if (string.IsNullOrEmpty(sTmp) || sTmp.Length < 15)
                return "";
            if ((sTmp.Substring(0, 15) ?? "") != "635218529567218")
                return "";
            int id;
            if (!int.TryParse(sTmp.Substring(15), out id)) id = 0;
            id -= 736;
            if (id == 831)
                id = 216;
            if (id < 100)
                return "???";

            if (id < 200)
                return "E1";  // lowfloor=0
            if (id < 300)
                return "105N";  // lowfloor=0
            if (id < 400)
                return "GT8";  // lowfloor=0, dla 313 i 323 low=1; 
            if (id < 450)
                return "EU8N";  // lowfloor=1
            if (id < 500)
                return "N8";  // lowfloor=1
            if (id < 600)
                return "???";
            if (id < 700)
                return "NGT6";  // lowfloor=2
            if (id < 800)
                return "???";
            if (id < 890)
                return "NGT8";
            if (id == 899)
                return "126N";
            if (id < 990)
                return "2014N";
            if (id == 990)
                return "405N-Kr";

            return "???";
        }

        private static string VehicleId2VehicleType11(string sTmp)
        {
            if (string.IsNullOrEmpty(sTmp) || sTmp.Length < 15)
                return "";
            if ((sTmp.Substring(0, 15) ?? "") != "-11889502973096")
                return "";
            // 123456789.12345

            int id;
            if(!int.TryParse(sTmp.Substring(15), out id)) id=0;
            if (id == 46005)
                return "405N";
            if (id < 46021)
                return "   ";
            if (id < 46126)
                return "2014N";
            if (id < 46214)
                return "NGT8";
            if (id < 46396)
                return "NGT6";
            if (id == 46399)
                return "2014N";
            if (id == 46403)
                return "NGT6";
            if (id == 46435)
                return "N8S-NF";
            if (id == 46439)
                return "N8S-NF";
            if (id == 46443)
                return "GT8";
            if (id < 46499)
                return "   ";
            if (id < 46580)
                return "EU8N";
            if (id < 46595)
                return "   ";
            if (id < 46596)
                return "GT8";
            if (id < 46715)
                return "   ";
            if (id < 46764)
                return "105N";
            if (id < 46891)
                return "   ";
            if (id < 47142)
                return "E1";

            // dalej sa pojedyncze, za duzo roboty

            return "   ";    // ale nie pytajnikuj, bo nie wiadomo czy nie wiadomo :)
        }

        private static string VehicleId2VehicleType(string sTmp)
        {
            // https://github.com/jacekkow/mpk-ttss/blob/master/common.js

            string sRet = "";
            sRet = VehicleId2VehicleType68(sTmp);
            if (!string.IsNullOrEmpty(sRet))
                return sRet;
            sRet = VehicleId2VehicleType11(sTmp);
            if (!string.IsNullOrEmpty(sRet))
                return sRet;
            return "    ";
        }


        public async Task WczytajTabliczke(string sCat, string sErrData, int iId, int iOdl)
        {

            // Dim sUrl As String
            // If sCat = "bus" Then
            // sUrl = "http://91.223.13.70"
            // Else
            // sUrl = "http://www.ttss.krakow.pl"
            // End If
            // sUrl = sUrl & "/internetservice/services/passageInfo/stopPassages/stop?mode=departure&stop="
            // Dim sPage As String = Await App.WebPageAsync(sUrl & iId, False)
            // If sPage = "" Then Exit Function

            bool bError = false;
            // Dim oJson As Windows.Data.Json.JsonObject = Nothing
            // Try
            // oJson = Windows.Data.Json.JsonObject.Parse(sPage)
            // Catch ex As Exception
            // bError = True
            // End Try
            // If bError Then
            // DialogBox("ERROR: JSON parsing error - tablica")
            // Exit Function
            // End If

            Newtonsoft.Json.Linq.JObject oJson = await KrakTram.App.WczytajTabliczke(sCat, sErrData, iId);
            if (oJson == null)
                return;

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
                p.k.DialogBox("ERROR: JSON \"actual\" array missing in " + sErrData);
                return;
            }

            if (oJsonStops.Count == 0)
                // przeciez tabliczka moze byc pusta (po kursach, przystanek nieczynny...)
                return;

            // Dim iMinSec As Integer = 3600 * iOdl / (App.GetSettingsInt("walkSpeed", 4) * 1000)
            // 20171108: nie walkspeed, ale aktualna szybkosc (nie mniej niz walkSpeed)

            int iMinSec;
            if (KrakTram.App.mSpeed < 1)
                iMinSec = 0;
            else
                iMinSec = (int)(3.6 * iOdl / App.mSpeed);


            bool bPkarMode = p.k.GetSettingsBool("pkarmode",p.k.IsThisMoje());


            foreach (Newtonsoft.Json.Linq.JObject oVal in oJsonStops)
            {
                int iCurrSec = 0;
                try { iCurrSec = (int)oVal["actualRelativeTime"]; } catch { }

                if (iCurrSec > iMinSec)
                {
                    JedenOdjazd oNew = new JedenOdjazd();

                    try
                    {
                        oNew.Linia = "!ERR!";
                        try { oNew.Linia = (string)oVal["patternText"]; } catch { }

                        int argresult = 0;
                        if (int.TryParse(oNew.Linia, out argresult))
                            oNew.iLinia = argresult;
                        else
                            oNew.iLinia = 9999;  // trafia na koniec


                        oNew.Typ = "!ERR!"; //  VehicleId2VehicleType(oVal.GetObject().GetNamedString("vehicleId", "!ERR!"));
                        try
                        {
                            oNew.Typ = (string)oVal["vehicleId"];
                            oNew.Typ = VehicleId2VehicleType(oNew.Typ);
                        }
                        catch { }

                        // oVal.GetObject().GetNamedString("direction", "!error!");
                        try { oNew.Kier = (string)oVal["direction"]; } catch { }
                        if (string.IsNullOrEmpty(oNew.Kier)) oNew.Kier = "!error!";

                        //  oVal.GetObject().GetNamedString("mixedTime", "!ERR!").Replace("%UNIT_MIN%", "min").Replace("Min", "min");
                        try { oNew.Mins = (string)oVal["mixedTime"]; } catch { }
                        if (string.IsNullOrEmpty(oNew.Mins)) oNew.Mins = "!ERR!";
                        oNew.Mins = oNew.Mins.Replace("%UNIT_MIN%", "min").Replace("Min", "min");

                        // "Plan: " + oVal.GetObject().GetNamedString("plannedTime", "!ERR!");
                        try { oNew.PlanTime = (string)oVal["plannedTime"]; } catch { }
                        if (string.IsNullOrEmpty(oNew.PlanTime)) oNew.PlanTime = "!ERR!";
                        oNew.PlanTime = "Plan: " + oNew.PlanTime;

                        // "Real: " + oVal.GetObject().GetNamedString("actualTime", "!ERR!");
                        try { oNew.ActTime = (string)oVal["actualTime"]; } catch { }
                        if(string.IsNullOrEmpty(oNew.ActTime)) oNew.ActTime = "!ERR!";
                        oNew.ActTime = "Real: " + oNew.ActTime;

                        // oJson.GetObject().GetNamedString("stopName", "!error!");
                        try { oNew.Przyst = (string)oJson["stopName"]; } catch { }
                        if (string.IsNullOrEmpty(oNew.Przyst)) oNew.Przyst = "!error!";

                        oNew.Odl = iOdl;
                        oNew.TimeSec = iCurrSec;
                        oNew.odlMin = iMinSec / 60;
                        oNew.uiCol1 = p.k.GetSettingsInt("widthCol0");
                        oNew.uiCol3 = p.k.GetSettingsInt("widthCol3");

                        oNew.sPrzystCzas = oNew.Przyst + " (" + oNew.Odl + " m, " + oNew.odlMin + " min)";

                        oNew.bPkarMode = Windows.UI.Xaml.Visibility.Collapsed;
                        oNew.sRawData = "";
                        if (bPkarMode)
                        {
                            oNew.bPkarMode = Windows.UI.Xaml.Visibility.Visible;
                            oNew.sRawData = oVal.ToString().Replace(",\"", ",\n\"");
                        }

                        // oNode.SetAttribute("numer",
                        // oVal.GetObject.GetNamedString("vehicleId", "12345678901234599999").Substring(15))
                        // oNode.SetAttribute("odlSec", iMinSec)


                        bool bBylo = false;

                        foreach (JedenOdjazd oTmp in moOdjazdy)
                        {
                            if ((oTmp.Kier ?? "") == (oNew.Kier ?? "") && (oTmp.Linia ?? "") == (oNew.Linia ?? ""))
                            {
                                int iOldSec = oTmp.TimeSec;
                                if (iCurrSec > iOldSec + 60 * p.k.GetSettingsInt("alsoNext", 5))
                                {
                                    bBylo = true;
                                    break;
                                }
                            }
                        }

                        if (!bBylo)
                            moOdjazdy.Add(oNew);
                    }
                    catch 
                    {
                    }
                }
            }

        }
        public void OdfiltrujMiedzytabliczkowo()
        {
            // usuwa z oXml to co powinien :) - czyli te same tramwaje z dalszych przystankow
            // <root><item ..>
            // o tabliczce: stop, odl, odlMin, odlSec - nazwa, odleglosc: metry, minuty, sec
            // o tramwaju: line, dir, time, timSec, typ, numer - linia, kierunek, mixedTime, sekundy, typ (eu8n), numer wozu

            foreach (JedenOdjazd oNode in moOdjazdy)
            {
                if (oNode.bShow)
                {
                    foreach (JedenOdjazd oNode1 in moOdjazdy)
                    {
                        if ((oNode.Linia ?? "") == (oNode1.Linia ?? ""))
                        {
                            if (oNode.odlMin < oNode1.odlMin)
                                oNode1.bShow = false;
                        }
                    }
                }
            }
        }

        public void FiltrWedleKierunku(bool bExclude, string sKier)
        {
            // bExclude = True, usun ten kierunek
            // = False, tylko ten kierunek

            foreach (JedenOdjazd oNode in moOdjazdy)
            {
                if (bExclude && (oNode.Kier ?? "") == (sKier ?? ""))
                    oNode.bShow = false;
                if (!bExclude && (oNode.Kier ?? "") != (sKier ?? ""))
                    oNode.bShow = false;
            }
        }
    }
}