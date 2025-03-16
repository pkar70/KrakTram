Imports System.Linq
Imports System.Net.Http
Imports System.Net.Security
'Imports System.Security.Cryptography.X509Certificates

Namespace MpkMain
    ' z namespace, by nie by³o widaæ niepotrzebnie struktur z których siê nie korzysta

    ''' <summary>
    ''' klasa bezpoœrednio rozmawiaj¹ca z MPK
    ''' </summary>
    Public Class MPK

        Private oHttp As New Net.Http.HttpClient
        'Private _handler As HttpClientHandler

        'Public Sub New()
        '    'Dim _handler As New HttpClientHandler
        '    '_handler.ClientCertificateOptions = ClientCertificateOption.Manual
        '    '_handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 'Function(hrm, c2, cc, pe) True 'AddressOf ServerCertificateCustomValidation
        '    'oHttp = New HttpClient(_handler)

        'End Sub

        'Private Shared Function ServerCertificateCustomValidation(requestMessage As HttpRequestMessage, certif As X509Certificate2, chain As X509Chain, sslErrors As SslPolicyErrors) As Boolean
        '    Return True
        '    '// It Is possible to inspect the certificate provided by the server.
        '    'Console.WriteLine($"Requested URI: {requestMessage.RequestUri}");
        '    'Console.WriteLine($"Effective date {certificate?.GetEffectiveDateString()}");
        '    'Console.WriteLine($"Exp date {certificate?.GetExpirationDateString()}");
        '    'Console.WriteLine($"Issuer {certificate?.Issuer}");
        '    'Console.WriteLine($"Subject {certificate?.Subject}");

        '    '// Based on the custom logic it Is possible to decide whether the client considers certificate valid Or Not
        '    'Console.WriteLine($"Errors {sslErrors}");
        '    'Return sslErrors == SslPolicyErrors.None;
        'End Function

        ''' <summary>
        ''' Baselink dla komend REST dla tramwajów
        ''' </summary>
        Public Property UriTram As String = "http://www.ttss.krakow.pl/"

        ''' <summary>
        ''' Baselink dla komend REST dla autobusów
        ''' </summary>
        Public Property UriBus As String = "https://ttss.mpk.krakow.pl/"
        '#If DEBUG Then
        '        Public Property UriBus As String = "https://bus.mpk.krakow.pl/"
        '#Else
        ' Public Property UriBus As String = "https://91.223.13.70/"
        '#End If

        ''' <summary>
        ''' zwraca baselink w zale¿noœci od bus/tram (ze slash na koñcu)
        ''' </summary>
        Public Function GetUriBase(isBus As Boolean) As String
            If isBus Then Return UriBus
            Return UriTram
        End Function


#Region "lista przystanków"

        Private moPrzystanki As List(Of MpkPrzystanek)
        Public Property errMessage As String = ""

        ''' <summary>
        ''' zwraca sumê list przystanków
        ''' </summary>
        ''' <returns>lista, ale tak¿e sprawdŸ errMessage (mo¿e wczytaæ tylko jedn¹ z dwu list na przyk³ad)</returns>
        Public Async Function DownloadListaPrzystankowAsync(Optional bTram As Boolean = True, Optional bBus As Boolean = True, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of MpkPrzystanek))
            moPrzystanki = New List(Of MpkPrzystanek)
            errMessage = ""

            Dim sRect As String = "?left=-648000000&bottom=-324000000&right=648000000&top=324000000"
            Dim sRest As String = "internetservice/geoserviceDispatcher/services/stopinfo/stops"

            If bTram Then
                Await ImportMainAsync(GetUriBase(False) & sRest & sRect, iTimeoutSecs)
                If errMessage <> "" Then errMessage &= vbCrLf
            End If

            If bBus Then
                Await ImportMainAsync(GetUriBase(True) & sRest & sRect, iTimeoutSecs)
                If errMessage <> "" Then errMessage &= vbCrLf
            End If

            Return moPrzystanki
        End Function


        ''' <summary>
        ''' Dodaje listê przystanków
        ''' </summary>
        ''' <returns>Zwraca "" gdy OK lub string gdy error</returns>
        Private Async Function ImportMainAsync(sUrl As String, iTimeoutSecs As Integer) As Task(Of String)

            Dim sPage As String

            If oHttp.Timeout.TotalSeconds <> iTimeoutSecs Then
                oHttp = New Net.Http.HttpClient
                oHttp.Timeout = TimeSpan.FromSeconds(iTimeoutSecs)
            End If

            Try
                sPage = Await oHttp.GetStringAsync(sUrl)
            Catch ex As Exception
                Return "resErrorGetHttp"
            End Try

            If sPage.IndexOf("""stops""") < 0 Then Return "resErrorBadTTSSstops"
            Dim iInd As Integer = sPage.IndexOf("[")
            sPage = sPage.Substring(iInd)
            iInd = sPage.LastIndexOf("]")
            sPage = sPage.Substring(0, iInd + 1)

            Dim oItemy As List(Of MpkPrzystanek)

            Try
                oItemy = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(List(Of MpkPrzystanek)))
            Catch ex As Exception
                Return "JsonConvert.DeserializeObject exception"
            End Try

            moPrzystanki = moPrzystanki.Concat(oItemy).ToList

            Return ""
        End Function

