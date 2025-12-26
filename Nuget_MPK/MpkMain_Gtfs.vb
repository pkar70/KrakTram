'Imports System.IO
'Imports System.Text.RegularExpressions
'Imports CsvHelper
'Imports CsvHelper.Configuration
'Imports pkar.MpkMain


'Namespace MpkMain
'    ' z namespace, by nie było widać niepotrzebnie struktur z których się nie korzysta

'    Public Class MPK_GTFS
'        inherits MPK_Common

'        Public Property cacheDir As String = ""

'        Private Sub EnsureCacheDir()
'            If cacheDir = "" Then cacheDir = IO.Path.GetTempPath
'            If Not IO.Directory.Exists(cacheDir) Then
'                IO.Directory.CreateDirectory(cacheDir)
'            End If
'        End Sub

'        Public Property errMessage As String = "" Implements IMPKMain.errMessage

'        Private oHttp As New Net.Http.HttpClient


'        Dim baseUrl As String = "https://gtfs.ztp.krakow.pl/"

'#Region "cache plików static"

'        ''' <summary>
'        ''' synchronizacja lokalnego cache z web
'        ''' </summary>
'        Private Async Function GetWebStaticDir() As Task
'            ' sprawdzenie daty pliku w cache, sprawdzenie daty pliku w web i jeśli nowsze, to ściągnij
'            ' w praktyce: niemal codziennie zmienia się plik zip z GTFS

'            ' Pobierz HTML strony
'            Dim html As String = Await oHttp.GetStringAsync(baseUrl)

'            Dim htmldoc As New HtmlAgilityPack.HtmlDocument()
'            htmldoc.LoadHtml(html)

'            For Each plikWeb In htmldoc.DocumentNode.SelectNodes("//li")
'                If Not plikWeb.InnerText.Contains(".zip") Then Continue For

'                ' Regex: nazwa pliku + data modyfikacji
'                Dim rx As New Regex("aktualizacja: (\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
'                Dim match = rx.Match(html)
'                Dim remoteDate As DateTime = DateTime.Parse(match.Groups(0).Value)

'                Dim iInd As Integer = plikWeb.InnerText.IndexOf("GTFS_KRK_")
'                If iInd < 1 Then Continue For
'                Dim fileName As String = plikWeb.InnerText.Substring(iInd, "GTFS_KRK_A.ZIP".Length)

'                Dim localPath As String = IO.Path.Combine(cacheDir, fileName)

'                If IO.File.Exists(localPath) Then
'                    Dim localDate As DateTime = IO.File.GetLastWriteTime(localPath)
'                    If localDate >= remoteDate Then Continue For
'                End If

'                Dim bytes = Await oHttp.GetByteArrayAsync(baseUrl & fileName)
'                IO.File.Delete(localPath)
'                IO.File.WriteAllBytes(localPath, bytes)
'            Next

'        End Function

'        Private Async Function GetFileFromStaticZip(zipfile As String, sFileName As String) As Task(Of String)
'            Dim oZip As New IO.Compression.ZipArchive(IO.File.OpenRead(IO.Path.Combine(cacheDir, zipfile & ".zip")), IO.Compression.ZipArchiveMode.Read)
'            Dim oEntry As IO.Compression.ZipArchiveEntry = oZip.GetEntry(sFileName)
'            Using sr As New IO.StreamReader(oEntry.Open())
'                Return Await sr.ReadToEndAsync()
'            End Using
'        End Function

'        Private Async Function PrzystankiZzipa(zipfile As String, category As String) As Task(Of List(Of MpkPrzystanek))
'            ' zinterpretuj CSV
'            ' zwróć listę po konwersji na MpkPrzystanek

'            Dim ret As New List(Of MpkPrzystanek)

'            Dim csvText As String = Await GetFileFromStaticZip(zipfile, "stops.txt")

'            Dim config = New CsvConfiguration() With {
'                .HasHeaderRecord = True,
'                .IgnoreBlankLines = True
'            }
'            '                 .TrimOptions = TrimOptions.Trim

