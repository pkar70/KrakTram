' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Windows.Data.Json
Imports Windows.Data.Xml.Dom
Imports Windows.Data.Xml.Xsl
Imports Windows.Devices.Geolocation
Imports Windows.Web.Http
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class MainPage
    Inherits Page

    Dim miSortMode As Integer = 0
    Dim msXml As String

    Private Sub bSetup_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Setup))
    End Sub


    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        App.CheckLoadStopList()
    End Sub

    Private Shared Function VehicleId2VehicleType(sTmp As String) As String
        ' https://github.com/jacekkow/mpk-ttss/blob/master/common.js

        If sTmp.Length < 15 Then Return " " ' <error>, znaczy nie ma danych w tabliczce
        If sTmp.Substring(0, 15) <> "635218529567218" Then Return "???"
        Dim id = CInt(sTmp.Substring(15)) - 736
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


    Private Async Function GetTablicaXml(iId As Integer, iOdl As Integer) As Task(Of String)
        Dim oHttp As New HttpClient()
        Dim sTmp As String
        sTmp = Await oHttp.GetStringAsync(New Uri("http://www.ttss.krakow.pl/internetservice/services/passageInfo/stopPassages/stop?mode=departure&stop=" & iId))

        Dim oJson As JsonObject
        Try
            oJson = JsonObject.Parse(sTmp)
        Catch ex As Exception
            App.DialogBox("ERROR: JSON parsing error - tablica")
            Return ""
        End Try

        Dim oJsonStops As New JsonArray
        Try
            oJsonStops = oJson.GetNamedArray("actual")
        Catch ex As Exception
            App.DialogBox("ERROR: JSON ""actual"" array missing")
            Return ""
        End Try

        If oJsonStops.Count = 0 Then
            ' przeciez tabliczka moze byc pusta (po kursach, przystanek nieczynny...)
            Return ""
        End If

        Dim oXml = New XmlDocument
        Dim oRoot = oXml.CreateElement("root")
        oXml.AppendChild(oRoot)

        Dim iMinSec As Integer = 3600 * iOdl / (App.GetSettingsInt("walkSpeed", 4) * 1000)

        For Each oVal In oJsonStops

            Dim iCurrSec As Integer = oVal.GetObject.GetNamedNumber("actualRelativeTime", 0)

            If iCurrSec > iMinSec Then  ' tylko kiedy mozna zdążyć

                Dim oNode As XmlElement = oXml.CreateElement("item")
                oNode.SetAttribute("line", oVal.GetObject.GetNamedString("patternText", "!error!"))
                oNode.SetAttribute("typ", VehicleId2VehicleType(
                    oVal.GetObject.GetNamedString("vehicleId", "!error!")))
                oNode.SetAttribute("numer",
                    oVal.GetObject.GetNamedString("vehicleId", "12345678901234599999").Substring(15))
                oNode.SetAttribute("dir", oVal.GetObject.GetNamedString("direction", "!error!"))
                oNode.SetAttribute("time",
                    oVal.GetObject.GetNamedString("mixedTime", "!error!").Replace("Min", "min"))
                oNode.SetAttribute("timeSec", iCurrSec.ToString)
                oNode.SetAttribute("stop", oJson.GetObject.GetNamedString("stopName", "!error!"))
                oNode.SetAttribute("odl", iOdl.ToString)
                oNode.SetAttribute("odlMin", iMinSec \ 60)
                oNode.SetAttribute("odlSec", iMinSec)


                Dim bBylo As Boolean = False

                Dim sTxt As String = "//item"
                sTxt = sTxt & "[@line='" & oVal.GetObject.GetNamedString("patternText", "!error!") &
                "' and @dir='" & oVal.GetObject.GetNamedString("direction", "!error!") & "']"
                For Each oTmpNode In oRoot.SelectNodes(sTxt)
                    Dim iOldSec = oTmpNode.SelectSingleNode("@timeSec").NodeValue
                    If iCurrSec > iOldSec + 60 * App.GetSettingsInt("alsoNext", 5) Then
                        bBylo = True
                        Exit For
                    End If
                Next
                If Not bBylo Then oRoot.AppendChild(oNode)

            End If

        Next

        sTmp = oXml.GetXml
        sTmp = sTmp.Replace("<root>", "")
        sTmp = sTmp.Replace("</root>", "")
        Return sTmp

    End Function
    Private Shared Sub OdfiltrujMiedzytabliczkowo(ByRef oXml As XmlDocument)
        ' usuwa z oXml to co powinien :)
        ' <root><item ..>
        ' o tabliczce: stop, odl, odlMin, odlSec - nazwa, odleglosc: metry, minuty, sec
        ' o tramwaju: line, dir, time, timSec, typ, numer - linia, kierunek, mixedTime, sekundy, typ (eu8n), numer wozu

        Dim iInd, iInd1 As Integer
        Dim oRoot = oXml.DocumentElement.SelectSingleNode("//root")
        For iInd = oRoot.SelectNodes("//item").Count - 1 To 0 Step -1
            Dim oNode = oRoot.ChildNodes.Item(iInd)
            For iInd1 = iInd - 1 To 0 Step -1

                If oRoot.ChildNodes.Item(iInd1).SelectSingleNode("@numer").NodeValue = oRoot.ChildNodes.Item(iInd).SelectSingleNode("@numer").NodeValue Then
                    If CInt(oRoot.ChildNodes.Item(iInd1).SelectSingleNode("@odlMin").NodeValue) >
                        CInt(oNode.SelectSingleNode("@odlMin").NodeValue) Then
                        oXml.DocumentElement.RemoveChild(oRoot.ChildNodes.Item(iInd1))
                        Exit For
                    End If
                End If
            Next
        Next

    End Sub

    Private Async Sub bGetGPS_Click(sender As Object, e As RoutedEventArgs)
        Dim iOdl As Integer

        If Not Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
            App.DialogBoxRes("resErrorNoNetwork")
            Exit Sub
        End If

        uiGetTtss.Visibility = Visibility.Collapsed
        uiWebView.Visibility = Visibility.Visible

        Dim oPoint As Point = Await App.GetCurrentPoint
        Dim sHtml = ""

        For Each oNode In App.oStops.SelectNodes("//stop")
            iOdl = App.GPSdistanceDwa(oPoint.X, oPoint.Y, oNode.SelectSingleNode("@lat").NodeValue, oNode.SelectSingleNode("@lon").NodeValue)
            If iOdl < App.GetSettingsInt("maxOdl") Then
                sHtml = sHtml & Await GetTablicaXml(CInt(oNode.SelectSingleNode("@id").NodeValue), iOdl)
            End If
        Next

        msXml = sHtml
        WypiszTabele()

    End Sub
    <CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId:="System.Int32.ToString")>
    Private Sub WypiszTabele()
        Dim sXml As String
        sXml = msXml

        If sXml = "" Then
            sXml = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("resZeroKursow")
            uiWebView.NavigateToString("<html><body><h1>" & sXml & "</h1></body></html>")
            Exit Sub
        End If

        sXml = "<root>" & sXml & "</root>"
        Dim oXmlDoc As New XmlDocument
        oXmlDoc.LoadXml(sXml)

        OdfiltrujMiedzytabliczkowo(oXmlDoc)

        ' tester: http://xslttest.appspot.com/
        Dim sXslt = "
