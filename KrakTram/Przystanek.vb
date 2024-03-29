﻿'Imports System.Net.Http
'Imports System.Net.NetworkInformation
Imports System.Xml.Serialization
'Imports Windows.Data.Json
'Imports Windows.Storage


<XmlType("stop")>
Public Class Przystanek
    <XmlAttribute()>
    Public Property Cat As String
    <XmlAttribute()>
    Public Property Lat As Double
    <XmlAttribute()>
    Public Property Lon As Double
    <XmlAttribute()>
    Public Property Name As String
    <XmlAttribute()>
    Public Property id As String
    <XmlIgnore>
    Public Property iSumDelay As Integer
    <XmlIgnore>
    Public Property iMaxDelay As Integer
    <XmlIgnore>
    Public Property iEntriesCount As Integer
    <XmlIgnore>
    Public Property iEntriesTotal As Integer
End Class



Public Class Przystanki
    Private moItemy As Collection(Of Przystanek) = New Collection(Of Przystanek)

    Private mdOpoznLastDate As Date = Date.Now.AddDays(-5)

    'Private msTyp As String

    'Public Sub New(sType As String)
    '    ' jesli nie "b....", to tramwaj
    '    sType = sType.ToLower
    '    If sType = "" Then
    '        msTyp = "t"
    '    Else
    '        If sType.Substring(0, 1) = "b" Then
    '            msTyp = "b"
    '        Else
    '            msTyp = "t"
    '        End If
    '    End If
    'End Sub

    ' Add
    Private Sub Add(sCat As String, dLatTtss As Double, dLonTtss As Double, sName As String, sId As String)
        Dim oNew As Przystanek = New Przystanek
        oNew.Cat = sCat
        oNew.id = sId
        oNew.Name = sName
        oNew.Lat = dLatTtss / 3600000.0
        oNew.Lon = dLonTtss / 3600000.0
        moItemy.Add(oNew) ' błąd resource; dodawanie pustego ("" oraz 0) też powoduje error
    End Sub
    ' Delete
    ' New
    Private Async Function Save() As Task

        Dim oFile As Windows.Storage.StorageFile =
            Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync(
                "stops1.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)

        If oFile Is Nothing Then Exit Function

        Dim oSer As XmlSerializer = New XmlSerializer(GetType(Collection(Of Przystanek)))
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        oSer.Serialize(oStream, moItemy)
        oStream.Dispose()   ' == fclose
    End Function

    ' Load
    Private Async Function Load() As Task(Of Boolean)
        ' ret=false gdy nie jest wczytane

        Dim oObj As Windows.Storage.StorageFile =
            Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("stops1.xml")
        If oObj Is Nothing Then Return False
        Dim oFile As Windows.Storage.StorageFile = TryCast(oObj, Windows.Storage.StorageFile)

        Dim oSer As XmlSerializer = New XmlSerializer(GetType(Collection(Of Przystanek)))
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        moItemy = TryCast(oSer.Deserialize(oStream), Collection(Of Przystanek))

        Return True
    End Function

    Private Async Function ImportMain(sUrl As String) As Task(Of String)
        Dim oHttp As New System.Net.Http.HttpClient()
        Dim sTmp As String = ""
        oHttp.Timeout = TimeSpan.FromSeconds(10)

        Try
            sTmp = Await oHttp.GetStringAsync(sUrl)
        Catch ex As Exception
            Return "resErrorGetHttp"
        End Try

        ' {"stops": [
        '{
        '  "category": "tram",
        '  "id": "6350927454370005230",
        '  "latitude": 180367133,
        '  "longitude": 72043450,
        '  "name": "Os.Piastów",
        '  "shortName": "378"
        '},

        If sTmp.IndexOf("""stops""") < 0 Then Return "resErrorBadTTSSstops"

        Dim oJson As Windows.Data.Json.JsonObject = Nothing
        Try
            oJson = Windows.Data.Json.JsonObject.Parse(sTmp)
        Catch ex As Exception
            Return "ERROR: JSON parsing error"
        End Try

        Dim oJsonStops As New Windows.Data.Json.JsonArray
        Try
            oJsonStops = oJson.GetNamedArray("stops")
        Catch ex As Exception
            Return "ERROR: JSON ""stops"" array missing"
        End Try

        If oJsonStops.Count = 0 Then Return "ERROR: JSON 0 obiektów"

        Try
            For Each oVal As Windows.Data.Json.IJsonValue In oJsonStops
                Dim sName As String
                Dim sCat As String
                Dim sShortName As String
                sName = oVal.GetObject.GetNamedString("name")
                sCat = oVal.GetObject.GetNamedString("category")
                sShortName = oVal.GetObject.GetNamedString("shortName")
                Dim dLat As Double
                Dim dLon As Double
                dLat = oVal.GetObject.GetNamedNumber("latitude")
                dLon = oVal.GetObject.GetNamedNumber("longitude")
                If sName.Length > 2 Then ' - potencjalne uciecie kilku nazw dziwnych, jak "PT"
                    Add(sCat,  ' zawsze bedzie "tram", wiec moze pomijac?
                        dLat,
                       dLon,
                        sName, sShortName)
                End If
            Next
        Catch ex As Exception
            Return "ERROR: at JSON converting"
        End Try

        Return ""

    End Function

    Private Async Function Import() As Task(Of Boolean)
        ' ret=false gdy nieudane wczytanie z sieci

        If Not Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
            DialogBoxRes("resErrorNoNetwork")
            Return False
        End If

        ' kiedys to bylo po testach, teraz - przed wczytaniem
        Dim oItemyOld As Collection(Of Przystanek) = moItemy  ' czy to skopiuje zawartosc?

        moItemy.Clear()

        Dim sRetMsg As String = Await ImportMain("http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000")

        If sRetMsg <> "" Then
            If sRetMsg.Substring(0, 3) = "res" Then
                Await DialogBoxRes(sRetMsg)
            Else
                DialogBox(sRetMsg)
            End If
            ' Return False    ' error byl, lista pusta
            ' nie konczymy, bo druga lista moze byc ok...
        End If

        If GetSettingsBool("settingsAlsoBus") Or True Then  ' wczytujemy zawsze...

            sRetMsg = Await ImportMain("http://91.223.13.70/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000")

            If sRetMsg <> "" Then
                If sRetMsg.Substring(0, 3) = "res" Then
                    Await DialogBoxRes(sRetMsg)
                Else
                    DialogBox(sRetMsg)
                End If
                ' byl error - ale tramwaje są, więc kontynuujemy
            End If

        End If

        Await Save()    ' teoretycznie mogloby byc bez Await, zeby sobie w tle robil Save
        SetSettingsInt("LastLoadStops", CInt(Date.Now.ToString("yyMMdd")))

        If GetSettingsBool("pkarmode") Then
            Await Compare(oItemyOld, moItemy)
        End If

        Return True
    End Function

    Public Async Function LoadOrImport(bForceLoad As Boolean) As Task

        Dim iHowOld As Integer
        Try ' 20171108: czasem przy starcie wylatuje, może tu?
            iHowOld = CInt(Date.Now.ToString("yyMMdd")) - GetSettingsInt("LastLoadStops")
        Catch ex As Exception
            iHowOld = 99
        End Try

        Dim bReaded As Boolean = False
        If Not bForceLoad Then bReaded = Await Load()  ' True gdy udane wczytanie; nie ma sensu czytac gdy wymuszamy import

        If Not bForceLoad Then
            If bReaded And iHowOld < 30 Then Return
        End If

        Await Import()

    End Function

    Public Function GetItem(sName As String, Optional sCat As String = "tram") As Przystanek
        For Each oItem As Przystanek In moItemy
            If oItem.Name = sName AndAlso oItem.Cat = sCat Then Return oItem
        Next
        Return Nothing
    End Function

    Public Function GetList(Optional sCat As String = "tram") As ICollection(Of Przystanek)
        Select Case sCat
            Case "all"
                Return moItemy
            Case "bus"
                ' Return From c In moItemy Where sCat = "bus"
                Return moItemy.Where(Function(s) s.Cat = "bus").ToList
            Case Else
                Return moItemy.Where(Function(s) s.Cat = "tram").ToList
        End Select

    End Function

    Private Async Function Compare(oOld As Collection(Of Przystanek), oNew As Collection(Of Przystanek)) As Task

        Dim sDiffsDel As String = ""

        For Each oItemOld As Przystanek In oOld

            Dim bDalej As Boolean = False
            For Each oItemNew As Przystanek In oNew
                If oItemNew.Name = oItemOld.Name Then
                    bDalej = True
                    Exit For
                End If
            Next

            If Not bDalej Then
                sDiffsDel = sDiffsDel & oItemOld.Name & vbCrLf
            End If

        Next

        Dim sDiffsNew As String = ""

        For Each oItemNew As Przystanek In oNew

            Dim bNowe As Boolean = True
            For Each oItemOld As Przystanek In oOld
                If oItemNew.Name = oItemOld.Name Then
                    bNowe = False
                    Exit For
                End If
            Next

            If bNowe Then
                sDiffsNew = sDiffsNew & oItemNew.Name & vbCrLf
            End If

        Next

        If sDiffsNew <> "" Then sDiffsNew = "Nowe:" & vbCrLf & sDiffsNew
        If sDiffsDel <> "" Then sDiffsDel = "Usunięte:" & vbCrLf & sDiffsDel

        If sDiffsNew <> "" OrElse sDiffsDel <> "" Then
            Await DialogBoxRes(
                GetLangString("resChangesInStopList") & vbCrLf &
                sDiffsDel & vbCrLf & sDiffsNew)
        End If

    End Function


    Public Async Function OpoznieniaFromHttpAsync(iType As Integer) As Task(Of Boolean)
        ' policzenie opóźnień, b0 = bus, b1 = tram
        If mdOpoznLastDate.AddMinutes(5) > Date.Now Then
            If Not Await DialogBoxYN("Niedawno było, na pewno?") Then Return False
        End If

        Dim sCat As String
        Select Case iType
            Case 1
                sCat = "tram"
            Case 2
                sCat = "bus"
            Case Else
                Return False
        End Select

        ' policz
        Dim sTxt As String = ""

        For Each oItem As Przystanek In moItemy
            ' wczytaj dane przystanku
            If oItem.Cat <> sCat Then Continue For

            sTxt = sTxt & oItem.id & vbTab & "Przystanek: " & oItem.Name & vbCrLf

            Dim oJson As Windows.Data.Json.JsonObject = Await App.WczytajTabliczke(oItem.Cat, oItem.Name, oItem.id)
            Dim bError As Boolean = False

            Dim oJsonStops As New Windows.Data.Json.JsonArray
            Try
                oJsonStops = oJson.GetNamedArray("actual")
            Catch ex As Exception
                bError = True
            End Try
            If bError Then
                'DialogBox("ERROR: JSON ""actual"" array missing")
                Continue For
                Return False
            End If

            oItem.iEntriesCount = 0
            oItem.iEntriesTotal = oJsonStops.Count
            oItem.iSumDelay = 0
            oItem.iMaxDelay = 0

            If oJsonStops.Count = 0 Then Continue For

            ' policz...
            For Each oVal As Windows.Data.Json.IJsonValue In oJsonStops
                oItem.iEntriesTotal += 1
                ' jesli PREDICTED (a nie np. PLANNED), to znaczy że liczymy
                Dim sPlanTime As String = oVal.GetObject.GetNamedString("plannedTime", "!ERR!")
                Dim sActTime As String = oVal.GetObject.GetNamedString("actualTime", "!ERR!")
                Dim sTypCzasu As String = oVal.GetObject.GetNamedString("status", "!ERR!")

                sTxt = sTxt & oItem.id & vbTab &
                    oVal.GetObject.GetNamedString("patternText", "!ERR!") & vbTab &
                    oVal.GetObject.GetNamedString("direction", "!error!") & vbTab &
                    sTypCzasu & vbTab & sPlanTime & vbTab & sActTime

                If sTypCzasu = "PREDICTED" Then
                    If sPlanTime <> "!ERR!" AndAlso sActTime <> "!ERR!" Then
                        Dim iAct As Integer = 0
                        Dim iPlan As Integer = 0
                        Try
                            iAct = sActTime.Substring(0, 2) * 60 + sActTime.Substring(3, 2)
                            iPlan = sPlanTime.Substring(0, 2) * 60 + sPlanTime.Substring(3, 2)
                        Catch ex As Exception

                        End Try

                        If iAct > 0 AndAlso iPlan > 0 Then
                            Dim iDelay As Integer = iAct - iPlan
                            sTxt = sTxt & vbTab & iDelay
                            oItem.iEntriesCount += 1
                            oItem.iSumDelay = oItem.iSumDelay + iDelay
                            oItem.iMaxDelay = Math.Max(oItem.iMaxDelay, iDelay)
                        End If

                    End If
                End If

                sTxt = sTxt & vbCrLf
            Next

            sTxt = sTxt & vbCrLf
        Next

        ClipPut(sTxt)
        ' sygnalizacja kiedy bylo ostatnie
        mdOpoznLastDate = Date.Now
        Return True
    End Function

    Public Function OpoznieniaGetStat(iType As Integer, ByRef iSumDelay As Integer, ByRef cItems As Integer, ByRef iMaxDelay As Integer) As Boolean
        ' iType: 1: tram, 2:bus
        Dim sCat As String
        Select Case iType
            Case 1
                sCat = "tram"
            Case 2
                sCat = "bus"
            Case Else
                Return False
        End Select

        iSumDelay = moItemy.Where(Function(s) s.Cat = sCat).Sum(Function(s) s.iSumDelay)
        cItems = moItemy.Where(Function(s) s.Cat = sCat).Sum(Function(s) s.iEntriesCount)
        iMaxDelay = moItemy.Where(Function(s) s.Cat = sCat).Max(Function(s) s.iMaxDelay)

        Return True    ' error
    End Function

    Public Function OpoznieniaDoMapy(bTram As Boolean, bBus As Boolean, oMapCtrl As Maps.MapControl) As Integer
        ' iType: 1: tram, 2:bus, 3: wszystko (ale nie 'other')
        If oMapCtrl Is Nothing Then Return 0

        ' https://docs.microsoft.com/en-us/windows/uwp/maps-and-location/display-poi

        Dim iCnt As Integer = 0

        Dim oBrush2min, oBrush3min, oBrush4min, oBrush5min As SolidColorBrush
        oBrush2min = New SolidColorBrush(Windows.UI.Colors.Yellow)
        oBrush3min = oBrush2min
        oBrush4min = oBrush2min
        oBrush5min = oBrush2min
        oBrush2min.Opacity = 0.3
        oBrush3min.Opacity = 0.4
        oBrush4min.Opacity = 0.5
        oBrush5min.Opacity = 0.6
        Dim oBrush10min, oBrush20min, oBrushMaxmin As SolidColorBrush
        oBrush10min = New SolidColorBrush(Windows.UI.Colors.OrangeRed)
        oBrush20min = New SolidColorBrush(Windows.UI.Colors.Red)
        oBrushMaxmin = New SolidColorBrush(Windows.UI.Colors.DarkRed)
        oBrush10min.Opacity = 0.5
        oBrush20min.Opacity = 0.5
        oBrushMaxmin.Opacity = 0.5


        For Each oItem As Przystanek In moItemy

            If oItem.iEntriesCount = 0 Then Continue For
            Select Case oItem.Cat
                Case "bus"
                    If Not bBus Then Continue For
                Case "tram"
                    If Not bTram Then Continue For
                Case Else
                    Continue For
            End Select

            Dim oNew As Windows.UI.Xaml.Shapes.Ellipse = New Windows.UI.Xaml.Shapes.Ellipse
            oNew.Height = 20
            oNew.Width = 20
            Dim dAvgDelay As Double = 0

            dAvgDelay = oItem.iSumDelay / oItem.iEntriesCount

            If dAvgDelay < 1 Then Continue For

            If dAvgDelay > 2 Then
                If dAvgDelay > 3 Then
                    If dAvgDelay > 4 Then
                        If dAvgDelay > 5 Then
                            If dAvgDelay > 10 Then
                                If dAvgDelay > 20 Then
                                    oNew.Fill = oBrushMaxmin
                                Else ' 10 < x < 20
                                    oNew.Fill = oBrush20min
                                End If
                            Else ' 5 < x < 10
                                oNew.Fill = oBrush10min
                            End If
                        Else ' 4 < x < 5
                            oNew.Fill = oBrush5min
                        End If
                    Else ' 3 < x < 4
                        oNew.Fill = oBrush4min
                    End If
                Else ' 2 < x < 3
                    oNew.Fill = oBrush3min
                End If
            Else ' 1 < x < 2
                oNew.Fill = oBrush2min
            End If

            Dim oPosition As Windows.Devices.Geolocation.BasicGeoposition
            oPosition = New Windows.Devices.Geolocation.BasicGeoposition
            oPosition.Latitude = oItem.Lat
            oPosition.Longitude = oItem.Lon
            Dim oPoint As Windows.Devices.Geolocation.Geopoint
            oPoint = New Windows.Devices.Geolocation.Geopoint(oPosition)

            iCnt += 1
            oMapCtrl.Children.Add(oNew)

            'shared member - ale skąd wie jaka mapa? nie można dwu wyświetlić?
            Windows.UI.Xaml.Controls.Maps.MapControl.SetLocation(oNew, oPoint)
            Windows.UI.Xaml.Controls.Maps.MapControl.SetNormalizedAnchorPoint(oNew, New Point(0.5, 0.5))
        Next

        Return iCnt

    End Function

End Class
