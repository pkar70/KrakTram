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
    Public Shared oFavour As New FavStopList
    Public Shared mdLat As Double = 100
    Public Shared mdLong, mSpeed As Double
    Public Shared mbGoGPS As Boolean = False
    Public Shared mMaxOdl As Double = 20
    Public Shared msCat As String = "tram"
    Public Shared moOdjazdy As ListaOdjazdow = New ListaOdjazdow

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

            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
            AddHandler rootFrame.Navigated, AddressOf OnNavigatedAddBackButton
            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed

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

#Region "BackButton"
    ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
    Private Sub OnNavigatedAddBackButton(sender As Object, e As NavigationEventArgs)
#If CONFIG = "Debug" Then
        ' próba wylapywania errorów gdy nic innego tego nie złapie
        Dim sDebugCatch As String = ""
        Try
#End If
            Dim oFrame As Frame = TryCast(sender, Frame)
            If oFrame Is Nothing Then Exit Sub

            Dim oNavig As Windows.UI.Core.SystemNavigationManager = Windows.UI.Core.SystemNavigationManager.GetForCurrentView

            If oFrame.CanGoBack Then
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible
            Else
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed
            End If


#If CONFIG = "Debug" Then
        Catch ex As Exception
            sDebugCatch = ex.Message
        End Try

        If sDebugCatch <> "" Then
#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
            App.DialogBox("DebugCatch in OnNavigatedAddBackButton:" & vbCrLf & sDebugCatch)
#Enable Warning BC42358
        End If
#End If

    End Sub

    Private Sub OnBackButtonPressed(sender As Object, e As Windows.UI.Core.BackRequestedEventArgs)
        Try
            TryCast(Window.Current.Content, Frame).GoBack()
            e.Handled = True
        Catch ex As Exception
        End Try
    End Sub