#End Region

        ''' <summary>
        ''' wczytaj string (zapewne JSON) dla autobusów/tramwajów, z REST path
        ''' </summary>
        Public Async Function ReadRest(isBus As Boolean, UriPath As String, Optional iTimeoutSecs As Integer = 10) As Task(Of String)

            If oHttp.Timeout.TotalSeconds <> iTimeoutSecs Then
                oHttp = New Net.Http.HttpClient
                oHttp.Timeout = TimeSpan.FromSeconds(iTimeoutSecs)
            End If

            Dim url As String = GetUriBase(isBus) & UriPath
            System.Diagnostics.Debug.WriteLine("REST: " + url)

            Try
                Return Await oHttp.GetStringAsync(url)
            Catch
            End Try

            Return Nothing
        End Function

#Region "wczytanie tabliczki"

        ''' <summary>
        ''' wczytaj tabliczkê, lub NULL, gdy nieudane
        ''' </summary>
        Public Async Function WczytajTabliczkeAsync(isBus As Boolean, sId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkTabliczka)

            Dim sParams As String = "?mode=departure&stop=" & sId
            Dim sRest As String = "internetservice/services/passageInfo/stopPassages/stop"

            Dim sPage As String = Await ReadRest(isBus, sRest & sParams, iTimeoutSecs)
            If sPage Is Nothing Then Return Nothing

            Try
                Return Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(MpkTabliczka))
            Catch ex As Exception
            End Try

            Return Nothing
        End Function

#End Region

#Region "dalsza trasa tripa"
        Public Async Function DownloadDalszaTrasaAsync(isBus As Boolean, tripId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkDalszaTrasa)
            Dim sParams As String = "?tripId=" & tripId
            Dim sRest As String = "internetservice/services/tripInfo/tripPassages"

            Dim sPage As String = Await ReadRest(isBus, sRest & sParams, iTimeoutSecs)
            If sPage Is Nothing Then Return Nothing

            Try
                Return Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(MpkDalszaTrasa))
            Catch ex As Exception
            End Try

            Return Nothing

        End Function

#End Region

#Region "przystanki na linii"
        Public Async Function DownloadRouteTrasaAsync(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkRouteStops)
            Dim sParams As String = "?routeId=" & routeId
            Dim sRest As String = "internetservice/services/routeInfo/routeStops"

            Dim sPage As String = Await ReadRest(isBus, sRest & sParams, iTimeoutSecs)
            If sPage Is Nothing Then Return Nothing

            Try
                Return Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(MpkRouteStops))
            Catch ex As Exception
            End Try

            Return Nothing

        End Function

#End Region

#Region "vehicleList"
        Public Async Function DownloadVehiclesState(isBus As Boolean, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkVehiclesState)
            Dim sRest As String = "internetservice/geoserviceDispatcher/services/vehicleinfo/vehicles"

            Dim sPage As String = Await ReadRest(isBus, sRest, iTimeoutSecs)
            If sPage Is Nothing Then Return Nothing

            Try
                Return Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(MpkVehiclesState))
            Catch ex As Exception
            End Try

            Return Nothing

        End Function

