Imports pkar.MpkWrap

Namespace MpkMain

    Public MustInherit Class MPK_Common

        Public Shared Property errMessage As String = ""

        Protected Shared oHttp As New Net.Http.HttpClient()

        Protected Shared Sub SetHttpTimeout(iTimeoutSecs As Integer)
            If oHttp.Timeout.TotalSeconds = iTimeoutSecs Then Return
            oHttp = New Net.Http.HttpClient
            oHttp.Timeout = TimeSpan.FromSeconds(iTimeoutSecs)
        End Sub

    End Class


    Public Class MPK_Merged
        Inherits MPK_Common

        ' skleja razem różne MpkMain - dane z różnych źródeł

        ''' <summary>
        ''' Wczytaj listę zmian w trasach
        ''' </summary>
        ''' <returns>NULL w razie błędu (i wtedy zobacz errMessage)</returns>
        Public Shared Async Function DownloadZmianyAsync(Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of MpkZmiana))
            Return Await MpkMain_Ztp.DownloadZmianyAsync(iTimeoutSecs)
        End Function

        ' wczytanie trasy linii
        Public Shared Async Function DownloadListaPrzystankowAsync(Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of Przystanek))
            Return Await MPK_Amistad.DownloadListaPrzystankowAsync(iTimeoutSecs)
        End Function

        Public Shared Async Function WczytajTabliczkeAsync(oPrzyst As Przystanek, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkTabliczka)
            Return Await Mpk_Zbiorkom.WczytajTabliczkeAsync(oPrzyst, iTimeoutSecs)
        End Function

        Public Shared Async Function WczytajTabliczkeAsync(isBus As Boolean, sId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkTabliczka)
            Return Await Mpk_Zbiorkom.WczytajTabliczkeAsync(isBus, sId, iTimeoutSecs)
        End Function

        Public Shared Async Function DownloadDalszaTrasaAsync(isBus As Boolean, vehicleId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkDalszaTrasa)
            Return Await Mpk_Zbiorkom.DownloadDalszaTrasaAsync(isBus, vehicleId, iTimeoutSecs)
        End Function

        ''' <summary>
        ''' wczytaj listę przystanków trasy konkretnej linii (wywoływać z LR0, nie 990)
        ''' </summary>
        ''' <returns>NULL w razie błędu (i wtedy zobacz errMessage)</returns>
        Public Shared Async Function DownloadTrasaLiniiAsync(linia As String, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of String))
            Return Await MPK_Amistad.DownloadTrasaLiniiAsync(linia, iTimeoutSecs)
        End Function

        'Public Shared Async Function DownloadRouteTrasaAsync(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkRouteStops)

        'End Function

        'Public Shared Async Function DownloadVehiclesState(isBus As Boolean, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkVehiclesState)

        'End Function


        '''' <summary>
        '''' wczytaj trasę do pokazywania na mapie, wedle pojazdu (id zwykle jest ujemny)
        '''' </summary>
        'Public Shared Async Function DownloadTrasaNaMapieVehicle(isBus As Boolean, vehicleId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo)

        'End Function

        '''' <summary>
        '''' wczytaj trasę do pokazywania na mapie, wedle routeId
        '''' </summary>
        'Public Shared Async Function DownloadTrasaNaMapieRoute(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo)
        'End Function


    End Class

End Namespace