#End Region



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

    Public Shared Function GetLangString(sMsg As String) As String
        If sMsg = "" Then Return ""

        Dim sRet As String = sMsg
        Try
            sRet = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(sMsg)
        Catch
        End Try
        Return sRet
    End Function


    Public Shared Async Sub DialogBox(sMsg As String)
        Dim oMsg As New MessageDialog(sMsg)
        Await oMsg.ShowAsync
    End Sub
    Public Shared Async Function DialogBoxRes(sMsg As String) As Task
        ' 20180523, reakcja na niemanie w resources - bo chyba na tym wyleciała app wedle Dashboard (DialogBoxRes.MoveNext)
        Dim sTxt As String
        Try
            sTxt = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(sMsg)
        Catch ex As Exception
            sTxt = "Some error occured"
        End Try
        Dim oMsg As New MessageDialog(sTxt)
        Await oMsg.ShowAsync
    End Function

    Public Shared Async Function DialogBoxResWait(sMsg As String) As Task
        ' 20180523, reakcja na niemanie w resources - bo chyba na tym wyleciała app wedle Dashboard (DialogBoxRes.MoveNext)
        Dim sTxt As String
        Try
            sTxt = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(sMsg)
        Catch ex As Exception
            sTxt = "Some error occured"
        End Try
        Dim oMsg As New MessageDialog(sTxt)
        Await oMsg.ShowAsync
    End Function

    'Public Shared Async Sub DialogBoxResYN(sMsg As String) As Task(Of Integer)
    '    With Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView()
    '        Dim oMsg As New MessageDialog(.GetString(sMsg))
    '        oMsg.Commands.Add(New UICommand(.GetString("resYes")))
    '        oMsg.Commands.Add(New UICommand(.GetString("resNo")))
    '        Await oMsg.ShowAsync
    '    End With
    'End Sub


    Public Shared Async Function DialogBoxInput(sMsgResId As String, Optional sDefaultResId As String = "", Optional sYesResId As String = "resDlgContinue", Optional sNoResId As String = "resDlgCancel") As Task(Of String)
        Dim sMsg, sYes, sNo, sDefault As String

        sDefault = ""

        With Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView()
            sMsg = .GetString(sMsgResId)
            sYes = .GetString(sYesResId)
            sNo = .GetString(sNoResId)
            If sDefaultResId <> "" Then sDefault = .GetString(sDefaultResId)
        End With

        If sMsg = "" Then sMsg = sMsgResId  ' zabezpieczenie na brak string w resource
        If sYes = "" Then sYes = sYesResId
        If sNo = "" Then sNo = sNoResId
        If sDefault = "" Then sDefault = sDefaultResId

        Dim oInputTextBox = New TextBox
        oInputTextBox.AcceptsReturn = False
        oInputTextBox.Text = sDefault
        Dim oDlg As New ContentDialog
        oDlg.Content = oInputTextBox
        oDlg.PrimaryButtonText = sYes
        oDlg.SecondaryButtonText = sNo
        oDlg.Title = sMsg

        Dim oCmd = Await oDlg.ShowAsync
        If oCmd <> ContentDialogResult.Primary Then Return ""

        Return oInputTextBox.Text

    End Function

    Public Shared Async Function CheckLoadStopList(Optional bForceLoad As Boolean = False) As Task
        Await oStops.LoadOrImport(bForceLoad)
    End Function

    Public Shared Async Function LoadFavList() As Task
        Await oFavour.LoadOrImport
    End Function
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

        Try
            Dim iRadix As Integer = 6371000
            Dim tLat As Double = (dLat - dLat0) * Math.PI / 180
            Dim tLon As Double = (dLon - dLon0) * Math.PI / 180
            Dim a As Double = Math.Sin(tLat / 2) * Math.Sin(tLat / 2) +
            Math.Cos(Math.PI / 180 * dLat0) * Math.Cos(Math.PI / 180 * dLat) *
            Math.Sin(tLon / 2) * Math.Sin(tLon / 2)
            Dim c As Double = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)))
            Dim d As Double = iRadix * c

            Return d

        Catch ex As Exception
            Return 0    ' nie powinno sie nigdy zdarzyc, ale na wszelki wypadek...
        End Try

    End Function

    Public Shared Function GPSdistance(oPos As Geoposition, dLat As Double, dLon As Double) As Integer

        Return App.GPSdistanceDwa(oPos.Coordinate.Point.Position.Latitude,
                oPos.Coordinate.Point.Position.Longitude, dLat, dLon)

    End Function

    Public Shared Async Function GetCurrentPoint() As Task(Of Point)
        Dim oPoint As Point

        mSpeed = App.GetSettingsInt("walkSpeed", 4)

        ' 20190221: usuwam, bo było że po wybraniu Fav potem nie działa wedle GPS?
        ' poza tym wywołanie w Odjazdy jest tylko wtedy, gdy wedle GPS,
        ' i zostaje odwołanie z Setup

        '' ma byc Favourite
        'If App.mdLat <> 100 Then
        '    oPoint.X = mdLat
        '    oPoint.Y = mdLong
        '    Return oPoint
        'End If

        ' na pewno ma byc wedle GPS

        oPoint.X = 50.0 '1985 ' latitude - dane domku, choc mała precyzja
        oPoint.Y = 19.9 '7872

        Dim rVal As GeolocationAccessStatus = Await Geolocator.RequestAccessAsync()
        If rVal <> GeolocationAccessStatus.Allowed Then
            'If Not GetSettingsBool("noGPSshown") Then
            Await DialogBoxRes("resErrorNoGPSAllowed")
            '    SetSettingsBool("noGPSshown", True)
            'End If
            Return oPoint
        End If

        Dim oDevGPS As Geolocator = New Geolocator()

        Dim oPos As Geoposition
        oDevGPS.DesiredAccuracyInMeters = GetSettingsInt("gpsPrec", 75) ' dla 4 km/h; 100 m = 90 sec, 75 m = 67 sec
        Dim oCacheTime As TimeSpan = New TimeSpan(0, 0, 30)  ' minuta ≈ 80 m (ale nie autobusem! wtedy 400 m)
        Dim oTimeout As TimeSpan = New TimeSpan(0, 0, 5)    ' timeout 
        Dim bErr As Boolean = False
        Try
            oPos = Await oDevGPS.GetGeopositionAsync(oCacheTime, oTimeout)
            oPoint.X = oPos.Coordinate.Point.Position.Latitude
            oPoint.Y = oPos.Coordinate.Point.Position.Longitude

            Dim dSpeed As Double
            ' 2018.11.13: dodaję: andalso hasvalue
            If oPos.Coordinate.Speed IsNot Nothing AndAlso oPos.Coordinate.Speed.HasValue Then
                If Not System.Double.IsNaN(oPos.Coordinate.Speed) Then
                    If oPos.Coordinate.Speed <> 0 Then
                        dSpeed = oPos.Coordinate.Speed / 3.6    ' z m/s na km/h
                        mSpeed = Math.Max(dSpeed, dSpeed - 1)   ' co ja tu miałem na myśli??
                        mSpeed = Math.Max(dSpeed, 1)            ' nie wiem, więc daję to (na wszelki wypadek - ograniczenie na 1 km/h)
                    End If
                End If
            End If
        Catch ex As Exception   ' zapewne timeout
            bErr = True
        End Try
        If bErr Then
            ' po tym wyskakuje później z błędem, więc może oPoint jest zepsute?
            ' dodaję zarówno ustalenie oPoint i mSpeed na defaulty, jak i Speed.HasValue
            Await App.DialogBoxRes("resErrorGettingPos")

            oPoint.X = 50.0 '1985 ' latitude - dane domku, choc mała precyzja
            oPoint.Y = 19.9 '7872
            mSpeed = App.GetSettingsInt("walkSpeed", 4)
        End If

        Return oPoint

    End Function

