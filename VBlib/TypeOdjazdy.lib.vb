Partial Public Class JedenOdjazd
    Public Property Linia As String
    Public Property iLinia As Integer
    Public Property Typ As String
    Public Property Kier As String
    Public Property Przyst As String
    Public Property Mins As String
    Public Property PlanTime As String
    Public Property ActTime As String
    Public Property Delay As Integer
    Public Property Odl As Integer
    Public Property TimeSec As Integer
    Public Property bShow As Boolean = True
    Public Property odlMin As Integer
    Public Property uiCol1 As Integer
    Public Property uiCol3 As Integer
    Public Property sPrzystCzas As String
    Public Property bPkarMode As Boolean
    Public Property sRawData As String
End Class

Partial Public Class ListaOdjazdow
    Public sLastError As String = ""

    Private moOdjazdy As ObjectModel.Collection(Of JedenOdjazd)

    Public Function GetItems() As ObjectModel.Collection(Of JedenOdjazd)
        Return moOdjazdy
    End Function

    Public Function Count() As Integer
        Return moOdjazdy.Count
    End Function

    Public Sub Clear()
        If moOdjazdy Is Nothing Then moOdjazdy = New ObjectModel.Collection(Of JedenOdjazd)()
        moOdjazdy.Clear()
    End Sub


#Region "typy pojazdów"

    Private Shared Function VehicleId2VehicleType68(sTmp As String) As String
        If String.IsNullOrEmpty(sTmp) OrElse sTmp.Length < 15 Then Return ""
        If Not Equals(If(sTmp.Substring(0, 15), ""), "635218529567218") Then Return ""
        Dim id As Integer
        If Not Integer.TryParse(sTmp.Substring(15), id) Then id = 0
        id -= 736
        If id = 831 Then id = 216
        If id < 100 Then Return "???"
        If id < 200 Then Return "E1"  ' lowfloor=0
        If id < 300 Then Return "105N"  ' lowfloor=0
        If id < 400 Then Return "GT8"  ' lowfloor=0, dla 313 i 323 low=1; 
        If id < 450 Then Return "EU8N"  ' lowfloor=1
        If id < 500 Then Return "N8"  ' lowfloor=1
        If id < 600 Then Return "???"
        If id < 700 Then Return "NGT6"  ' lowfloor=2
        If id < 800 Then Return "???"
        If id < 890 Then Return "NGT8"
        If id = 899 Then Return "126N"
        If id < 990 Then Return "2014N"
        If id = 990 Then Return "405N-Kr"
        Return "???"
    End Function

    Private Shared Function VehicleId2VehicleType11(sTmp As String) As String
        If String.IsNullOrEmpty(sTmp) OrElse sTmp.Length < 15 Then Return ""
        If Not Equals(If(sTmp.Substring(0, 15), ""), "-11889502973096") Then Return ""
        ' 123456789.12345

        Dim id As Integer
        If Not Integer.TryParse(sTmp.Substring(15), id) Then id = 0
        If id = 46005 Then Return "405N"
        If id < 46021 Then Return "   "
        If id < 46126 Then Return "2014N"
        If id < 46214 Then Return "NGT8"
        If id < 46396 Then Return "NGT6"
        If id = 46399 Then Return "2014N"
        If id = 46403 Then Return "NGT6"
        If id = 46435 Then Return "N8S-NF"
        If id = 46439 Then Return "N8S-NF"
        If id = 46443 Then Return "GT8"
        If id < 46499 Then Return "   "
        If id < 46580 Then Return "EU8N"
        If id < 46595 Then Return "   "
        If id < 46596 Then Return "GT8"
        If id < 46715 Then Return "   "
        If id < 46764 Then Return "105N"
        If id < 46891 Then Return "   "
        If id < 47142 Then Return "E1"

        ' dalej sa pojedyncze, za duzo roboty

        Return "   "    ' ale nie pytajnikuj, bo nie wiadomo czy nie wiadomo :)
    End Function

    Private Shared Function VehicleId2VehicleType(sTmp As String) As String
        ' https://github.com/jacekkow/mpk-ttss/blob/master/common.js

        Dim sRet = ""
        sRet = VehicleId2VehicleType68(sTmp)
        If Not String.IsNullOrEmpty(sRet) Then Return sRet
        sRet = VehicleId2VehicleType11(sTmp)
        If Not String.IsNullOrEmpty(sRet) Then Return sRet
        Return "    "
    End Function