#End Region

#Region "trasa na mapie"


        ''' <summary>
        ''' wczytaj trasê do pokazywania na mapie, wedle pojazdu (id zwykle jest ujemny)
        ''' </summary>
        Public Async Function DownloadTrasaNaMapieVehicle(isBus As Boolean, vehicleId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo)
            Dim sRest As String = "internetservice/geoserviceDispatcher/services/pathinfo/vehicle"
            Dim sParam As String = "?id=" & vehicleId

            Dim sPage As String = Await ReadRest(isBus, sRest & sParam, iTimeoutSecs)
            If sPage Is Nothing Then Return Nothing

            Try
                Return Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(MpkPathInfo))
            Catch ex As Exception
            End Try

            Return Nothing

        End Function

        ''' <summary>
        ''' wczytaj trasê do pokazywania na mapie, wedle routeId
        ''' </summary>
        Public Async Function DownloadTrasaNaMapieRoute(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo)
            Dim sRest As String = "internetservice/geoserviceDispatcher/services/pathinfo/route"
            Dim sParam As String = "?id=" & routeId

            Dim sPage As String = Await ReadRest(isBus, sRest & sParam, iTimeoutSecs)
            If sPage Is Nothing Then Return Nothing

            Try
                Return Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(MpkPathInfo))
            Catch ex As Exception
            End Try

            Return Nothing

        End Function


#End Region

#Region "wczytanie trasy linii"

        ''' <summary>
        ''' wczytaj listê przystanków trasy konkretnej linii (wywo³ywaæ z LR0, nie 990)
        ''' </summary>
        ''' <returns>NULL w razie b³êdu (i wtedy zobacz errMessage)</returns>
        Public Async Function DownloadTrasaLiniiAsync(linia As String, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of String))

            Dim sPage As String = ""
            errMessage = ""

            Dim oUri As Uri = New Uri("https://rozklady.mpk.krakow.pl/?lang=PL&linia=" & linia) ' http://rozklady.mpk.krakow.pl/?lang=PL&linia=50


            ' normalnie nie zadzia³a - robi redirect do b³êdnego Location - b³¹d po stronie serwera MPK

            Using oHCH As Net.Http.HttpClientHandler = New Net.Http.HttpClientHandler()

                oHCH.AllowAutoRedirect = False

                Using oHttp As Net.Http.HttpClient = New Net.Http.HttpClient(oHCH)

                    oHttp.Timeout = TimeSpan.FromSeconds(iTimeoutSecs)

                    ' oHttp.DefaultRequestHeaders.Add("Referer", )
                    Try
                        Dim oHttResp = Await oHttp.GetAsync(oUri)

                        If oHttResp.StatusCode = Net.HttpStatusCode.Found Then
                            oHttResp = Await oHttp.GetAsync(oHttResp.RequestMessage.RequestUri) ' tak, to jest == oUri!
                        End If
                        If oHttResp.IsSuccessStatusCode Then sPage = Await oHttResp.Content.ReadAsStringAsync()
                    Catch
                        errMessage = "resErrorGetHttp"
                        Return Nothing
                    End Try

                    If String.IsNullOrEmpty(sPage) Then
                        Dim oHttResp = Await oHttp.GetAsync(oUri)

                        If oHttResp.StatusCode = Net.HttpStatusCode.Found Then
                            oHttResp = Await oHttp.GetAsync(oHttResp.RequestMessage.RequestUri)
                        End If

                        If oHttResp.IsSuccessStatusCode Then sPage = Await oHttResp.Content.ReadAsStringAsync()
                    End If

                End Using
            End Using

            ' tak wycinam, bo nie ma ¿adnych ID ani nic takiego, co pozwoli³oby wyci¹æ
            Dim iInd As Integer
            iInd = sPage.IndexOf("Trasa:")
            If iInd < 10 Then
                errMessage = "Bad file structure1"
                Return Nothing
            End If
            iInd = sPage.IndexOf("Przystanki", iInd)    ' samo przystanki jest trzy razy
            If iInd < 10 Then
                errMessage = "Bad file structure2"
                Return Nothing
            End If
            sPage = sPage.Substring(iInd)
            iInd = sPage.IndexOf("<table")
            If iInd < 10 Then
                errMessage = "Bad file structure3"
                Return Nothing
            End If
            If iInd > 250 Then
                errMessage = "Bad file structure4"
                Return Nothing
            End If
            sPage = sPage.Substring(iInd)
            iInd = sPage.IndexOf("</table")
            If iInd < 10 Then
                errMessage = "Bad file structure5"
                Return Nothing
            End If
            sPage = sPage.Substring(0, iInd) & "<table>"

            Dim oHtmlDoc As New HtmlAgilityPack.HtmlDocument()
            oHtmlDoc.LoadHtml(sPage)

            Dim entries As HtmlAgilityPack.HtmlNodeCollection = oHtmlDoc.DocumentNode.SelectNodes("//tr")
            If entries Is Nothing Then
                errMessage = "Cos nie tak, nie widze przystankow"
                Return Nothing
            End If

            Dim stopy As New List(Of String)

            For Each entry As HtmlAgilityPack.HtmlNode In entries
                stopy.Add(entry.SelectSingleNode(".//td").InnerText.Trim)
            Next

            Return stopy
        End Function