#Region "testy sieciowe"

    Public Shared Function IsMobile() As Boolean
        Return (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily = "Windows.Mobile")
    End Function

    Public Shared Function IsNetIPavailable(bMsg As Boolean) As Boolean
        If App.GetSettingsBool("offline") Then Return False

        If Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then Return True
        If bMsg Then
#Disable Warning BC42358
            DialogBox("ERROR: no IP network available")
#Enable Warning BC42358
        End If
        Return False
    End Function

    Public Shared Function IsCellInet() As Boolean
        Return Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWwanConnectionProfile
    End Function
#End Region
    Public Shared Function RemoveHtmlTags(sHtml As String) As String
        Dim iInd0, iInd1 As Integer

        iInd0 = sHtml.IndexOf("<script")
        If iInd0 > 0 Then
            iInd1 = sHtml.IndexOf("</script>", iInd0)
            If iInd1 > 0 Then
                sHtml = sHtml.Remove(iInd0, iInd1 - iInd0 + 9)
            End If
        End If

        iInd0 = sHtml.IndexOf("<")
        iInd1 = sHtml.IndexOf(">")
        While iInd0 > -1
            If iInd1 > -1 Then
                sHtml = sHtml.Remove(iInd0, iInd1 - iInd0 + 1)
            Else
                sHtml = sHtml.Substring(0, iInd0)
            End If
            sHtml = sHtml.Trim

            iInd0 = sHtml.IndexOf("<")
            iInd1 = sHtml.IndexOf(">")
        End While

        sHtml = sHtml.Replace("&nbsp;", " ")
        sHtml = sHtml.Replace(vbLf, vbCrLf)
        sHtml = sHtml.Replace(vbCrLf & vbCrLf, vbCrLf)
        sHtml = sHtml.Replace(vbCrLf & vbCrLf, vbCrLf)
        sHtml = sHtml.Replace(vbCrLf & vbCrLf, vbCrLf)

        Return sHtml.Trim

    End Function


End Class


