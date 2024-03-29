﻿' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

Imports Windows.ApplicationModel.Resources
Imports Windows.Data.Xml.Dom
Imports Windows.Data.Xml.Xsl
Imports Windows.Devices.Geolocation
Imports Windows.UI.Popups
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class Setup
    Inherits Page

    Shared oXmlPlaces As New XmlDocument
    Private moTask As Task(Of Point) = Nothing
    Private msRunType As String

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        msRunType = e.Parameter.ToString
        ' MAIN, ODJAZD
    End Sub

    Private Sub bOk_Click(sender As Object, e As RoutedEventArgs)
        SetSettingsInt("maxOdl", uiMaxOdlSld.Value)
        SetSettingsInt("walkSpeed", uiWalkSpeedSld.Value)
        SetSettingsInt("alsoNext", uiAlsoNextSld.Value)
        SetSettingsInt("gpsPrec", uiGPSPrecSld.Value)
        SetSettingsString("favPlaces", oXmlPlaces.GetXml)

        If uiAlsoBus.IsOn AndAlso Not GetSettingsBool("settingsAlsoBus") Then
            ' koniecznosc przeladowania przystankow - mozliwe ze :)
        End If
        SetSettingsBool("settingsAlsoBus", uiAlsoBus.IsOn)

        Me.Frame.GoBack()
    End Sub

    Private Async Sub bLoadStops_Click(sender As Object, e As RoutedEventArgs)
        ' na reload stops? bo reszta funkcjonalnosci jest w mainpage
        'Me.Frame.Navigate(GetType(ListaPrzystankow))
        uiReloadStop.IsEnabled = False
        Await App.CheckLoadStopList(True)
        uiReloadStop.IsEnabled = True
    End Sub

    Private Sub Setup_Loaded(sender As Object, e As RoutedEventArgs)
        uiMaxOdlSld.Value = GetSettingsInt("maxOdl", 1000)
        uiWalkSpeedSld.Value = GetSettingsInt("walkSpeed", 4)
        uiAlsoNextSld.Value = GetSettingsInt("alsoNext", 5)
        uiGPSPrecSld.Value = GetSettingsInt("gpsPrec", 75)

        uiPositionLat.Visibility = Visibility.Collapsed
        uiPositionLong.Visibility = Visibility.Collapsed
        uiPositionName.Visibility = Visibility.Collapsed
        uiPositionButt.Visibility = Visibility.Collapsed

        uiPositionLat.Text = App.mdLat
        uiPositionLong.Text = App.mdLong
        uiAlsoBus.IsOn = GetSettingsBool("settingsAlsoBus")
        'Setup_SizeChanged(sender, Nothing) ' wielkosc WebView

        'ReloadFavPlaces()
        'uiPosition.SelectedIndex = 0
    End Sub



    Private Async Sub eMaxOdl_Changed(sender As Object, e As RangeBaseValueChangedEventArgs) Handles uiMaxOdlSld.ValueChanged

        If uiMaxOdlTxt Is Nothing Then Exit Sub
        uiMaxOdlTxt.Text = uiMaxOdlSld.Value & " m"

        ' oPoint - albo narzucony, albo z GPS
        Dim oPoint As Point
        If App.mdLat = 100 Then
            oPoint = Await App.GetCurrentPoint
        Else
            oPoint.X = App.mdLat
            oPoint.Y = App.mdLong
        End If

        'Dim sHtml As string = "<html><body>"
        'Dim iOdl As Integer
        'For Each oNode In App.oStops.SelectNodes("//stop")
        '    iOdl = App.GPSdistanceDwa(oPoint.X, oPoint.Y, oNode.SelectSingleNode("@lat").NodeValue, oNode.SelectSingleNode("@lon").NodeValue)
        '    If iOdl < uiMaxOdlSld.Value Then
        '        sHtml = sHtml & "<li><b>" & oNode.SelectSingleNode("@name").NodeValue & "</b>, " & iOdl & " metrów"
        '    End If
        'Next
        'sHtml = sHtml & "</body></html>"

        Dim sXml As String = "<root>"
        Dim iOdl As Integer
        Dim iCnt As Integer = 0
        Dim iMinOdl As Integer = 100000
        Dim sTmp As String

        For Each oNode As Przystanek In App.oStops.GetList("all")
            iOdl = App.GPSdistanceDwa(oPoint.X, oPoint.Y, oNode.Lat, oNode.Lon)
            If iOdl < uiMaxOdlSld.Value Then
                sXml = sXml & "<item name='" & oNode.Name
                If GetSettingsBool("settingsAlsoBus") Then
                    If oNode.Cat = "bus" Then
                        sXml = sXml & " (A)"
                    Else
                        sXml = sXml & " (T)"
                    End If
                End If

                sXml = sXml & "' odl='" & iOdl & "' "
                iOdl = 60 * iOdl / (uiWalkSpeedSld.Value * 1000)
                sXml = sXml & "odlMinut='" & iOdl & "' />"
                iCnt = iCnt + 1
            End If
            iMinOdl = Math.Min(iMinOdl, iOdl)
        Next

        sXml = sXml & "</root>"

        If iCnt > 0 Then


            ' tester: http://xslttest.appspot.com/
            Dim sXslt As String = "