#End Region

#Region "wczytanie listy zmian trasy"

        ''' <summary>
        ''' Wczytaj listê zmian w trasach
        ''' </summary>
        ''' <returns>NULL w razie b³êdu (i wtedy zobacz errMessage)</returns>
        Public Async Function DownloadZmianyAsync(Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of MpkZmiana))

            Dim sPage As String
            errMessage = ""

            ' mo¿na by³oby przejœæ na RSS, ale... System.ServiceModel.Syndication jest dopiero od .Net 2 :(
            ' https://ztp.krakow.pl/feed?post_type=komunikat
            ' oraz w tym nie ma linii

            ' przechodzê na HtmlAgilityPack i zjadanie strony

            If oHttp.Timeout.TotalSeconds <> iTimeoutSecs Then
                oHttp = New Net.Http.HttpClient
                oHttp.Timeout = TimeSpan.FromSeconds(iTimeoutSecs)
            End If

            Try
                ' Android: "Ssl error:1000007d:SSL routines:OPENSSL_internal:CERTIFICATE_VERIFY_FAILED\n  at /Users/builder/jenki…"
                sPage = Await oHttp.GetStringAsync(New Uri("https://ztp.krakow.pl/transport-publiczny/komunikacja-miejska/komunikaty"))
            Catch
                errMessage = "resErrorGetHttp"
                Return Nothing
            End Try

            Dim moItemy As New List(Of MpkZmiana)

            Dim oHtmlDoc As New HtmlAgilityPack.HtmlDocument()
            oHtmlDoc.LoadHtml(sPage)

            ' iterujemy <div class="card">

            Dim entries As HtmlAgilityPack.HtmlNodeCollection = oHtmlDoc.DocumentNode.SelectNodes("//div[@class='card']")
            If entries Is Nothing Then
                errMessage = "Cos nie tak, nie powinno byc null - jakies komunikaty jednak powinny byæ"
                Return Nothing
            End If

            For Each entry As HtmlAgilityPack.HtmlNode In entries
                Dim oNew As New MpkZmiana

                ' UWAGA! "." jest istotna, bez niej idzie od DocumentRoot a nie wewn¹trz entry!
                oNew.sCzas = entry.SelectSingleNode(".//div[@class='date']").InnerText.Trim
                oNew.sLinie = entry.SelectSingleNode(".//div[@class='lines']").InnerText.Trim
                oNew.sTytul = entry.SelectSingleNode(".//div[@class='message-title']").InnerText.Trim
                oNew.sInfo = entry.SelectSingleNode(".//div[@class='card-body-inner']/div").InnerText.Trim
                oNew.sLink = entry.SelectSingleNode(".//a").Attributes("href").Value

                moItemy.Add(oNew)
            Next

            Return moItemy
        End Function

