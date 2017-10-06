Imports Windows.Storage
Imports System.Net.Http
Imports Windows.Data.Json
Imports Windows.Data.Xml.Dom
Imports Windows.UI.Popups
Imports Windows.Devices.Geolocation
Imports System.Net.NetworkInformation
''' <summary>
''' Provides application-specific behavior to supplement the default Application class.
''' </summary>
NotInheritable Class App
    Inherits Application

    Public Shared oStops As New XmlDocument
    Public Shared mdLat As Double = 100
    Public Shared mdLong As Double

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
        Dim oMsg As New MessageDialog(
            Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(sMsg))
        Await oMsg.ShowAsync
    End Sub

    Public Shared Async Sub CheckLoadStopList(Optional bForceLoad As Boolean = False)

        Dim bReaded As Boolean = False

        Dim oObj = ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("stops.xml")
        Dim oFile = TryCast(oObj, StorageFile)
        If Not oFile Is Nothing Then
            Dim sTxt As String = Await FileIO.ReadTextAsync(oFile)
            Try
                oStops.LoadXml(sTxt)
                bReaded = True
            Catch ex As Exception
                DialogBox("ERROR loading stops")
            End Try
        End If

        If Not bForceLoad Then
            Dim iTmp As Integer = GetSettingsInt("LastLoadStops")
            If bReaded And CInt(Date.Now.AddDays(-30).ToString("yyMMdd")) > GetSettingsInt("LastLoadStops") Then Exit Sub
        End If

        If Not NetworkInterface.GetIsNetworkAvailable() Then
            DialogBoxRes("resErrorNoNetwork")
            Exit Sub
        End If

        Dim oHttp As New HttpClient()
        Dim sTmp As String
        sTmp = Await oHttp.GetStringAsync("http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000")

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
            Exit Sub
        End If

        Dim oJson As JsonObject
        Try
            oJson = JsonObject.Parse(sTmp)
        Catch ex As Exception
            DialogBox("ERROR: JSON parsing error")
            Exit Sub
        End Try

        Dim oJsonStops As New JsonArray
        Try
            oJsonStops = oJson.GetNamedArray("stops")
        Catch ex As Exception
            DialogBox("ERROR: JSON ""stops"" array missing")
            Exit Sub
        End Try

        If oJsonStops.Count = 0 Then
            DialogBox("ERROR: JSON 0 obiektów")
            Exit Sub
        End If

        Dim sXml As String = ""
        Try
            For Each oVal In oJsonStops
                sXml = sXml & vbCrLf & "<stop "
                sXml = sXml & "cat=""" & oVal.GetObject.GetNamedString("category") & """ "
                sXml = sXml & "latTtss=""" & oVal.GetObject.GetNamedNumber("latitude") & """ "
                sXml = sXml & "lonTtss=""" & oVal.GetObject.GetNamedNumber("longitude") & """ "
                sXml = sXml & "lat=""" & oVal.GetObject.GetNamedNumber("latitude") / 3600000.0 & """ "
                sXml = sXml & "lon=""" & oVal.GetObject.GetNamedNumber("longitude") / 3600000.0 & """ "
                sXml = sXml & "name=""" & oVal.GetObject.GetNamedString("name") & """ "
                sXml = sXml & "id=""" & oVal.GetObject.GetNamedString("shortName") & """ "
                sXml = sXml & " />"
            Next
        Catch ex As Exception
            DialogBox("ERROR: at JSON converting")
            Exit Sub
        End Try

        sXml = "<stops>" & sXml & "</stops>"

        Dim sampleFile As StorageFile = Await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(
            "stops.xml", CreationCollisionOption.ReplaceExisting)
        Await FileIO.WriteTextAsync(sampleFile, sXml)
        Try
            oStops.LoadXml(sXml)
            SetSettingsInt("LastLoadStops", CInt(Date.Now.ToString("yyMMdd")))
        Catch ex As Exception
            DialogBox("ERROR loading XmlDocument")
        End Try

    End Sub

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
        oPoint.X = 50.01985 ' latitude
        oPoint.Y = 19.97872

        Dim oDevGPS As Geolocator
        oDevGPS = New Geolocator()

        Dim rVal As GeolocationAccessStatus = Await Geolocator.RequestAccessAsync()
        If rVal = GeolocationAccessStatus.Allowed Then
            Dim oPos As Geoposition
            Try
                oDevGPS.DesiredAccuracyInMeters = 75
                oPos = Await oDevGPS.GetGeopositionAsync()
                oPoint.X = oPos.Coordinate.Point.Position.Latitude
                oPoint.Y = oPos.Coordinate.Point.Position.Longitude
            Catch ex As Exception
                App.DialogBoxRes("resErrorGettingPos")
            End Try
        End If

        Return oPoint

    End Function

End Class