<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0'>
    <xsl:template match='/'>
        <html>
            <body>
                <ul>
                    <xsl:apply-templates select='root/item'>
                        <xsl:sort data-type='number' select='@odl' />
                    </xsl:apply-templates>
                </ul>
            </body>
        </html>
    </xsl:template>

  <xsl:template match='root/item'>
    <li><b><xsl:value-of select='@name'/></b>, <xsl:value-of select='@odl'/> m (<xsl:value-of select='@odlMinut' /> min)</li>
  </xsl:template>
</xsl:stylesheet>
"

            Dim oXsltDoc As XmlDocument = New XmlDocument
            oXsltDoc.LoadXml(sXslt)
            Dim oXP As XsltProcessor = New XsltProcessor(oXsltDoc)
            Dim oXmlDoc As XmlDocument = New XmlDocument
            oXmlDoc.LoadXml(sXml)
            sTmp = oXP.TransformToString(oXmlDoc.DocumentElement)
        Else
            sTmp = ResourceLoader.GetForCurrentView().GetString("resNearestStop")
            If iMinOdl < 10000 Then
                sTmp = sTmp.Replace("###", iMinOdl.ToString & " m")
            Else
                sTmp = sTmp.Replace("###", (iMinOdl \ 1000) & " km")
            End If
            sTmp = "<html><body><b>" & sTmp & "</b></body></html>"
        End If

        uiSetupWebView.NavigateToString(sTmp)

    End Sub

    Private Sub uiWalk_Changed(sender As Object, e As RangeBaseValueChangedEventArgs) Handles uiWalkSpeedSld.ValueChanged
        If uiWalkSpeedTxt Is Nothing Then Exit Sub
        uiWalkSpeedTxt.Text = uiWalkSpeedSld.Value & " km/h"
    End Sub

    Private Sub uiNext_Changed(sender As Object, e As RangeBaseValueChangedEventArgs) Handles uiAlsoNextSld.ValueChanged
        If uiAlsoNextTxt Is Nothing Then Exit Sub
        uiAlsoNextTxt.Text = uiAlsoNextSld.Value & " min"
    End Sub



    Private Async Sub uiPositOk_Click(sender As Object, e As RoutedEventArgs) Handles uiPositionButt.Click
        ' dodawanie nowego entry

        Dim sTxt As String = uiPositionName.Text
        'sTxt = sTxt.Replace("*", "")
        'sTxt = sTxt.Replace("[", "")
        'sTxt = sTxt.Replace("]", "")

        If sTxt.Length < 4 Then
            Await DialogBoxRes("resErrorNazwaZaKrotka")
            Exit Sub
        End If

        Dim dLat, dLon As Double
        If Not Double.TryParse(uiPositionLong.Text, dLon) OrElse
                Not Double.TryParse(uiPositionLat.Text, dLat) Then
            Await DialogBoxRes("resBadFloat")
            Exit Sub
        End If

        If sTxt = "pkarinit" Then
            App.oFavour.InitPkar()
        Else
            If dLon < 19 Or dLon > 21 Or
            dLat < 49 Or dLat > 51 Then
                Await DialogBoxRes("resErrorPozaKrakowem")
                Exit Sub
            End If

            App.oFavour.Add(sTxt, dLat, dLon, uiMaxOdlSld.Value)
            App.mdLat = dLat    ' i ustalamy to jako biezace wspolrzedne
            App.mdLong = dLon
        End If

        uiPositionName.Visibility = Visibility.Collapsed
        'uiPositionFavButt.Visibility = Visibility.Collapsed
        ' 20171019 - usuwamy wiecej, i jeszcze zmieniamy WebView wielkosc
        uiPositionLat.Visibility = Visibility.Collapsed
        uiPositionLong.Visibility = Visibility.Collapsed
        uiPositionButt.Visibility = Visibility.Collapsed
        uiOpenPosPanel.Visibility = Visibility.Visible

        'Setup_SizeChanged(sender, Nothing) ' wielkosc WebView

        'ReloadFavPlaces()
        'uiPosition.SelectedIndex = uiPosition.Items.Count - 1 ' na ostatni (bo bez sortowania)

    End Sub