#End Region



    End Class


    Public Class MpkPrzystanek
        Public Property category As String  ' tram,bus,other
        Public Property id As String    ' typu 8059230041856279808
        Public Property latitude As Integer ' 180078192
        Public Property longitude As Integer    ' 71757072
        Public Property name As String  ' £agiewniki SKA
        Public Property shortName As String ' 3643
    End Class

    Public Class MpkTabliczka
        Public Property actual As List(Of MpkOdjazd)
        Public Property directions() As Object
        Public Property firstPassageTime As Long
        Public Property generalAlerts() As Object
        Public Property lastPassageTime As Long
        Public Property old As List(Of MpkOdjazd)
        Public Property routes As List(Of MpkRoute)
        Public Property stopName As String
        Public Property stopShortName As String
    End Class

    ' inheritowanie, bo chcemy ToJSON mieæ
    Public Class MpkOdjazd
        Inherits pkar.BaseStruct

        Public Property actualRelativeTime As Integer   ' secs
        Public Property actualTime As String    ' HH:mm
        Public Property direction As String ' "SALWATOR"
        Public Property mixedTime As String ' "3 %UNIT_MIN%", 
        Public Property passageid As String
        Public Property patternText As String
        Public Property plannedTime As String
        Public Property routeId As String
        Public Property status As String    ' "PLANNED", "DEPARTED", "PREDICTED"
        Public Property tripId As String
        Public Property vehicleId As String
    End Class

    Public Class MpkRoute
        Public Property alerts() As Object  ' []
        Public Property authority As String ' "MPK"
        Public Property directions As List(Of String)   ' "Kurdwanów P+R", "Pleszów"
        Public Property id As String    ' "8059228650286875580"
        Public Property name As String
        Public Property routeType As String ' "tram"
        Public Property shortName As String ' = name
    End Class

    Public Class MpkDalszaTrasa
        Public Property actual As List(Of MpkDalszyStop)
        Public Property directionText As String
        Public Property old As List(Of MpkDalszyStop)
        Public Property routeName As String
    End Class

    Public Class MpkDalszyStop
        Public Property actualTime As String
        Public Property status As String
        Public Property [stop] As MpkDalszyStop_Stop
        Public Property stop_seq_num As String
    End Class

    Public Class MpkDalszyStop_Stop
        Public Property id As String
        Public Property name As String
        Public Property shortName As String
    End Class

    Public Class MpkZmiana
        Public Property sLinie As String
        Public Property sCzas As String
        Public Property sTytul As String
        Public Property sInfo As String
        Public Property sLink As String

    End Class

    Public Class MpkVehiclesState
        Public Property lastUpdate As Long
        Public Property vehicles As List(Of MpkVehicleState)
    End Class


    Public Class MpkVehicleState
        Public Property path As List(Of MpkVehicleStatePath)
        Public Property isDeleted As Boolean
        Public Property color As String
        Public Property heading As Integer
        Public Property latitude As Integer
        Public Property name As String
        Public Property tripId As String
        Public Property id As String
        Public Property category As String
        Public Property longitude As Integer
    End Class

    Public Class MpkVehicleStatePath
        Public Property y1 As Integer
        Public Property length As Single
        Public Property x1 As Integer
        Public Property y2 As Integer
        Public Property angle As Integer
        Public Property x2 As Integer
    End Class


    Public Class MpkWayPoint
        Public Property lat As Integer
        Public Property lon As Integer
        Public Property seq As String
    End Class
    Public Class MpkPath
        Public Property color As String
        Public Property wayPoints As List(Of MpkWayPoint)
    End Class

    Public Class MpkPathInfo
        Public Property paths As List(Of MpkPath)
    End Class

    Public Class MpkRouteStops
        Public Property route As MpkRoute
        Public Property stops As List(Of MpkRouteStop)
    End Class

    Public Class MpkRouteStop
        Public Property id As String
        Public Property name As String
        Public Property number As String
    End Class


End Namespace
