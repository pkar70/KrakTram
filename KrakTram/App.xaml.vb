Imports Windows.Storage
Imports System.Net.Http
Imports Windows.Data.Json
Imports Windows.Data.Xml.Dom
Imports Windows.UI.Popups
Imports Windows.Devices.Geolocation
Imports System.Net.NetworkInformation
Imports System.Xml.Serialization
''' <summary>
''' Provides application-specific behavior to supplement the default Application class.
''' </summary>
NotInheritable Class App
    Inherits Application

    ' Public Shared oStops As New XmlDocument
    Public Shared oStops As New Przystanki
    Public Shared mdLat As Double = 100
    Public Shared mdLong, mSpeed As Double

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
        Private Itemy = New Collection(Of Przystanek)
        ' Add
        Private Sub Add(sCat As String, dLatTtss As Double, dLonTtss As Double, sName As String, sId As String)
            Dim oNew = New Przystanek
            oNew.Cat = sCat
            oNew.id = sId
            oNew.Name = sName
            oNew.Lat = dLatTtss / 3600000.0
            oNew.Lon = dLonTtss / 3600000.0
            Itemy.Add(oNew)
        End Sub
        ' Delete
        ' New
        Private Async Function Save() As Task

            Dim oFile As StorageFile = Await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(
            "stops1.xml", CreationCollisionOption.ReplaceExisting)

            If oFile Is Nothing Then Exit Function

            Dim oSer = New XmlSerializer(GetType(Collection(Of Przystanek)))
            Dim oStream = Await oFile.OpenStreamForWriteAsync
            oSer.Serialize(oStream, Itemy)
            oStream.Dispose()   ' == fclose



        End Function

        ' Load
        Private Async Function Load() As Task(Of Boolean)
            ' ret=false gdy nie jest wczytane

            Dim oObj As StorageFile = Await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(
            "stops1.xml")
            If oObj Is Nothing Then Return False
            Dim oFile = TryCast(oObj, StorageFile)

            Dim oSer = New XmlSerializer(GetType(Collection(Of Przystanek)))
            Dim oStream = Await oFile.OpenStreamForReadAsync
            Itemy = TryCast(oSer.Deserialize(oStream), Collection(Of Przystanek))

            Return True
        End Function

        Private Async Function Import() As Task(Of Boolean)
            ' ret=false gdy nieudane wczytanie z sieci

            If Not NetworkInterface.GetIsNetworkAvailable() Then
                DialogBoxRes("resErrorNoNetwork")
                Return False
            End If

            Dim oHttp As New HttpClient()
            Dim sTmp As String = ""
            oHttp.Timeout = TimeSpan.FromSeconds(10)

            Dim bError = False

            Try
                sTmp = Await oHttp.GetStringAsync("http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000")
            Catch ex As Exception
                bError = True
            End Try
            If bError Then
                DialogBoxRes("resErrorGetHttp")
                Return False
            End If

            ' {"stops": [
            '{
            '  "category": "tram",
            '  "id": "6350927454370005230",
            '  "latitude": 180367133,
            '  "longitude": 72043450,
            '  "name": "Os.Piastów",
            '  "shortName": "378"
            '},

            If sTmp.IndexOf("""stops""") < 0 Then
                DialogBoxRes("resErrorBadTTSSstops")
                Return False
            End If

            Dim oJson As JsonObject
            Try
                oJson = JsonObject.Parse(sTmp)
            Catch ex As Exception
                bError = True
            End Try
            If bError Then
                DialogBox("ERROR: JSON parsing error")
                Return False
            End If

            Dim oJsonStops As New JsonArray
            Try
                oJsonStops = oJson.GetNamedArray("stops")
            Catch ex As Exception
                bError = True
            End Try
            If bError Then
                DialogBox("ERROR: JSON ""stops"" array missing")
                Return False
            End If

            If oJsonStops.Count = 0 Then
                DialogBox("ERROR: JSON 0 obiektów")
                Return False
            End If

            Try
                For Each oVal In oJsonStops
                    Add(oVal.GetObject.GetNamedString("category"),
                        oVal.GetObject.GetNamedNumber("latitude"),
                        oVal.GetObject.GetNamedNumber("longitude"),
                        oVal.GetObject.GetNamedString("name"),
                        oVal.GetObject.GetNamedString("shortName"))
                Next
            Catch ex As Exception
                bError = True
            End Try
            If bError Then
                DialogBox("ERROR: at JSON converting")
                Return False
            End If

            Await Save()    ' teoretycznie mogloby byc bez Await, zeby sobie w tle robil Save
            SetSettingsInt("LastLoadStops", CInt(Date.Now.ToString("yyMMdd")))

            Return True
        End Function

        Public Async Function LoadOrImport(bForceLoad As Boolean) As Task

            Dim iHowOld As Integer
            Try ' 20171108: czasem przy starcie wylatuje, może tu?
                iHowOld = CInt(Date.Now.ToString("yyMMdd")) - GetSettingsInt("LastLoadStops")
            Catch ex As Exception
                iHowOld = 99
            End Try

            Dim bReaded = Await Load()  ' True gdy udane wczytanie

            If Not bForceLoad Then
                If bReaded And iHowOld < 30 Then Return
            End If

            Await Import()

        End Function

        Public Function GetList() As ICollection(Of Przystanek)
            Return Itemy
        End Function
    End Class

    ''' <summary>
    ''' Invoked when the application is launched normally by the end user.  Other entry points
    ''' will be used when the application is launched to open a specific file, to display
    ''' search results, and so forth.
    ''' </summary>
    ''' <param name="e">Details about the launch request and process.</param>
    Protected Overrides Sub OnLaunched(e As Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)
        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If rootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = New Frame()

            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed

            If e.PreviousExecutionState = ApplicationExecutionState.Terminated Then
                ' TODO: Load state from previously suspended application
            End If
            ' Place the frame in the current Window
            Window.Current.Content = rootFrame
        End If

        If e.PrelaunchActivated = False Then
            If rootFrame.Content Is Nothing Then
                ' When the navigation stack isn't restored navigate to the first page,
                ' configuring the new page by passing required information as a navigation
                ' parameter
                SetSettingsBool("noGPSshown", False)
                rootFrame.Navigate(GetType(MainPage), e.Arguments)
            End If

            ' Ensure the current window is active
            Window.Current.Activate()
        End If
    End Sub

    ''' <summary>
    ''' Invoked when Navigation to a certain page fails
    ''' </summary>
    ''' <param name="sender">The Frame which failed navigation</param>
    ''' <param name="e">Details about the navigation failure</param>
    Private Sub OnNavigationFailed(sender As Object, e As NavigationFailedEventArgs)
        Throw New Exception("Failed to load Page " + e.SourcePageType.FullName)
    End Sub

    ''' <summary>
    ''' Invoked when application execution is being suspended.  Application state is saved
    ''' without knowing whether the application will be terminated or resumed with the contents
    ''' of memory still intact.
    ''' </summary>
    ''' <param name="sender">The source of the suspend request.</param>
    ''' <param name="e">Details about the suspend request.</param>
    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs) Handles Me.Suspending
        Dim deferral As SuspendingDeferral = e.SuspendingOperation.GetDeferral()
        ' TODO: Save application state and stop any background activity
        deferral.Complete()
    End Sub

    Public Shared Async Sub DialogBox(sMsg As String)
        Dim oMsg As New MessageDialog(sMsg)
        Await oMsg.ShowAsync
    End Sub
    Public Shared Async Sub DialogBoxRes(sMsg As String)
        ' 20180523, reakcja na niemanie w resources - bo chyba na tym wyleciała app wedle Dashboard (DialogBoxRes.MoveNext)
        Dim sTxt As String
        Try
            sTxt = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(sMsg)
        Catch ex As Exception
            sTxt = "Some error occured"
        End Try
        Dim oMsg As New MessageDialog(sTxt)
        Await oMsg.ShowAsync
    End Sub

    'Public Shared Async Sub DialogBoxResYN(sMsg As String) As Task(Of Integer)
    '    With Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView()
    '        Dim oMsg As New MessageDialog(.GetString(sMsg))
    '        oMsg.Commands.Add(New UICommand(.GetString("resYes")))
    '        oMsg.Commands.Add(New UICommand(.GetString("resNo")))
    '        Await oMsg.ShowAsync
    '    End With
    'End Sub


    Public Shared Async Sub CheckLoadStopList(Optional bForceLoad As Boolean = False)

        Await oStops.LoadOrImport(bForceLoad)
        'Dim bReaded As Boolean = False
        'Dim bError = False  ' 20171108: zeby nie bylo DialogBox (async sub) w ramach Try/Catch

        'Dim oObj = ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("stops.xml")

        'Dim oFile = TryCast(oObj, StorageFile)
        'If oFile IsNot Nothing Then
        '    Dim sTxt As String = Await FileIO.ReadTextAsync(oFile)
        '    Try
        '        oStops.LoadXml(sTxt)
        '        bReaded = True
        '    Catch ex As Exception
        '        bError = True
        '    End Try
        '    If bError Then DialogBox("ERROR loading stops")
        'End If

        'Dim iHowOld As Integer
        'Try ' 20171108: czasem przy starcie wylatuje, może tu?
        '    iHowOld = CInt(Date.Now.ToString("yyMMdd")) - GetSettingsInt("LastLoadStops")
        'Catch ex As Exception
        '    iHowOld = 99
        'End Try

        'If Not bForceLoad Then
        '    If bReaded And iHowOld < 30 Then Exit Sub
        'End If

        'If Not NetworkInterface.GetIsNetworkAvailable() Then
        '    DialogBoxRes("resErrorNoNetwork")
        '    Exit Sub
        'End If

        'Dim oHttp As New HttpClient()
        'Dim sTmp As String
        'oHttp.Timeout = TimeSpan.FromSeconds(10)

        'Try
        '    sTmp = Await oHttp.GetStringAsync("http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000")
        'Catch ex As Exception
        '    bError = True
        'End Try
        'If bError Then
        '    DialogBoxRes("resErrorGetHttp")
        '    Exit Sub
        'End If

        '' {"stops": [
        ''{
        ''  "category": "tram",
        ''  "id": "6350927454370005230",
        ''  "latitude": 180367133,
        ''  "longitude": 72043450,
        ''  "name": "Os.Piastów",
        ''  "shortName": "378"
        ''},

        'If sTmp.IndexOf("""stops""") < 0 Then
        '    DialogBoxRes("resErrorBadTTSSstops")
        '    Exit Sub
        'End If

        'Dim oJson As JsonObject
        'Try
        '    oJson = JsonObject.Parse(sTmp)
        'Catch ex As Exception
        '    bError = True
        'End Try
        'If bError Then
        '    DialogBox("ERROR: JSON parsing error")
        '    Exit Sub
        'End If

        'Dim oJsonStops As New JsonArray
        'Try
        '    oJsonStops = oJson.GetNamedArray("stops")
        'Catch ex As Exception
        '    bError = True
        'End Try
        'If bError Then
        '    DialogBox("ERROR: JSON ""stops"" array missing")
        '    Exit Sub
        'End If

        'If oJsonStops.Count = 0 Then
        '    DialogBox("ERROR: JSON 0 obiektów")
        '    Exit Sub
        'End If

        'Dim sXml As String = ""
        'Try
        '    For Each oVal In oJsonStops
        '        sXml = sXml & vbCrLf & "<stop "
        '        sXml = sXml & "cat=""" & oVal.GetObject.GetNamedString("category") & """ "
        '        sXml = sXml & "latTtss=""" & oVal.GetObject.GetNamedNumber("latitude") & """ "
        '        sXml = sXml & "lonTtss=""" & oVal.GetObject.GetNamedNumber("longitude") & """ "
        '        sXml = sXml & "lat=""" & oVal.GetObject.GetNamedNumber("latitude") / 3600000.0 & """ "
        '        sXml = sXml & "lon=""" & oVal.GetObject.GetNamedNumber("longitude") / 3600000.0 & """ "
        '        sXml = sXml & "name=""" & oVal.GetObject.GetNamedString("name") & """ "
        '        sXml = sXml & "id=""" & oVal.GetObject.GetNamedString("shortName") & """ "
        '        sXml = sXml & " />"
        '    Next
        'Catch ex As Exception
        '    bError = True
        'End Try
        'If bError Then
        '    DialogBox("ERROR: at JSON converting")
        '    Exit Sub
        'End If

        'sXml = "<stops>" & sXml & "</stops>"

        'Try
        '    oStops.LoadXml(sXml)

        '    ' 20171108: zapis pliku tylko gdy mamy poprawny XML
        '    Dim sampleFile As StorageFile = Await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(
        '    "stops.xml", CreationCollisionOption.ReplaceExisting)
        '    Await FileIO.WriteTextAsync(sampleFile, sXml)

        '    SetSettingsInt("LastLoadStops", CInt(Date.Now.ToString("yyMMdd")))
        'Catch ex As Exception
        '    bError = True
        'End Try

        'If bError Then DialogBox("ERROR loading XmlDocument")

    End Sub
#Region "Get/Set settings"

    Public Shared Function GetSettingsString(sName As String, Optional sDefault As String = "") As String
        Dim sTmp As String

        sTmp = sDefault

        If ApplicationData.Current.RoamingSettings.Values.ContainsKey(sName) Then
            sTmp = ApplicationData.Current.RoamingSettings.Values(sName).ToString
        End If
        If ApplicationData.Current.LocalSettings.Values.ContainsKey(sName) Then
            sTmp = ApplicationData.Current.LocalSettings.Values(sName).ToString
        End If

        Return sTmp

    End Function

    Public Shared Function GetSettingsInt(sName As String, Optional iDefault As Integer = 0) As Integer
        Dim sTmp As Integer

        sTmp = iDefault

        If ApplicationData.Current.RoamingSettings.Values.ContainsKey(sName) Then
            sTmp = CInt(ApplicationData.Current.RoamingSettings.Values(sName).ToString)
        End If
        If ApplicationData.Current.LocalSettings.Values.ContainsKey(sName) Then
            sTmp = CInt(ApplicationData.Current.LocalSettings.Values(sName).ToString)
        End If

        Return sTmp

    End Function

    Public Shared Function GetSettingsBool(sName As String, Optional iDefault As Boolean = False) As Boolean
        Dim sTmp As Boolean

        sTmp = iDefault

        If ApplicationData.Current.RoamingSettings.Values.ContainsKey(sName) Then
            sTmp = CBool(ApplicationData.Current.RoamingSettings.Values(sName).ToString)
        End If
        If ApplicationData.Current.LocalSettings.Values.ContainsKey(sName) Then
            sTmp = CBool(ApplicationData.Current.LocalSettings.Values(sName).ToString)
        End If

        Return sTmp

    End Function

    Public Shared Sub SetSettingsString(sName As String, sValue As String, Optional bRoam As Boolean = False)
        If bRoam Then ApplicationData.Current.RoamingSettings.Values(sName) = sValue
        ApplicationData.Current.LocalSettings.Values(sName) = sValue
    End Sub

    Public Shared Sub SetSettingsInt(sName As String, sValue As Integer, Optional bRoam As Boolean = False)
        If bRoam Then ApplicationData.Current.RoamingSettings.Values(sName) = sValue.ToString
        ApplicationData.Current.LocalSettings.Values(sName) = sValue.ToString
    End Sub

    Public Shared Sub SetSettingsBool(sName As String, sValue As Boolean, Optional bRoam As Boolean = False)
        If bRoam Then ApplicationData.Current.RoamingSettings.Values(sName) = sValue.ToString
        ApplicationData.Current.LocalSettings.Values(sName) = sValue.ToString
    End Sub
#End Region

    Public Shared Function GPSdistanceDwa(dLat0 As Double, dLon0 As Double, dLat As Double, dLon As Double) As Integer
        ' https://stackoverflow.com/questions/28569246/how-to-get-distance-between-two-locations-in-windows-phone-8-1

        Dim iRadix As Integer = 6371000
        Dim tLat As Double = (dLat - dLat0) * Math.PI / 180
        Dim tLon As Double = (dLon - dLon0) * Math.PI / 180
        Dim a As Double = Math.Sin(tLat / 2) * Math.Sin(tLat / 2) +
            Math.Cos(Math.PI / 180 * dLat0) * Math.Cos(Math.PI / 180 * dLat) *
            Math.Sin(tLon / 2) * Math.Sin(tLon / 2)
        Dim c As Double = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)))
        Dim d As Double = iRadix * c

        Return d

    End Function

    Public Shared Function GPSdistance(oPos As Geoposition, dLat As Double, dLon As Double) As Integer

        Return App.GPSdistanceDwa(oPos.Coordinate.Point.Position.Latitude,
                oPos.Coordinate.Point.Position.Longitude, dLat, dLon)

    End Function

    Public Shared Async Function GetCurrentPoint() As Task(Of Point)
        Dim oPoint As Point

        mSpeed = App.GetSettingsInt("walkSpeed", 4)

        ' ma byc Favourite
        If App.mdLat <> 100 Then
            oPoint.X = mdLat
            oPoint.Y = mdLong
            Return oPoint
        End If

        ' na pewno ma byc wedle GPS

        oPoint.X = 50.0 '1985 ' latitude - dane domku, choc mała precyzja
        oPoint.Y = 19.9 '7872

        Dim rVal As GeolocationAccessStatus = Await Geolocator.RequestAccessAsync()
        If rVal <> GeolocationAccessStatus.Allowed Then
            If Not GetSettingsBool("noGPSshown") Then
                DialogBoxRes("resErrorNoGPSAllowed")
                SetSettingsBool("noGPSshown", True)
            End If
            Return oPoint
        End If

        Dim oDevGPS = New Geolocator()

        Dim oPos As Geoposition
        oDevGPS.DesiredAccuracyInMeters = GetSettingsInt("gpsPrec", 75) ' dla 4 km/h; 100 m = 90 sec, 75 m = 67 sec
        Dim oCacheTime = New TimeSpan(0, 0, 30)  ' minuta ≈ 80 m (ale nie autobusem! wtedy 400 m)
        Dim oTimeout = New TimeSpan(0, 0, 3)    ' timeout 
        Dim bErr = False
        Try
            oPos = Await oDevGPS.GetGeopositionAsync(oCacheTime, oTimeout)
            oPoint.X = oPos.Coordinate.Point.Position.Latitude
            oPoint.Y = oPos.Coordinate.Point.Position.Longitude

            Dim dSpeed As Double
            If oPos.Coordinate.Speed IsNot Nothing Then
                If Not System.Double.IsNaN(oPos.Coordinate.Speed) Then
                    dSpeed = oPos.Coordinate.Speed / 3.6    ' z m/s na km/h
                    mSpeed = Math.Max(dSpeed, dSpeed - 1)
                End If
            End If
        Catch ex As Exception   ' zapewne timeout
            bErr = True
        End Try
        If bErr Then App.DialogBoxRes("resErrorGettingPos")

        Return oPoint

    End Function

End Class

