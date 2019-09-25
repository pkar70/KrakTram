
Public Class JedenOdjazd
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
    Public Property bPkarMode As Visibility
    Public Property sRawData As String
End Class

Public Class ListaOdjazdow
    Private moOdjazdy As Collection(Of JedenOdjazd)

    Public Function GetItems() As Collection(Of JedenOdjazd)
        Return moOdjazdy
    End Function

    Public Function Count() As Integer
        Return moOdjazdy.Count
    End Function

    Public Sub Clear()
        If moOdjazdy Is Nothing Then moOdjazdy = New Collection(Of JedenOdjazd)
        moOdjazdy.Clear()
    End Sub


    ' przemigrowane do App, bo wykorzystywane takze w innym miejscu
    'Private Async Function WebPageAsync(sUri As String, bNoRedir As Boolean) As Task(Of String)
    '    Dim sTmp As String = ""

    '    If Not Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
    '        DialogBoxRes("resErrorNoNetwork")
    '        Return ""
    '    End If

    '    Dim oHttp As System.Net.Http.HttpClient
    '    If bNoRedir Then
    '        Dim oHCH As System.Net.Http.HttpClientHandler = New System.Net.Http.HttpClientHandler
    '        oHCH.AllowAutoRedirect = False
    '        oHttp = New System.Net.Http.HttpClient(oHCH)
    '    Else
    '        oHttp = New System.Net.Http.HttpClient()
    '    End If

    '    Dim sPage As String = ""


    '    Dim bError As Boolean = False

    '    oHttp.Timeout = TimeSpan.FromSeconds(8)


    '    Try
    '        sPage = Await oHttp.GetStringAsync(New Uri(sUri))
    '    Catch ex As Exception
    '        bError = True
    '    End Try
    '    If bError Then
    '        DialogBoxRes("resErrorGetHttp")
    '        Return ""
    '    End If

    '    Return sPage
    'End Function


    Private Shared Function VehicleId2VehicleType68(sTmp As String) As String
        If sTmp.Length < 15 Then Return ""
        If sTmp.Substring(0, 15) <> "635218529567218" Then Return ""
        Dim id As Integer = CInt(sTmp.Substring(15)) - 736
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
        If sTmp.Length < 15 Then Return ""
        If sTmp.Substring(0, 15) <> "-11889502973096" Then Return ""
        '                            123456789.12345

        Dim id As Integer = CInt(sTmp.Substring(15))
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

        Dim sRet As String = ""
        sRet = VehicleId2VehicleType68(sTmp)
        If sRet <> "" Then Return sRet
        sRet = VehicleId2VehicleType11(sTmp)
        If sRet <> "" Then Return sRet
        Return "    "

    End Function


    Public Async Function WczytajTabliczke(sCat As String, iId As Integer, iOdl As Integer) As Task

        'Dim sUrl As String
        'If sCat = "bus" Then
        '    sUrl = "http://91.223.13.70"
        'Else
        '    sUrl = "http://www.ttss.krakow.pl"
        'End If
        'sUrl = sUrl & "/internetservice/services/passageInfo/stopPassages/stop?mode=departure&stop="
        'Dim sPage As String = Await App.WebPageAsync(sUrl & iId, False)
        'If sPage = "" Then Exit Function

        Dim bError As Boolean
        'Dim oJson As Windows.Data.Json.JsonObject = Nothing
        'Try
        '    oJson = Windows.Data.Json.JsonObject.Parse(sPage)
        'Catch ex As Exception
        '    bError = True
        'End Try
        'If bError Then
        '    DialogBox("ERROR: JSON parsing error - tablica")
        '    Exit Function
        'End If

        Dim oJson As Windows.Data.Json.JsonObject = Await App.WczytajTabliczke(sCat, iId)
        If oJson Is Nothing Then Return

        Dim oJsonStops As New Windows.Data.Json.JsonArray
        Try
            oJsonStops = oJson.GetNamedArray("actual")
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            DialogBox("ERROR: JSON ""actual"" array missing")
            Return
        End If

        If oJsonStops.Count = 0 Then
            ' przeciez tabliczka moze byc pusta (po kursach, przystanek nieczynny...)
            Return
        End If

        ' Dim iMinSec As Integer = 3600 * iOdl / (App.GetSettingsInt("walkSpeed", 4) * 1000)
        ' 20171108: nie walkspeed, ale aktualna szybkosc (nie mniej niz walkSpeed)

        Dim iMinSec As Integer
        If App.mSpeed < 1 Then
            iMinSec = 0
        Else
            iMinSec = 3.6 * iOdl / App.mSpeed
        End If


        Dim bPkarMode As Boolean = GetSettingsBool("pkarmode")


        For Each oVal As Windows.Data.Json.IJsonValue In oJsonStops

            Dim iCurrSec As Integer = oVal.GetObject.GetNamedNumber("actualRelativeTime", 0)

            If iCurrSec > iMinSec Then  ' tylko kiedy mozna zdążyć
                Dim oNew As JedenOdjazd = New JedenOdjazd

                Try
                    oNew.Linia = oVal.GetObject.GetNamedString("patternText", "!ERR!")

                    oNew.iLinia = 999   ' trafia na koniec
                    Integer.TryParse(oNew.Linia, oNew.iLinia)

                    oNew.Typ = VehicleId2VehicleType(oVal.GetObject.GetNamedString("vehicleId", "!ERR!"))
                    oNew.Kier = oVal.GetObject.GetNamedString("direction", "!error!")
                    oNew.Mins = oVal.GetObject.GetNamedString("mixedTime", "!ERR!").Replace("%UNIT_MIN%", "min").Replace("Min", "min")
                    oNew.PlanTime = "Plan: " & oVal.GetObject.GetNamedString("plannedTime", "!ERR!")
                    oNew.ActTime = "Real: " & oVal.GetObject.GetNamedString("actualTime", "!ERR!")
                    oNew.Przyst = oJson.GetObject.GetNamedString("stopName", "!error!")
                    oNew.Odl = iOdl
                    oNew.TimeSec = iCurrSec
                    oNew.odlMin = iMinSec \ 60
                    oNew.uiCol1 = GetSettingsInt("widthCol0")
                    oNew.uiCol3 = GetSettingsInt("widthCol3")

                    oNew.sPrzystCzas = oNew.Przyst & " (" & oNew.Odl & " m, " & oNew.odlMin & " min)"

                    oNew.bPkarMode = Visibility.Collapsed
                    oNew.sRawData = ""
                    If bPkarMode Then
                        oNew.bPkarMode = Visibility.Visible
                        oNew.sRawData = oVal.ToString.Replace(",""", "," & vbCrLf & """")
                    End If

                    'oNode.SetAttribute("numer",
                    '    oVal.GetObject.GetNamedString("vehicleId", "12345678901234599999").Substring(15))
                    'oNode.SetAttribute("odlSec", iMinSec)


                    Dim bBylo As Boolean = False

                    For Each oTmp As JedenOdjazd In moOdjazdy
                        If oTmp.Kier = oNew.Kier AndAlso oTmp.Linia = oNew.Linia Then
                            Dim iOldSec As Integer = oTmp.TimeSec
                            If iCurrSec > iOldSec + 60 * GetSettingsInt("alsoNext", 5) Then
                                bBylo = True
                                Exit For
                            End If
                        End If
                    Next

                    If Not bBylo Then moOdjazdy.Add(oNew)

                Catch ex As Exception
                    ' jakby jakichś danych nie było, pomiń
                End Try


            End If

        Next

    End Function
    Public Sub OdfiltrujMiedzytabliczkowo()
        ' usuwa z oXml to co powinien :) - czyli te same tramwaje z dalszych przystankow
        ' <root><item ..>
        ' o tabliczce: stop, odl, odlMin, odlSec - nazwa, odleglosc: metry, minuty, sec
        ' o tramwaju: line, dir, time, timSec, typ, numer - linia, kierunek, mixedTime, sekundy, typ (eu8n), numer wozu

        For Each oNode As JedenOdjazd In moOdjazdy
            If oNode.bShow Then
                For Each oNode1 As JedenOdjazd In moOdjazdy
                    If oNode.Linia = oNode1.Linia Then
                        If oNode.odlMin < oNode1.odlMin Then oNode1.bShow = False
                    End If
                Next
            End If
        Next

    End Sub

    Public Sub FiltrWedleKierunku(bExclude As Boolean, sKier As String)
        ' bExclude = True, usun ten kierunek
        '           = False, tylko ten kierunek

        For Each oNode As JedenOdjazd In moOdjazdy
            If bExclude AndAlso oNode.Kier = sKier Then oNode.bShow = False
            If Not bExclude AndAlso oNode.Kier <> sKier Then oNode.bShow = False
        Next
    End Sub

End Class
