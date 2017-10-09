' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

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

    Private Sub bOk_Click(sender As Object, e As RoutedEventArgs)
        App.SetSettingsInt("maxOdl", uiMaxOdlSld.Value)
        App.SetSettingsInt("walkSpeed", uiWalkSpeedSld.Value)
        App.SetSettingsInt("alsoNext", uiAlsoNextSld.Value)
        App.SetSettingsString("favPlaces", oXmlPlaces.GetXml)

        Me.Frame.Navigate(GetType(MainPage))
    End Sub

    Private Sub bLoadStops_Click(sender As Object, e As RoutedEventArgs)
        App.CheckLoadStopList(True)
    End Sub

    Private Sub Setup_Loaded(sender As Object, e As RoutedEventArgs)
        uiMaxOdlSld.Value = App.GetSettingsInt("maxOdl", 1000)
        uiWalkSpeedSld.Value = App.GetSettingsInt("walkSpeed", 4)
        uiAlsoNextSld.Value = App.GetSettingsInt("alsoNext", 5)
        uiPositionLat.Visibility = Visibility.Collapsed
        uiPositionLong.Visibility = Visibility.Collapsed
        uiPositionName.Visibility = Visibility.Collapsed
        uiPositionButt.Visibility = Visibility.Collapsed

        Setup_SizeChanged(sender, Nothing) ' wielkosc WebView

        ReloadFavPlaces()
        uiPosition.SelectedIndex = 0
    End Sub
    Private Sub ReloadFavPlaces()
        Dim sTxt As String
        sTxt = App.GetSettingsString("favPlaces", "<places></places>")
        'sTxt = "<places></places>"
        Try
            oXmlPlaces.LoadXml(sTxt)
        Catch ex As Exception
            App.DialogBox("ERROR loading favourites list")
        End Try

        ' dwa podstawowe
        uiPosition.Items.Clear
        uiPosition.Items.Add(ResourceLoader.GetForCurrentView().GetString("resSettPosItemGPS"))
        uiPosition.Items.Add(ResourceLoader.GetForCurrentView().GetString("resSettPosItemEnter"))

        For Each oPlace In oXmlPlaces.DocumentElement.SelectNodes("//place")
            uiPosition.Items.Add(oPlace.SelectSingleNode("@name").NodeValue)
        Next

    End Sub
    Private Sub Setup_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        If uiSetupWebView Is Nothing Then Exit Sub
        uiSetupWebView.Height = uiPage.ActualHeight - uiSetup_Grid.ActualHeight - 20    ' zgaduje, tu sie powinno odjac poczatek rysowania (Y.top)
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

        'Dim sHtml = "<html><body>"
        'Dim iOdl As Integer
        'For Each oNode In App.oStops.SelectNodes("//stop")
        '    iOdl = App.GPSdistanceDwa(oPoint.X, oPoint.Y, oNode.SelectSingleNode("@lat").NodeValue, oNode.SelectSingleNode("@lon").NodeValue)
        '    If iOdl < uiMaxOdlSld.Value Then
        '        sHtml = sHtml & "<li><b>" & oNode.SelectSingleNode("@name").NodeValue & "</b>, " & iOdl & " metrów"
        '    End If
        'Next
        'sHtml = sHtml & "</body></html>"

        Dim sXml = "<root>"
        Dim iOdl As Integer
        Dim iCnt As Integer = 0
        Dim iMinOdl As Integer = 100000
        Dim sTmp As String

        For Each oNode In App.oStops.SelectNodes("//stop")
            iOdl = App.GPSdistanceDwa(oPoint.X, oPoint.Y, oNode.SelectSingleNode("@lat").NodeValue, oNode.SelectSingleNode("@lon").NodeValue)
            If iOdl < uiMaxOdlSld.Value Then
                sXml = sXml & "<item name='" & oNode.SelectSingleNode("@name").NodeValue & "' odl='" & iOdl & "' "
                iOdl = 60 * iOdl / (uiWalkSpeedSld.Value * 1000)
                sXml = sXml & "odlMinut='" & iOdl & "' />"
                iCnt = iCnt + 1
            End If
            iMinOdl = Math.Min(iMinOdl, iOdl)
        Next

        sXml = sXml & "</root>"

        If iCnt > 0 Then


            ' tester: http://xslttest.appspot.com/
            Dim sXslt = "
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

            Dim oXsltDoc As New XmlDocument
            oXsltDoc.LoadXml(sXslt)
            Dim oXP = New XsltProcessor(oXsltDoc)
            Dim oXmlDoc As New XmlDocument
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

    Private Async Sub uiPosition_Changed(sender As Object, e As SelectionChangedEventArgs) Handles uiPosition.SelectionChanged

        If uiPositionFavButt Is Nothing Then Exit Sub
        uiPositionLat.Visibility = Visibility.Collapsed
        uiPositionLong.Visibility = Visibility.Collapsed
        uiPositionButt.Visibility = Visibility.Collapsed
        uiPositionFavButt.Visibility = Visibility.Collapsed
        uiPositionName.Visibility = Visibility.Collapsed
        Setup_SizeChanged(sender, Nothing) ' wielkosc WebView

        Dim iTmp As Integer = uiPosition.SelectedIndex
        If iTmp < 0 Then Exit Sub

        Dim sTxt As String = uiPosition.Items.ElementAt(iTmp)

        If sTxt = ResourceLoader.GetForCurrentView().GetString("resSettPosItemGPS") Then
            ' uzywaj GPS
            App.mdLat = 100
            eMaxOdl_Changed(Nothing, Nothing)
            Exit Sub
        End If

        If sTxt = ResourceLoader.GetForCurrentView().GetString("resSettPosItemEnter") Then

            uiPositionLat.Visibility = Visibility.Visible
            uiPositionLong.Visibility = Visibility.Visible
            uiPositionButt.Visibility = Visibility.Visible
            uiPositionName.Visibility = Visibility.Visible
            Setup_SizeChanged(sender, Nothing) ' wielkosc WebView

            ' wstaw aktualne wspolrzedne
            Dim oPoint As Point = Await App.GetCurrentPoint
            uiPositionLat.Text = oPoint.X
            uiPositionLong.Text = oPoint.Y
            App.mdLat = oPoint.X
            App.mdLong = oPoint.Y


            Exit Sub
        End If

        'If uiPosition.SelectedValue.Substring(0, 1) = "*" Then
        '    ' nowiutki swiezutki
        '    App.mdLat = uiPositionLat.Text
        '    App.mdLong = uiPositionLong.Text
        '    Exit Sub
        'End If

        ' to bedzie jakis Favourite
        Try
            Dim oItem = oXmlPlaces.DocumentElement.SelectSingleNode("//place[@name='" & sTxt & "']")
            App.mdLat = oItem.SelectSingleNode("@lat").NodeValue
            App.mdLong = oItem.SelectSingleNode("@long").NodeValue
            uiMaxOdlSld.Value = oItem.SelectSingleNode("@maxOdl").NodeValue

            uiPositionFavButt.Visibility = Visibility.Visible
            eMaxOdl_Changed(Nothing, Nothing)
        Catch ex As Exception

        End Try

    End Sub

    '<CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId:="e")>
    'Private Sub uiAddRemove_Click(sender As Object, e As RoutedEventArgs)
    '    ' w nowej wersji - to tylko kasowanie
    'End Sub

    Private Sub uiPositOk_Click(sender As Object, e As RoutedEventArgs) Handles uiPositionButt.Click
        ' dodawanie nowego entry

        Dim sTxt = uiPositionName.Text
        sTxt = sTxt.Replace("*", "")
        sTxt = sTxt.Replace("[", "")
        sTxt = sTxt.Replace("]", "")

        If sTxt.Length > 3 Then
            ' ominiecie zabawy w Nodes - dodajemy na koncu
            sTxt = "<place name='" & sTxt & "'" &
                " long='" & App.mdLong & "'" &
                " lat='" & App.mdLat & "'" &
                " maxOdl='" & uiMaxOdlSld.Value & "' />" & vbCrLf
            sTxt = oXmlPlaces.GetXml.Replace("</places>", sTxt & "</places>")
            Try
                oXmlPlaces.LoadXml(sTxt)
                App.SetSettingsString("favPlaces", sTxt, True)
            Catch ex As Exception

            End Try
            uiPositionName.Visibility = Visibility.Collapsed
            uiPositionFavButt.Visibility = Visibility.Collapsed
            ReloadFavPlaces()
            uiPosition.SelectedIndex = uiPosition.Items.Count - 1 ' na ostatni (bo bez sortowania)
        Else
            App.DialogBoxRes("resErrorNazwaZaKrotka")
        End If

    End Sub

    Private Sub uiLat_Changed(sender As Object, e As TextChangedEventArgs) Handles uiPositionLat.TextChanged
        App.mdLat = uiPositionLat.Text
    End Sub

    Private Sub uiLong_Changed(sender As Object, e As TextChangedEventArgs) Handles uiPositionLong.TextChanged
        App.mdLong = uiPositionLong.Text
    End Sub

    Private Sub bFavButton_Click(sender As Object, e As RoutedEventArgs) Handles uiPositionFavButt.Click
        Dim iTmp As Integer = uiPosition.SelectedIndex
        Dim sTxt As String = uiPosition.Items.ElementAt(iTmp)

        oXmlPlaces.DocumentElement.RemoveChild(
                oXmlPlaces.DocumentElement.SelectSingleNode("//place[@name='" & sTxt & "']"))
        App.SetSettingsString("favPlaces", oXmlPlaces.GetXml, True)
        ' select [locator] - ale moze niech sie samo zmieni...

        uiPositionFavButt.Visibility = Visibility.Collapsed

        ReloadFavPlaces()
        uiPosition.SelectedIndex = 0


    End Sub
End Class