'            Using reader As New StringReader(csvText)
'                Using csv As New CsvReader(reader, config)

'                    ' Wczytaj nagłówki
'                    csv.Read()
'                    csv.ReadHeader()

'                    Dim prevStopName As String = ""

'                    ' Iteracja po rekordach
'                    While csv.Read()

'                        Dim oNew As New MpkPrzystanek With {.category = category, .id = csv.GetField("stop_id"), .name = csv.GetField("stop_name"), .latitude = csv.GetField(Of Double)("stop_lat"), .longitude = csv.GetField(Of Double)("stop_lon")}
'                        If oNew.name = prevStopName Then Continue While

'                        ' stop_3304_132801,782-01,"Agatowa","01",50.021911666,20.042668333,,,0,,,
'                        ' M: 23,103-05,"Rondo Czyżyńskie","05",50.073820,20.016490,1,,0,,,0
'                        ' A: stop_297_40805,103-05,"Rondo Czyżyńskie","05",50.073819444,20.016491111,,,0,,,
'                        ' T: stop_246_40819,,"Rondo Czyżyńskie","",50.0733,20.01888,,,0,,,,

'                        prevStopName = oNew.name
'                        If csv.GetField("zone_id") = "1" Then oNew.mobilis = True ' null albo 1

'                        If Not oNew.id.Contains("_") Then oNew.id = "mob_" & oNew.id

'                        ret.Add(oNew)

'                    End While

'                End Using
'            End Using

'        End Function


'#End Region

'        ' ściąga listę przystanków - nie ma sam z siebie żadnego cache'owania
'        Public Async Function DownloadListaPrzystankowAsync(Optional bTram As Boolean = True, Optional bBus As Boolean = True, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of MpkPrzystanek)) Implements IMPKMain.DownloadListaPrzystankowAsync
'            Await GetWebStaticDir()

'            Dim ret As New List(Of MpkPrzystanek)
'            If bTram Then
'                ret = Await PrzystankiZzipa("GTFS_KRK_T", "tram")
'            Else
'                ret = Await PrzystankiZzipa("GTFS_KRK_A", "bus")
'                ret = ret.Concat(Await PrzystankiZzipa("GTFS_KRK_M", "bus"))
'            End If

'            ' remove duplicates

'            Return ret
'        End Function

'        Public Function WczytajTabliczkeAsync(isBus As Boolean, sId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkTabliczka) Implements IMPKMain.WczytajTabliczkeAsync

'            'Dim bytes = Await oHttp.GetByteArrayAsync(baseUrl & fileName)
'            '    IO.File.Delete(localPath)
'            'IO.File.WriteAllBytes(localPath, bytes)

'        End Function

'        Public Function DownloadDalszaTrasaAsync(isBus As Boolean, tripId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkDalszaTrasa) Implements IMPKMain.DownloadDalszaTrasaAsync
'            Throw New NotImplementedException()
'        End Function

'        Public Function DownloadRouteTrasaAsync(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkRouteStops) Implements IMPKMain.DownloadRouteTrasaAsync
'            Throw New NotImplementedException()
'        End Function

'        Public Function DownloadVehiclesState(isBus As Boolean, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkVehiclesState) Implements IMPKMain.DownloadVehiclesState
'            Throw New NotImplementedException()
'        End Function

'        Public Function DownloadTrasaNaMapieVehicle(isBus As Boolean, vehicleId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo) Implements IMPKMain.DownloadTrasaNaMapieVehicle
'            Throw New NotImplementedException()
'        End Function

'        Public Function DownloadTrasaNaMapieRoute(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo) Implements IMPKMain.DownloadTrasaNaMapieRoute
'            Throw New NotImplementedException()
'        End Function

'        Public Function DownloadTrasaLiniiAsync(linia As String, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of String)) Implements IMPKMain.DownloadTrasaLiniiAsync
'            Throw New NotImplementedException()
'        End Function

'        Public Function DownloadZmianyAsync(Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of MpkZmiana)) Implements IMPKMain.DownloadZmianyAsync
'            Throw New NotImplementedException()
'        End Function
'    End Class



'End Namespace