Imports System.Net.Http.Headers
Imports System.Reflection
Imports System.Security.Cryptography.X509Certificates
Imports Newtonsoft
Imports Newtonsoft.Json.Linq
Imports pkar.DotNetExtensions
Imports pkar.MpkMain
Imports pkar.MpkWrap

Public Class Mpk_Zbiorkom
    Inherits MPK_Common

    Private Const URI_BASE As String = "https://api.zbiorkom.live/4.8/krakow/"

    Public Shared Async Function WczytajTabliczkeAsync(oPrzyst As Przystanek, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkTabliczka)
        Debug.WriteLine("Zbieram tabliczki dla przystanku " & oPrzyst.Name)
        ' wersja Zbiorkom posługuje się tylko nazwami, iLp iteruje kolejne przystanki

        ' "https://api.zbiorkom.live/4.8/krakow/stops/getDepartures?id=dauna01"
        ' ściągnie 20 odjazdów, można tym sterować przez parametr
        Dim oRet As New MpkTabliczka With {.stopName = oPrzyst.Name, .actual = New List(Of MpkOdjazd)}

        Dim stopname As String = oPrzyst.Name.ToLower.Depolit.Replace(" ", "-")

        Dim iMaxCnt As Integer = Math.Max(1, oPrzyst.Ami_Count)

        ' kolejne słupki
        For iLp As Integer = 1 To 15
            Dim oneTabl As MpkTabliczka = Await WczytajTabliczkeAsync(True, stopname & iLp.ToString("00"))
            ' ale tej tabliczki nie ma, np. Politechnika nie ma 05
            If oneTabl Is Nothing Then Continue For

            oRet.actual = oRet.actual.Concat(oneTabl.actual).ToList '- to nie działa??

            iMaxCnt -= 1
            If iMaxCnt < 1 Then Exit For
        Next

        Return oRet

    End Function

    Public Shared Async Function WczytajTabliczkeAsync(isBus As Boolean, sId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkTabliczka)
        Debug.WriteLine(" tabliczka dla słupka " & sId)

        Dim urik As String = URI_BASE & "stops/getDepartures?id=" & sId
        Dim sPage As String

        Try
            sPage = Await oHttp.GetStringAsync(urik)
        Catch ex As Exception
            Debug.WriteLine("nieistniejący słupek")
            Return Nothing
        End Try

        Dim root As JArray = JArray.Parse(sPage)
        Dim departures As JToken = root(1)

        Dim oRet As New MpkTabliczka With {.stopName = sId, .actual = New List(Of MpkOdjazd)}

        For Each dep As JToken In departures

            Dim lineInfo As JToken = dep(2)
            Dim times As JToken = dep(7)

            Dim kiedyPlan As DateTime = UnixToDateTime(CLng(times(0)))
            Dim kiedyReal As DateTime = UnixToDateTime(CLng(times(1)))

            Dim oNew As New MpkOdjazd With {
                    .direction = dep(1), ', As String ' "SALWATOR"
                    .patternText = lineInfo(0),
                    .vehicleId = dep(5).ToString.Replace("0/", ""),
                    .plannedTime = kiedyPlan.ToString("HH:mm"),
                    .actualTime = kiedyReal.ToString("HH:mm"),
                    .actualRelativeTime = (kiedyReal - Date.Now).TotalSeconds
            }

            ' niby times(2) to opoznienie, ale jest "live" (z cudzysłowami) albo liczba - różnica między (0) iLp (2)
            Dim nibyDelay As JToken = times(2)
            oNew.status = "PLANNED"
            If nibyDelay.Type = JTokenType.Integer Then oNew.status = "PREDICTED"
            If nibyDelay.Type = JTokenType.String Then
                If CStr(nibyDelay) = "live" Then
                    oNew.status = "PREDICTED"
                End If
            End If

            If oNew.status = "PREDICTED" Then
                oNew.mixedTime = (oNew.actualRelativeTime / 60).Floor & " min"
            Else
                oNew.mixedTime = oNew.plannedTime
            End If

            oRet.actual.Add(oNew)
        Next


        Debug.WriteLine($"Mam {oRet.actual.Count} odjazdów")
        Return oRet

    End Function

    Public Shared Async Function DownloadDalszaTrasaAsync(isBus As Boolean, vehicleId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkDalszaTrasa)
        Dim sPage As String = ""

        Try
            sPage = Await oHttp.GetStringAsync(UrikDlaTrasy(isBus, vehicleId))
        Catch ex As Exception
        End Try

        If sPage = "" Then
            Try
                sPage = Await oHttp.GetStringAsync(UrikDlaTrasy(Not isBus, vehicleId))
            Catch ex As Exception
            End Try
        End If

        If sPage = "" Then Return Nothing

        'Public Function ParsePlannedArrivals(json As String) As List(Of (StopName As String, Time As String))
        Dim root = JObject.Parse(sPage)


        Dim currStop As Integer = CInt(root("sequence")) ' sequence
        Dim tripStops = root("trip")(6)      ' lista przystanków
        Dim stopTimes = root("stops")        ' czasy dla przystanków

        ' .routeName będzie jak dla GTFS
        Dim oRet As New MpkDalszaTrasa With {.directionText = root("trip")(2), .routeName = root("trip")(0)}
        oRet.actual = New List(Of MpkDalszyStop)
        ' Wrap.DalszaTrip używa tylko tej listy .actual, żadnego innego pola nie trzeba

        For iLp = 0 To tripStops.Count - 1
            Dim stopInfo = tripStops(iLp)
            Dim czasy = stopTimes(iLp)

            ' Wrap.DalszaTrip używa tylko .name i .shortname, id jest zbędny
            ' GUI uzywa tylko .name do pokazania listy, .shortname do szukania na liscie przystanków
            Dim oNewStop As New MpkDalszyStop_Stop With {
                        .id = stopInfo(0),      ' "dworzec-glowny-wschod01",
                        .name = stopInfo(1), ' ze słupkiem, "Dworzec Główny Wschód 01",
                        .shortName = stopInfo(1)}

            ' Wrap.DalszaTrip używa wszystkiego poza stopseq
            ' GUI używa stop.name oraz .actual, plus stop.shortname
            Dim oNew As New MpkDalszyStop With {
            .[stop] = oNewStop,
            .stop_seq_num = iLp - currStop,
            .status = ""
            }

            Dim arrival = czasy(0)           ' arrival array
            Dim planned As Long = CLng(arrival(0))   ' planned arrival timestamp (ms)
            Dim dt = DateTimeOffset.FromUnixTimeMilliseconds(planned).ToLocalTime().DateTime
            oNew.actualTime = dt.ToString("HH:mm")


            oRet.actual.Add(oNew)
        Next

        Return oRet
    End Function

    Public Async Function DownloadTrasaNaMapieVehicle(isBus As Boolean, vehicleId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo)
        Dim sPage As String = Await oHttp.GetStringAsync(UrikDlaTrasy(isBus, vehicleId))
        Dim root As JArray = JArray.Parse(sPage)

        Dim currStop As Integer = CInt(root("sequence")) ' sequence
        Dim tripStops = root("trip")(6)      ' lista przystanków

        Dim oRet As New MpkPathInfo With {.paths = New List(Of MpkPath)}
        '            Public Property color As String - kopiowany
        'Public Property wayPoints As List(Of MpkWayPoint)

        'Public Property lat As Integer
        'Public Property lon As Integer
        'Public Property seq As String - wg tego sort

        ' na razie nie robie, bo nie wykorzytuję w GUI

        For i = currStop To tripStops.Count - 1
            Dim stopInfo = tripStops(i)
            Dim geo = stopInfo(2)
        Next


    End Function

    Private Shared Function UrikDlaTrasy(isBus As Boolean, vehicleId As String) As String
        Dim urik As String = URI_BASE & "trips/getTripByVehicle?vehicle=" '"3%2FMM847"
        urik &= If(isBus, "3", "0")
        urik &= "/" & vehicleId
        Return urik
    End Function

    Private Shared Function UnixToDateTime(ms As Long) As DateTime
        Dim epoch = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        Return epoch.AddMilliseconds(ms).ToLocalTime()
    End Function


    Private Shared Function ParseDelay(token As JToken) As Integer
        If token.Type = JTokenType.String AndAlso token.ToString() = "live" Then
            Return 0
        End If
        Return CInt(token)
    End Function




End Class
