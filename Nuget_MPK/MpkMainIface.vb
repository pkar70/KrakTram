
''Imports System.IO
'Imports System.Globalization
'Imports System.IO
'Imports System.Net
'Imports System.Text.RegularExpressions
'Imports CsvHelper
'Imports CsvHelper.Configuration

'Namespace MpkMain


'    Public Interface IMPKMain
'        Property errMessage As String

'        Function DownloadListaPrzystankowAsync(Optional bTram As Boolean = True, Optional bBus As Boolean = True, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of MpkPrzystanek))

'        ''' <summary>
'        ''' wczytaj tabliczkê, lub NULL, gdy nieudane
'        ''' </summary>
'        Function WczytajTabliczkeAsync(isBus As Boolean, sId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkTabliczka)


'        Function DownloadDalszaTrasaAsync(isBus As Boolean, tripId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkDalszaTrasa)

'        Function DownloadRouteTrasaAsync(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkRouteStops)

'        Function DownloadVehiclesState(isBus As Boolean, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkVehiclesState)


'        ''' <summary>
'        ''' wczytaj trasê do pokazywania na mapie, wedle pojazdu (id zwykle jest ujemny)
'        ''' </summary>
'        Function DownloadTrasaNaMapieVehicle(isBus As Boolean, vehicleId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo)

'        ''' <summary>
'        ''' wczytaj trasê do pokazywania na mapie, wedle routeId
'        ''' </summary>
'        Function DownloadTrasaNaMapieRoute(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo)

'        ''' <summary>
'        ''' wczytaj listê przystanków trasy konkretnej linii (wywo³ywaæ z LR0, nie 990)
'        ''' </summary>
'        ''' <returns>NULL w razie b³êdu (i wtedy zobacz errMessage)</returns>
'        Function DownloadTrasaLiniiAsync(linia As String, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of String))


'        ''' <summary>
'        ''' Wczytaj listê zmian w trasach
'        ''' </summary>
'        ''' <returns>NULL w razie b³êdu (i wtedy zobacz errMessage)</returns>
'        Function DownloadZmianyAsync(Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of MpkZmiana))

'    End Interface


'End Namespace
