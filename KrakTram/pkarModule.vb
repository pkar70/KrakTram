' (...)
'            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed
'
'            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
'            AddHandler rootFrame.Navigated, AddressOf OnNavigatedAddBackButton
'            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed
' (...)


Partial Public Class App

#Region "Back button"

    ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
    Private Sub OnNavigatedAddBackButton(sender As Object, e As NavigationEventArgs)
        Try
            Dim oFrame As Frame = TryCast(sender, Frame)
            If oFrame Is Nothing Then Exit Sub

            Dim oNavig As Windows.UI.Core.SystemNavigationManager = Windows.UI.Core.SystemNavigationManager.GetForCurrentView

            If oFrame.CanGoBack Then
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible
            Else
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed
            End If

            Return

        Catch ex As Exception
            pkar.CrashMessageExit("@OnNavigatedAddBackButton", ex.message)
        End Try

    End Sub

    Private Sub OnBackButtonPressed(sender As Object, e As Windows.UI.Core.BackRequestedEventArgs)
        Try
            TryCast(Window.Current.Content, Frame).GoBack()
            e.Handled = True
        Catch ex As Exception
        End Try
    End Sub

#End Region

End Class

Public Module pkar
#Region "CrashMessage"
    Public Async Function CrashMessageShow() As Task
        Dim sTxt As String = GetSettingsString("appFailData")
        If sTxt = "" Then Return
        Await DialogBox("Fail message:" & vbCrLf & sTxt)
        SetSettingsString("appFailData", "")
    End Function

    Public Sub CrashMessageAdd(sTxt As String, exMsg As String)
        Dim sAdd As String = Date.Now.ToString("HH:mm:ss") & " " & sTxt & vbCrLf & exMsg & vbCrLf
#If DEBUG Then
        MakeToast(sAdd)
        Debug.WriteLine(sAdd)
#Else
        If GetSettingsBool("crashShowToast") Then MakeToast(sAdd)
#End If
        SetSettingsString("appFailData", GetSettingsString("appFailData") & sAdd)
    End Sub

    Public Sub CrashMessageExit(sTxt As String, exMsg As String)
        CrashMessageAdd(sTxt, exMsg)
        TryCast(Application.Current, App).Exit()
    End Sub
#End Region

    ' -- CLIPBOARD ---------------------------------------------

#Region "ClipBoard"
    Public Sub ClipPut(sTxt As String)
        Dim oClipCont As DataTransfer.DataPackage = New DataTransfer.DataPackage
        oClipCont.RequestedOperation = DataTransfer.DataPackageOperation.Copy
        oClipCont.SetText(sTxt)
        DataTransfer.Clipboard.SetContent(oClipCont)
    End Sub

    Public Async Function ClipGet() As Task(Of String)
        Dim oClipCont As DataTransfer.DataPackageView = DataTransfer.Clipboard.GetContent
        Return Await oClipCont.GetTextAsync()
    End Function
#End Region


    ' -- Get/Set Settings ---------------------------------------------

#Region "Get/Set settings"

#Region "String"

    Public Function GetSettingsString(oTBox As TextBlock, sName As String, Optional sDefault As String = "") As String
        Dim sTmp As String = GetSettingsString(sName, sDefault)
        oTBox.Text = sTmp
        Return sTmp
    End Function

    Public Function GetSettingsString(oTBox As TextBox, sName As String, Optional sDefault As String = "") As String
        Dim sTmp As String = GetSettingsString(sName, sDefault)
        oTBox.Text = sTmp
        Return sTmp
    End Function


    Public Function GetSettingsString(sName As String, Optional sDefault As String = "") As String
        Dim sTmp As String

        sTmp = sDefault

        With Windows.Storage.ApplicationData.Current
            If .RoamingSettings.Values.ContainsKey(sName) Then
                sTmp = .RoamingSettings.Values(sName).ToString
            End If
            If .LocalSettings.Values.ContainsKey(sName) Then
                sTmp = .LocalSettings.Values(sName).ToString
            End If
        End With

        Return sTmp

    End Function

    Public Sub SetSettingsString(sName As String, sValue As String)
        SetSettingsString(sName, sValue, False)
    End Sub

    Public Sub SetSettingsString(sName As String, sValue As String, bRoam As Boolean)
        With Windows.Storage.ApplicationData.Current
            If bRoam Then .RoamingSettings.Values(sName) = sValue
            .LocalSettings.Values(sName) = sValue
        End With
    End Sub


    Public Sub SetSettingsString(sName As String, sValue As TextBox, bRoam As Boolean)
        SetSettingsString(sName, sValue.Text, bRoam)
    End Sub

    Public Sub SetSettingsString(sName As String, sValue As TextBox)
        SetSettingsString(sName, sValue.Text, False)
    End Sub