#End Region

    Public Async Function WczytajTabliczke(sCat As String, sErrData As String, iId As Integer, iOdl As Integer, mSpeed As Double, bPkarMode As Boolean) As Task(Of String)
        ' mSpeed = App.mSpeed
        ' Dim bPkarMode As Boolean = p.k.GetSettingsBool("pkarmode", p.k.IsThisMoje())
        'oNew.uiCol1 = GetSettingsInt("widthCol0")  ' linia, typ pojazdu
        'oNew.uiCol3 = GetSettingsInt("widthCol3")  ' czas odjazdu
        'If iCurrSec > iOldSec + 60 * GetSettingsInt("alsoNext", 5) Then

        Dim oJson As Newtonsoft.Json.Linq.JObject = Await VBlib.App.WczytajTabliczke(sCat, sErrData, iId)
        If oJson Is Nothing Then
            Return VBlib.App.sLastError
        End If

        Dim oJsonStops As Newtonsoft.Json.Linq.JArray = New Newtonsoft.Json.Linq.JArray()

        Try
            oJsonStops = CType(oJson("actual"), Newtonsoft.Json.Linq.JArray)
        Catch
            Return "ERROR: JSON ""actual"" array missing in " & sErrData
        End Try

        If oJsonStops.Count < 1 Then Return "" ' przeciez tabliczka moze byc pusta (po kursach, przystanek nieczynny...)

        ' Dim iMinSec As Integer = 3600 * iOdl / (App.GetSettingsInt("walkSpeed", 4) * 1000)
        ' 20171108: nie walkspeed, ale aktualna szybkosc (nie mniej niz walkSpeed)

        Dim iMinSec As Integer

        If mSpeed < 1 Then
            iMinSec = 0
        Else
            iMinSec = 3.6 * iOdl / mSpeed
        End If


        For Each oVal As Newtonsoft.Json.Linq.JObject In oJsonStops
            Dim iCurrSec = 0

            Try
                iCurrSec = CInt(oVal("actualRelativeTime"))
            Catch
            End Try

            If iCurrSec > iMinSec Then
                Dim oNew As JedenOdjazd = New JedenOdjazd()

                Try
                    oNew.Linia = "!ERR!"

                    Try
                        oNew.Linia = CStr(oVal("patternText"))
                    Catch
                    End Try

                    Dim argresult = 0

                    If Integer.TryParse(oNew.Linia, argresult) Then
                        oNew.iLinia = argresult
                    Else
                        oNew.iLinia = 9999
                    End If  ' trafia na koniec

                    oNew.Typ = "!ERR!" '  VehicleId2VehicleType(oVal.GetObject().GetNamedString("vehicleId", "!ERR!"));

                    Try
                        oNew.Typ = CStr(oVal("vehicleId"))
                        oNew.Typ = VehicleId2VehicleType(oNew.Typ)
                    Catch
                    End Try

                    ' oVal.GetObject().GetNamedString("direction", "!error!");
                    Try
                        oNew.Kier = CStr(oVal("direction"))
                    Catch
                    End Try

                    If String.IsNullOrEmpty(oNew.Kier) Then oNew.Kier = "!error!"

                    '  oVal.GetObject().GetNamedString("mixedTime", "!ERR!").Replace("%UNIT_MIN%", "min").Replace("Min", "min");
                    Try
                        oNew.Mins = CStr(oVal("mixedTime"))
                    Catch
                    End Try

                    If String.IsNullOrEmpty(oNew.Mins) Then oNew.Mins = "!ERR!"
                    oNew.Mins = oNew.Mins.Replace("%UNIT_MIN%", "min").Replace("Min", "min")

                    ' "Plan: " + oVal.GetObject().GetNamedString("plannedTime", "!ERR!");
                    Try
                        oNew.PlanTime = CStr(oVal("plannedTime"))
                    Catch
                    End Try

                    If String.IsNullOrEmpty(oNew.PlanTime) Then oNew.PlanTime = "!ERR!"
                    oNew.PlanTime = "Plan: " & oNew.PlanTime

                    ' "Real: " + oVal.GetObject().GetNamedString("actualTime", "!ERR!");
                    Try
                        oNew.ActTime = CStr(oVal("actualTime"))
                    Catch
                    End Try

                    If String.IsNullOrEmpty(oNew.ActTime) Then oNew.ActTime = "!ERR!"
                    oNew.ActTime = "Real: " & oNew.ActTime

                    ' oJson.GetObject().GetNamedString("stopName", "!error!");
                    Try
                        oNew.Przyst = CStr(oJson("stopName"))
                    Catch
                    End Try

                    If String.IsNullOrEmpty(oNew.Przyst) Then oNew.Przyst = "!error!"
                    oNew.Odl = iOdl
                    oNew.TimeSec = iCurrSec
                    oNew.odlMin = iMinSec / 60
                    oNew.uiCol1 = GetSettingsInt("widthCol0") ' linia, typ pojazdu
                    oNew.uiCol3 = GetSettingsInt("widthCol3") ' czas odjazdu
                    oNew.sPrzystCzas = $"{oNew.Przyst} ({oNew.Odl} m, {oNew.odlMin} min)"
                    oNew.bPkarMode = False  ' Windows.UI.Xaml.Visibility.Collapsed
                    oNew.sRawData = ""

                    If bPkarMode Then
                        oNew.bPkarMode = True   ' Windows.UI.Xaml.Visibility.Visible
                        oNew.sRawData = oVal.ToString().Replace(",""", "," & vbLf & """")
                    End If

                    ' oNode.SetAttribute("numer",
                    ' oVal.GetObject.GetNamedString("vehicleId", "12345678901234599999").Substring(15))
                    ' oNode.SetAttribute("odlSec", iMinSec)


                    Dim bBylo = False

                    For Each oTmp In moOdjazdy

                        If oTmp.Kier = oNew.Kier AndAlso oTmp.Linia = oNew.Linia Then
                            Dim iOldSec = oTmp.TimeSec

                            If iCurrSec > iOldSec + 60 * GetSettingsInt("alsoNext") Then '  GetSettingsInt("alsoNext", 5) 
                                bBylo = True
                                Exit For
                            End If
                        End If
                    Next

                    If Not bBylo Then moOdjazdy.Add(oNew)
                Catch
                End Try
            End If
        Next

        Return ""   ' wszystko OK
    End Function

    Public Sub OdfiltrujMiedzytabliczkowo()
        ' usuwa z oXml to co powinien :) - czyli te same tramwaje z dalszych przystankow
        ' <root><item ..>
        ' o tabliczce: stop, odl, odlMin, odlSec - nazwa, odleglosc: metry, minuty, sec
        ' o tramwaju: line, dir, time, timSec, typ, numer - linia, kierunek, mixedTime, sekundy, typ (eu8n), numer wozu

        For Each oNode In moOdjazdy

            If oNode.bShow Then
                For Each oNode1 In moOdjazdy

                    If oNode.Linia = oNode1.Linia Then
                        If oNode.odlMin < oNode1.odlMin Then oNode1.bShow = False
                    End If
                Next
            End If
        Next
    End Sub

    Public Sub FiltrWedleKierunku(ByVal bExclude As Boolean, ByVal sKier As String)
        ' bExclude = True, usun ten kierunek
        ' = False, tylko ten kierunek

        For Each oNode In moOdjazdy
            If bExclude AndAlso oNode.Kier = sKier Then oNode.bShow = False
            If Not bExclude AndAlso oNode.Kier <> sKier Then oNode.bShow = False
        Next
    End Sub
End Class
