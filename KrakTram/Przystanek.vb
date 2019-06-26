'Imports System.Net.Http
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
End Class



Public Class Przystanki
    Private moItemy As Collection(Of Przystanek) = New Collection(Of Przystanek)
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
            App.DialogBoxRes("resErrorNoNetwork")
            Return False
        End If

        ' kiedys to bylo po testach, teraz - przed wczytaniem
        Dim oItemyOld As Collection(Of Przystanek) = moItemy  ' czy to skopiuje zawartosc?

        moItemy.Clear()

        Dim sRetMsg As String = Await ImportMain("http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000")

        If sRetMsg <> "" Then
            If sRetMsg.Substring(0, 3) = "res" Then
                Await App.DialogBoxRes(sRetMsg)
            Else
                App.DialogBox(sRetMsg)
            End If
            Return False    ' error byl, lista pusta
        End If

        If App.GetSettingsBool("settingsAlsoBus") Or True Then  ' wczytujemy zawsze...

            sRetMsg = Await ImportMain("http://91.223.13.70/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000")

            If sRetMsg <> "" Then
                If sRetMsg.Substring(0, 3) = "res" Then
                    Await App.DialogBoxRes(sRetMsg)
                Else
                    App.DialogBox(sRetMsg)
                End If
                ' byl error - ale tramwaje są, więc kontynuujemy
            End If

        End If

        Await Save()    ' teoretycznie mogloby byc bez Await, zeby sobie w tle robil Save
        App.SetSettingsInt("LastLoadStops", CInt(Date.Now.ToString("yyMMdd")))

        If App.GetSettingsBool("pkarmode") Then
            Await Compare(oItemyOld, moItemy)
        End If

        Return True
    End Function

    Public Async Function LoadOrImport(bForceLoad As Boolean) As Task

        Dim iHowOld As Integer
        Try ' 20171108: czasem przy starcie wylatuje, może tu?
            iHowOld = CInt(Date.Now.ToString("yyMMdd")) - App.GetSettingsInt("LastLoadStops")
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
            Await App.DialogBoxRes(
                App.GetLangString("resChangesInStopList") & vbCrLf &
                sDiffsDel & vbCrLf & sDiffsNew)
        End If

    End Function
End Class