#End Region
#Region "Int"
    Public Function GetSettingsInt(sName As String, Optional iDefault As Integer = 0) As Integer
        Dim sTmp As Integer

        sTmp = iDefault

        With Windows.Storage.ApplicationData.Current
            If .RoamingSettings.Values.ContainsKey(sName) Then
                sTmp = CInt(.RoamingSettings.Values(sName).ToString)
            End If
            If .LocalSettings.Values.ContainsKey(sName) Then
                sTmp = CInt(.LocalSettings.Values(sName).ToString)
            End If
        End With

        Return sTmp

    End Function

    Public Sub SetSettingsInt(sName As String, sValue As Integer)
        SetSettingsInt(sName, sValue, False)
    End Sub

    Public Sub SetSettingsInt(sName As String, sValue As Integer, bRoam As Boolean)
        With Windows.Storage.ApplicationData.Current
            If bRoam Then .RoamingSettings.Values(sName) = sValue.ToString
            .LocalSettings.Values(sName) = sValue.ToString
        End With
    End Sub
#End Region
#Region "Bool"
    Public Function GetSettingsBool(sName As String, Optional iDefault As Boolean = False) As Boolean
        Dim sTmp As Boolean

        sTmp = iDefault
        With Windows.Storage.ApplicationData.Current
            If .RoamingSettings.Values.ContainsKey(sName) Then
                sTmp = CBool(.RoamingSettings.Values(sName).ToString)
            End If
            If .LocalSettings.Values.ContainsKey(sName) Then
                sTmp = CBool(.LocalSettings.Values(sName).ToString)
            End If
        End With

        Return sTmp

    End Function

    Public Function GetSettingsBool(oSwitch As ToggleSwitch, sName As String, Optional iDefault As Boolean = False) As Boolean
        Dim sTmp As Boolean
        sTmp = GetSettingsBool(sName, iDefault)
	oSwitch.IsOn = sTmp
        Return sTmp

    End Function

    Public Sub SetSettingsBool(sName As String, sValue As Boolean)
        SetSettingsBool(sName, sValue, False)
    End Sub

    Public Sub SetSettingsBool(sName As String, sValue As Boolean, bRoam As Boolean)
        With Windows.Storage.ApplicationData.Current
            If bRoam Then .RoamingSettings.Values(sName) = sValue.ToString
            .LocalSettings.Values(sName) = sValue.ToString
        End With
    End Sub

    Public Sub SetSettingsBool(sValue As ToggleSwitch, sName As String, Optional bRoam As Boolean = False)
        SetSettingsBool(sName, sValue.IsOn, bRoam)
    End Sub

    Public Sub SetSettingsBool(sName As String, sValue As ToggleSwitch, bRoam As Boolean)
	SetSettingsBool(sName, sValue.IsOn, bRoam)
    End Sub

    Public Sub SetSettingsBool(sName As String, sValue As ToggleSwitch)
	SetSettingsBool(sName, sValue.IsOn, False)
    End Sub

#End Region

#End Region



    ' -- Testy sieciowe ---------------------------------------------

#Region "testy sieciowe"

    Public Function NetIsMobile() As Boolean
        Return (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily = "Windows.Mobile")
    End Function

    Public Function NetIsIPavailable(bMsg As Boolean) As Boolean
        If GetSettingsBool("offline") Then Return False

        If Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then Return True
        If bMsg Then
#Disable Warning BC42358
            DialogBox("ERROR: no IP network available")