#Region "Stary model"

    'Private Async Sub uiPosition_Changed(sender As Object, e As SelectionChangedEventArgs) Handles uiPosition.SelectionChanged

    '    If uiPositionFavButt Is Nothing Then Exit Sub
    '    uiPositionLat.Visibility = Visibility.Collapsed
    '    uiPositionLong.Visibility = Visibility.Collapsed
    '    uiPositionButt.Visibility = Visibility.Collapsed
    '    uiPositionFavButt.Visibility = Visibility.Collapsed
    '    uiPositionName.Visibility = Visibility.Collapsed
    '    Setup_SizeChanged(sender, Nothing) ' wielkosc WebView

    '    Dim iTmp As Integer = uiPosition.SelectedIndex
    '    If iTmp < 0 Then Exit Sub

    '    Dim sTxt As String = uiPosition.Items.ElementAt(iTmp)

    '    If sTxt = ResourceLoader.GetForCurrentView().GetString("resSettPosItemGPS") Then
    '        ' uzywaj GPS
    '        App.mdLat = 100
    '        uiGPSPrecSld.Visibility = Visibility.Visible
    '        uiGPSPrecTxt.Visibility = Visibility.Visible
    '        eMaxOdl_Changed(Nothing, Nothing)
    '        Exit Sub
    '    End If

    '    If sTxt = ResourceLoader.GetForCurrentView().GetString("resSettPosItemEnter") Then

    '        uiPositionLat.Visibility = Visibility.Visible
    '        uiPositionLong.Visibility = Visibility.Visible
    '        uiPositionButt.Visibility = Visibility.Visible
    '        uiPositionName.Visibility = Visibility.Visible
    '        Setup_SizeChanged(sender, Nothing) ' wielkosc WebView

    '        ' wstaw aktualne wspolrzedne
    '        Dim oPoint As Point = Await App.GetCurrentPoint
    '        uiPositionLat.Text = oPoint.X
    '        uiPositionLong.Text = oPoint.Y
    '        App.mdLat = oPoint.X
    '        App.mdLong = oPoint.Y


    '        Exit Sub
    '    End If

    'If uiPosition.SelectedValue.Substring(0, 1) = "*" Then
    '    ' nowiutki swiezutki
    '    App.mdLat = uiPositionLat.Text
    '    App.mdLong = uiPositionLong.Text
    '    Exit Sub
    'End If

    ' to bedzie jakis Favourite
    '    Try
    '        Dim oItem As IXmlNode = oXmlPlaces.DocumentElement.SelectSingleNode("//place[@name='" & sTxt & "']")
    '        App.mdLat = oItem.SelectSingleNode("@lat").NodeValue
    '        App.mdLong = oItem.SelectSingleNode("@long").NodeValue
    '        uiMaxOdlSld.Value = oItem.SelectSingleNode("@maxOdl").NodeValue

    '        uiPositionFavButt.Visibility = Visibility.Visible
    '        uiGPSPrecSld.Visibility = Visibility.Collapsed
    '        uiGPSPrecTxt.Visibility = Visibility.Collapsed

    '        eMaxOdl_Changed(Nothing, Nothing)
    '    Catch ex As Exception

    '    End Try

    'End Sub

    '<CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId:="e")>
    'Private Sub uiAddRemove_Click(sender As Object, e As RoutedEventArgs)
    '    ' w nowej wersji - to tylko kasowanie
    'End Sub

    'Private Sub uiLat_Changed(sender As Object, e As TextChangedEventArgs) Handles uiPositionLat.TextChanged
    '    If Not Double.TryParse(uiPositionLat.Text, App.mdLat) Then
    '        App.mdLat = 0
    '    End If
    'End Sub

    'Private Sub uiLong_Changed(sender As Object, e As TextChangedEventArgs) Handles uiPositionLong.TextChanged
    '    If Not Double.TryParse(uiPositionLong.Text, App.mdLong) Then
    '        App.mdLong = 0
    '    End If
    'End Sub

    'Private Sub bFavButton_Click(sender As Object, e As RoutedEventArgs) Handles uiPositionFavButt.Click
    '    Dim iTmp As Integer = uiPosition.SelectedIndex
    '    Dim sTxt As String = uiPosition.Items.ElementAt(iTmp)

    '    oXmlPlaces.DocumentElement.RemoveChild(
    '            oXmlPlaces.DocumentElement.SelectSingleNode("//place[@name='" & sTxt & "']"))
    '    App.SetSettingsString("favPlaces", oXmlPlaces.GetXml, True)
    '    ' select [locator] - ale moze niech sie samo zmieni...

    '    uiPositionFavButt.Visibility = Visibility.Collapsed

    '    ReloadFavPlaces()
    '    uiPosition.SelectedIndex = 0


    'End Sub

    'Private Sub ReloadFavPlaces()
    '    Dim sTxt As String
    '    sTxt = App.GetSettingsString("favPlaces", "<places></places>")
    '    'sTxt = "<places></places>"
    '    Dim bError As Boolean = False
    '    Try
    '        oXmlPlaces.LoadXml(sTxt)
    '    Catch ex As Exception
    '        bError = True
    '    End Try
    '    If bError Then App.DialogBox("ERROR loading favourites list")

    '    ' dwa podstawowe
    '    uiPosition.Items.Clear
    '    uiPosition.Items.Add(ResourceLoader.GetForCurrentView().GetString("resSettPosItemGPS"))
    '    uiPosition.Items.Add(ResourceLoader.GetForCurrentView().GetString("resSettPosItemEnter"))

    '    For Each oPlace As IXmlNode In oXmlPlaces.DocumentElement.SelectNodes("//place")
    '        uiPosition.Items.Add(oPlace.SelectSingleNode("@name").NodeValue)
    '    Next

    'End Sub

    'Private Sub Setup_SizeChanged(sender As Object, e As SizeChangedEventArgs)
    '    ' poprzednio ręcznie, teraz samo niby (via Stretch)
    '    'If uiSetupWebView Is Nothing Then Exit Sub
    '    'uiSetupWebView.Height = uiPage.ActualHeight - uiSetup_Grid.ActualHeight - 20    ' zgaduje, tu sie powinno odjac poczatek rysowania (Y.top)
    'End Sub