<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0'>
    <xsl:template match='root'>
        <html>
            <head>
            <style>
.Line  {font-size:#FONTSIZELINE#pt; font-weight:bold}
.Center {text-align:center;}
.Kier  {font-size:#FONTSIZEKIER#pt; font-weight:bold}
.Stop  {font-size:#FONTSIZESTOP#pt}
.Time  {font-size:#FONTSIZETIME#pt}
.Typ  {font-size:#FONTSIZEMINI#pt}
.Odl   {font-size:#FONTSIZEODL#pt}
            </style>
            </head>
            <body>
                <table border='1'>
                    <xsl:apply-templates select='/root/item'>
    #SORTBY#
                    </xsl:apply-templates>
                </table>
            </body>
        </html>
    </xsl:template>

  <xsl:template match='root/item'>
    <tr><td rowspan='2' class='Center'><span class='Line'><xsl:value-of select='@line'/></span><br/>
        <span class='Typ'><xsl:value-of select='@typ'/></span></td>
     <td><span class='Kier'><xsl:value-of select='@dir'/></span></td>
    <td rowspan='2' class='Center'><span class='Time'><xsl:value-of select='@time'/></span></td>
    </tr>
    <tr><td><span class='Stop'><xsl:value-of select='@stop'/> </span> 
        <span class='Odl'> (<xsl:value-of select='@odl'/> m, <xsl:value-of select='@odlMin'/> min)</span></td>
    </tr>

  </xsl:template>
</xsl:stylesheet>
"
        ' <span class='Typ'> dla @stop - bo błąd w telefonie, i pokazuje duże

        ' skalowanie
        Dim dTmp = Windows.Graphics.Display.DisplayInformation.GetForCurrentView.RawPixelsPerViewPixel
        dTmp = dTmp * dTmp * dTmp
        ' PC = 1, Lumia = 1.5; kolejne potegi: 1.5; 2.25, 3.375, 5.062

        sXslt = sXslt.Replace("#FONTSIZELINE#", CInt(12 * dTmp * 1.7).ToString)
        sXslt = sXslt.Replace("#FONTSIZEMINI#", CInt(12 * dTmp * 0.75).ToString)    ' typ
        sXslt = sXslt.Replace("#FONTSIZETIME#", CInt(12 * dTmp * 1.25).ToString)
        sXslt = sXslt.Replace("#FONTSIZEKIER#", CInt(12 * dTmp).ToString)

        ' EDGE BUG OVERRIDE
        If CInt(12 * dTmp * 0.85) = 34 Then
            sXslt = sXslt.Replace("#FONTSIZESTOP#", "24")
            sXslt = sXslt.Replace("#FONTSIZEODL#", "20")    ' odl
        Else    ' normalnie, np. PC
            sXslt = sXslt.Replace("#FONTSIZESTOP#", CInt(12 * dTmp * 0.85).ToString)
            sXslt = sXslt.Replace("#FONTSIZEODL#", CInt(12 * dTmp * 0.75).ToString)    ' odl
        End If

        Dim sTmp As String
        Select Case miSortMode
            Case 1  ' stop/czas/dir
                sTmp = "<xsl:sort select='@stop' /><xsl:sort data-type='number' select='@timeSec' /><xsl:sort select='@dir' />"
            Case 2  ' dir/stop/czas
                sTmp = "<xsl:sort select='@dir' /><xsl:sort select='@stop' /><xsl:sort data-type='number' select='@timeSec' />"
            Case Else   ' czyli takze domyslne zero; linia/kierunek/czas
                sTmp = "<xsl:sort data-type='number' select='@line' /><xsl:sort select='@dir' /><xsl:sort data-type='number' select='@timeSec' />"
        End Select

        sXslt = sXslt.Replace("#SORTBY#", sTmp)
        Dim oXsltDoc As New XmlDocument
        oXsltDoc.LoadXml(sXslt)
        Dim oXP = New XsltProcessor(oXsltDoc)
        sTmp = oXP.TransformToString(oXmlDoc.DocumentElement)

        'sHtml = sHtml.Replace("</body>", "<p>RawPixelsPerViewPixel = " & iTmp.ToString & "</p></body>")
        uiWebView.NavigateToString(sTmp)
    End Sub

    Private Sub Page_Resized(sender As Object, e As SizeChangedEventArgs)
        uiWebView.Height = uiMainGrid.ActualHeight - 50
    End Sub

    Private Sub bSortByLine_Click(sender As Object, e As RoutedEventArgs)
        miSortMode = 0
        uiSortKier.IsChecked = False
        uiSortLine.IsChecked = True
        uiSortStop.IsChecked = False
        WypiszTabele()
    End Sub

    Private Sub bSortByStop_Click(sender As Object, e As RoutedEventArgs)
        miSortMode = 1
        uiSortKier.IsChecked = False
        uiSortLine.IsChecked = False
        uiSortStop.IsChecked = True
        WypiszTabele()
    End Sub

    Private Sub bSortByKier_Click(sender As Object, e As RoutedEventArgs)
        miSortMode = 2
        uiSortKier.IsChecked = True
        uiSortLine.IsChecked = False
        uiSortStop.IsChecked = False
        WypiszTabele()
    End Sub
End Class