#Enable Warning BC42358
        End If
        Return False
    End Function

    Public Function NetIsCellInet() As Boolean
        Return Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWwanConnectionProfile
    End Function


    Public Function GetHostName() As String
        Dim hostNames As IReadOnlyList(Of Windows.Networking.HostName) =
                Windows.Networking.Connectivity.NetworkInformation.GetHostNames()
        For Each oItem As Windows.Networking.HostName In hostNames
            If oItem.DisplayName.Contains(".local") Then
                Return oItem.DisplayName.Replace(".local", "")
            End If
        Next
        Return ""
    End Function


    Public Function IsThisMoje() As Boolean
        Dim sTmp As String = GetHostName.ToLower
        If sTmp = "home-pkar" Then Return True
        If sTmp = "lumia_pkar" Then Return True
        If sTmp = "kuchnia_pk" Then Return True
        If sTmp = "ppok_pk" Then Return True
        'If sTmp.Contains("pkar") Then Return True
        'If sTmp.EndsWith("_pk") Then Return True
        Return False
    End Function

    Public Async Function NetWiFiOffOn() As Task(Of Boolean)

        ' https://social.msdn.microsoft.com/Forums/ie/en-US/60c4a813-dc66-4af5-bf43-e632c5f85593/uwpbluetoothhow-to-turn-onoff-wifi-bluetooth-programmatically?forum=wpdevelop
        Dim result222 = Await Windows.Devices.Radios.Radio.RequestAccessAsync()
        Dim radios As IReadOnlyList(Of Windows.Devices.Radios.Radio) = Await Windows.Devices.Radios.Radio.GetRadiosAsync()

        For Each oRadio In radios
            If oRadio.Kind = Windows.Devices.Radios.RadioKind.WiFi Then
                Dim oStat As Windows.Devices.Radios.RadioAccessStatus =
                    Await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.Off)
                If oStat <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False
                Await Task.Delay(3 * 1000)
                oStat = Await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.On)
                If oStat <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False
            End If
        Next

        Return True
    End Function

#End Region


    ' -- DialogBoxy ---------------------------------------------

#Region "DialogBoxy"


    Public Async Function DialogBox(sMsg As String) As Task
        Dim oMsg As Windows.UI.Popups.MessageDialog = New Windows.UI.Popups.MessageDialog(sMsg)
        Await oMsg.ShowAsync
    End Function

    Public Function GetLangString(sMsg As String) As String
        If sMsg = "" Then Return ""

        Dim sRet As String = sMsg
        Try
            sRet = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(sMsg)
        Catch
        End Try
        Return sRet
    End Function

    Public Async Function DialogBoxRes(sMsg As String) As Task
        sMsg = GetLangString(sMsg)
        Await DialogBox(sMsg)
    End Function


    Public Async Sub DialogBoxError(iNr As Integer, sMsg As String)
        Dim sTxt As String = GetLangString("errAnyError")
        sTxt = sTxt & " (" & iNr & ")" & vbCrLf & sMsg
        Await DialogBox(sMsg)
    End Sub

    Public Async Function DialogBoxYN(sMsg As String, Optional sYes As String = "Tak", Optional sNo As String = "Nie") As Task(Of Boolean)
        Dim oMsg As Windows.UI.Popups.MessageDialog = New Windows.UI.Popups.MessageDialog(sMsg)
        Dim oYes As Windows.UI.Popups.UICommand = New Windows.UI.Popups.UICommand(sYes)
        Dim oNo As Windows.UI.Popups.UICommand = New Windows.UI.Popups.UICommand(sNo)
        oMsg.Commands.Add(oYes)
        oMsg.Commands.Add(oNo)
        oMsg.DefaultCommandIndex = 1    ' default: No
        oMsg.CancelCommandIndex = 1
        Dim oCmd As Windows.UI.Popups.IUICommand = Await oMsg.ShowAsync
        If oCmd Is Nothing Then Return False
        If oCmd.Label = sYes Then Return True

        Return False
    End Function

    Public Async Function DialogBoxResYN(sMsgResId As String, Optional sYesResId As String = "resDlgYes", Optional sNoResId As String = "resDlgNo") As Task(Of Boolean)
        Dim sMsg, sYes, sNo As String

        With Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView()
            sMsg = .GetString(sMsgResId)
            sYes = .GetString(sYesResId)
            sNo = .GetString(sNoResId)
        End With

        If sMsg = "" Then sMsg = sMsgResId  ' zabezpieczenie na brak string w resource
        If sYes = "" Then sYes = sYesResId
        If sNo = "" Then sNo = sNoResId

        Return Await DialogBoxYN(sMsg, sYes, sNo)
    End Function


    Public Async Function DialogBoxInput(sMsgResId As String, Optional sDefaultResId As String = "", Optional sYesResId As String = "resDlgContinue", Optional sNoResId As String = "resDlgCancel") As Task(Of String)
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