#End Region

    Private Sub uiGpsPrec_Changed(sender As Object, e As RangeBaseValueChangedEventArgs) Handles uiGPSPrecSld.ValueChanged

        If uiGPSPrecTxt Is Nothing Then Exit Sub
        uiGPSPrecTxt.Text = uiGPSPrecSld.Value & " m"

        ' musi od razu, żeby zaraz zaczęło działać (np. przy przestawianiu odleglosci od przystanku)
        SetSettingsInt("gpsPrec", uiGPSPrecSld.Value)
    End Sub

    Private Sub uiPage_LostFocus(sender As Object, e As RoutedEventArgs) Handles uiPage.LostFocus
        'If moTask Is Nothing Then Exit Sub
        'If moTask.IsCompleted Then Exit Sub

        ' i co dalej nie wiem, mialoby jakos zrobic Cancel
    End Sub

    Private Sub uiOpenPosPanel_Click(sender As Object, e As RoutedEventArgs)
        uiPositionLat.Visibility = Visibility.Visible
        uiPositionLong.Visibility = Visibility.Visible
        uiPositionButt.Visibility = Visibility.Visible
        uiPositionName.Visibility = Visibility.Visible
        uiOpenPosPanel.Visibility = Visibility.Collapsed
    End Sub

    Private Sub UiAlsoBus_Toggled(sender As Object, e As RoutedEventArgs) Handles uiAlsoBus.Toggled
        eMaxOdl_Changed(Nothing, Nothing)
    End Sub
End Class