#End Region



    ' --- INNE FUNKCJE ------------------------

    Public Sub SetBadgeNo(iInt As Integer)
        ' https://docs.microsoft.com/en-us/windows/uwp/controls-and-patterns/tiles-and-notifications-badges

        Dim oXmlBadge As Windows.Data.Xml.Dom.XmlDocument
        oXmlBadge = Windows.UI.Notifications.BadgeUpdateManager.GetTemplateContent(
                Windows.UI.Notifications.BadgeTemplateType.BadgeNumber)

        Dim oXmlNum As Windows.Data.Xml.Dom.XmlElement
        oXmlNum = CType(oXmlBadge.SelectSingleNode("/badge"), Windows.Data.Xml.Dom.XmlElement)
        oXmlNum.SetAttribute("value", iInt.ToString)

        Windows.UI.Notifications.BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(
                New Windows.UI.Notifications.BadgeNotification(oXmlBadge))
    End Sub


    Public Function XmlSafeString(sInput As String) As String
        Dim sTmp As String
        sTmp = sInput.Replace("&", "&amp;")
        sTmp = sTmp.Replace("<", "&lt;")
        sTmp = sTmp.Replace(">", "&gt;")
        Return sTmp
    End Function

    Public Function XmlSafeStringQt(sInput As String) As String
        Dim sTmp As String
        sTmp = XmlSafeString(sInput)
        sTmp = sTmp.Replace("""", "&quote;")
        Return sTmp
    End Function

    Public Function ToastAction(sAType As String, sAct As String, sGuid As String, sContent As String) As String
        Dim sTmp As String = sContent
        If sTmp <> "" Then sTmp = GetSettingsString(sTmp, sTmp)

        Dim sTxt As String = "<action " &
            "activationType=""" & sAType & """ " &
            "arguments=""" & sAct & sGuid & """ " &
            "content=""" & sTmp & """/> "
        Return sTxt
    End Function

    Public Sub MakeToast(sMsg As String, Optional sMsg1 As String = "")
        Dim sXml = "<visual><binding template='ToastGeneric'><text>" & XmlSafeString(sMsg)
        If sMsg1 <> "" Then sXml = sXml & "</text><text>" & XmlSafeString(sMsg1)
        sXml = sXml & "</text></binding></visual>"
        Dim oXml = New Windows.Data.Xml.Dom.XmlDocument
        oXml.LoadXml("<toast>" & sXml & "</toast>")
        Dim oToast = New Windows.UI.Notifications.ToastNotification(oXml)
        Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(oToast)
    End Sub

    Public Function WinVer() As Integer
        'Unknown = 0,
        'Threshold1 = 1507,   // 10240
        'Threshold2 = 1511,   // 10586
        'Anniversary = 1607,  // 14393 Redstone 1
        'Creators = 1703,     // 15063 Redstone 2
        'FallCreators = 1709 // 16299 Redstone 3
        'April = 1803		// 17134
        'October = 1809		// 17763
        '? = 190?		// 18???

        'April  1803, 17134, RS5

        Dim u As ULong = ULong.Parse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion)
        u = (u And &HFFFF0000L) >> 16
        Return u
        'For i As Integer = 5 To 1 Step -1
        '    If Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", i) Then Return i
        'Next

        'Return 0
    End Function


    Private moHttp As Windows.Web.Http.HttpClient = New Windows.Web.Http.HttpClient

    Public Async Function HttpPageAsync(sUrl As String, sErrMsg As String, Optional sData As String = "") As Task(Of String)
        Try
            If Not NetIsIPavailable(True) Then Return ""
            If sUrl = "" Then Return ""

            If sUrl.Substring(0, 4) <> "http" Then sUrl = "http://beskid.geo.uj.edu.pl/p/dysk" & sUrl

            If moHttp Is Nothing Then
                moHttp = New Windows.Web.Http.HttpClient
                moHttp.DefaultRequestHeaders.UserAgent.TryParseAdd("GrajCyganie")
            End If

            Dim sError = ""
            Dim oResp As Windows.Web.Http.HttpResponseMessage = Nothing

            Try
                If sData <> "" Then
                    Dim oHttpCont = New Windows.Web.Http.HttpStringContent(sData, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded")
                    oResp = Await moHttp.PostAsync(New Uri(sUrl), oHttpCont)
                Else
                    oResp = Await moHttp.GetAsync(New Uri(sUrl))
                End If
            Catch ex As Exception
                sError = ex.Message
            End Try

            If sError <> "" Then
                Await DialogBox("error " & sError & " at " & sErrMsg & " page")
                Return ""
            End If

            If oResp.StatusCode = 303 Or oResp.StatusCode = 302 Or oResp.StatusCode = 301 Then
                ' redirect
                sUrl = oResp.Headers.Location.ToString
                'If sUrl.ToLower.Substring(0, 4) <> "http" Then
                '    sUrl = "https://sympatia.onet.pl/" & sUrl   ' potrzebne przy szukaniu
                'End If

                If sData <> "" Then
                    ' Dim oHttpCont = New HttpStringContent(sData, Text.Encoding.UTF8, "application/x-www-form-urlencoded")
                    Dim oHttpCont = New Windows.Web.Http.HttpStringContent(sData, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded")
                    oResp = Await moHttp.PostAsync(New Uri(sUrl), oHttpCont)
                Else
                    oResp = Await moHttp.GetAsync(New Uri(sUrl))
                End If
            End If

            If oResp.StatusCode > 290 Then
                Await DialogBox("ERROR " & oResp.StatusCode & " getting " & sErrMsg & " page")
                Return ""
            End If

            Dim sResp As String = ""
            Try
                sResp = Await oResp.Content.ReadAsStringAsync
            Catch ex As Exception
                sError = ex.Message
            End Try

            If sError <> "" Then
                Await DialogBox("error " & sError & " at ReadAsStringAsync " & sErrMsg & " page")
                Return ""
            End If

            Return sResp

        Catch ex As Exception
            CrashMessageExit("@HttpPageAsync", ex.message)
        End Try

        Return ""
    End Function

    Public Function RemoveHtmlTags(sHtml As String) As String
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


    Public Sub OpenBrowser(oUri As Uri, bForceEdge As Boolean)
        If bForceEdge Then
            Dim options As Windows.System.LauncherOptions = New Windows.System.LauncherOptions()
            options.TargetApplicationPackageFamilyName = "Microsoft.MicrosoftEdge_8wekyb3d8bbwe"
#Disable Warning BC42358
            Windows.System.Launcher.LaunchUriAsync(oUri, options)
        Else
            Windows.System.Launcher.LaunchUriAsync(oUri)
#Enable Warning BC42358
        End If

    End Sub

    Public Sub OpenBrowser(sUri As String, bForceEdge As Boolean)
        Dim oUri As Uri = New Uri(sUri)
        OpenBrowser(oUri, bForceEdge)
    End Sub


    Public Function FileLen2string(iBytes As Long) As String
        If iBytes = 1 Then Return "1 byte"
        If iBytes < 10000 Then Return iBytes & " bytes"
        iBytes = iBytes \ 1024
        If iBytes = 1 Then Return "1 kibibyte"
        If iBytes < 2000 Then Return iBytes & " kibibytes"
        iBytes = iBytes \ 1024
        If iBytes = 1 Then Return "1 mebibyte"
        If iBytes < 2000 Then Return iBytes & " mebibytes"
        iBytes = iBytes \ 1024
        If iBytes = 1 Then Return "1 gibibyte"
        Return iBytes & " gibibytes"
    End Function


    Public Function UnixTimeToTime(lTime As Long) As DateTime
        '1509993360
        Dim dtDateTime As DateTime = New DateTime(1970, 1, 1, 0, 0, 0, 0)
        dtDateTime = dtDateTime.AddSeconds(lTime)   ' UTC
        ' dtDateTime.Kind = DateTimeKind.Utc
        Return dtDateTime.ToLocalTime
    End Function


End Module
