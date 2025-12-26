Imports System.Globalization
Imports pkar.MpkMain

Namespace MpkWrap

    ''' <summary>
    ''' wrapper dla MPKinternal, format danych: mój
    ''' </summary>

    Public Class Tabliczka

        ''' <summary>
        ''' statystyka ostatniej tabliczki
        ''' </summary>
        Public Property LastStat As OpoznieniaStat

        Private Shared oMpk As New MpkMain.MPK_Merged

        ''' <summary>
        ''' wczytuj tabliczkę, konwertując na mój format
        ''' </summary>
        ''' <param name="iMinSec">czas dotarcia do przystanku - wcześniejszych odjazdów nie podawaj</param>
        ''' <returns></returns>
        Public Async Function WczytajTabliczke(oPrzyst As Przystanek, iMinSec As Integer, Optional HttpTimeoutSecs As Integer = 10) As Task(Of List(Of Odjazd))
            '' <param name="isBus">false: tram, true: bus</param>
            '' <param name="sId">identyfikator (shortName z listy przystanków)</param>

            Dim tabliczka As MpkTabliczka = Await oMpk.WczytajTabliczkeAsync(oPrzyst, HttpTimeoutSecs)
            If tabliczka Is Nothing Then Return Nothing

            Dim lista As New List(Of Odjazd)

            For Each entry As MpkOdjazd In tabliczka.actual

                If entry.actualRelativeTime < iMinSec Then Continue For

                Dim oNew As New Odjazd

                oNew.Linia = entry.patternText

                Dim argresult = 0

                If Integer.TryParse(oNew.Linia, argresult) Then
                    oNew.iLinia = argresult
                Else
                    oNew.iLinia = 9999
                End If  ' trafia na koniec

                ' 2024.0.04
                If oNew.iLinia > 989 AndAlso oNew.iLinia < 1000 Then
                    oNew.Linia = "LR" & oNew.iLinia - 990
                End If

                oNew.Kier = entry.direction
                If String.IsNullOrEmpty(oNew.Kier) Then oNew.Kier = "!error!"

                oNew.Mins = entry.mixedTime
                If String.IsNullOrEmpty(oNew.Mins) Then oNew.Mins = "!ERR!"
                oNew.Mins = oNew.Mins.Replace("%UNIT_MIN%", "min").Replace("Min", "min")

                oNew.PlanTime = entry.plannedTime
                If String.IsNullOrEmpty(oNew.PlanTime) Then oNew.PlanTime = "!ERR!"
                oNew.PlanTime = "Plan: " & oNew.PlanTime

                oNew.ActTime = entry.actualTime
                If String.IsNullOrEmpty(oNew.ActTime) Then oNew.ActTime = "!ERR!"
                oNew.ActTime = "Real: " & oNew.ActTime

                oNew.tripId = entry.tripId
                oNew.vehicleId = entry.vehicleId

                oNew.Przyst = tabliczka.stopName
                If String.IsNullOrEmpty(oNew.Przyst) Then oNew.Przyst = "!error!"

                oNew.TimeSec = entry.actualRelativeTime
                oNew.odlMin = iMinSec / 60

                oNew.sRawData = entry.DumpAsJSON(True).Replace(",""", "," & vbLf & """")

                lista.Add(oNew)
            Next


            PoliczStatystyke(tabliczka.actual)

            Return lista
        End Function

        Public Async Function GetDelayStats(isBus As Boolean, sId As String) As Task(Of OpoznieniaStat)
            'Await WczytajTabliczke(isBus, sId, 0)
            ' wersja uproszczona - stat z ostatniego czytania tabliczki
            Return LastStat
        End Function

        Private Sub PoliczStatystyke(lista As List(Of MpkOdjazd))
            LastStat = New OpoznieniaStat

            ' robimy to w Try, żeby nie zepsuć zwykłego pokazywania tabliczki "jakbyco"
            Try

                LastStat.itemsCount = lista.Count
                LastStat.noRealTimeCount = (From c In lista Where c.actualTime = "").Count
                LastStat.onTimeCount = (From c In lista Where c.plannedTime = c.actualTime).Count

                Dim minuty As New List(Of Integer)
                For Each oItem As MpkOdjazd In From c In lista Where c.actualTime <> ""
                    Dim plan As TimeSpan = TimeSpan.ParseExact(oItem.plannedTime, "hh\:mm", CultureInfo.InvariantCulture)
                    Dim act As TimeSpan = TimeSpan.ParseExact(oItem.actualTime, "hh\:mm", CultureInfo.InvariantCulture)
                    minuty.Add(Math.Max(0, (act - plan).TotalMinutes))
                Next

                If minuty.Count > 0 Then
                    LastStat.DelayMin = minuty.Min
                    LastStat.DelayMax = minuty.Max
                    LastStat.DelayAvg = minuty.Average
                    LastStat.DelaySum = minuty.Sum
                End If

                Dim numberCount As Integer = minuty.Count
                Dim halfIndex As Integer = minuty.Count \ 2
                Dim sortedNumbers = minuty.OrderBy(Function(n) n)
                Dim median As Double
                If (numberCount Mod 2 = 0) Then
                    median = (sortedNumbers.ElementAt(halfIndex) + sortedNumbers.ElementAt(halfIndex - 1)) / 2
                Else
                    median = sortedNumbers.ElementAt(halfIndex)
                End If

                LastStat.DelayMedian = median

            Catch ex As Exception

            End Try

        End Sub

    End Class


    Public Class Odjazd
        Inherits pkar.BaseStruct

        Public Property Linia As String
        ''' <summary>
        ''' wersja integer, do sortowania. 9999 gdy nie da się przetworzyć na integer.
        ''' </summary>
        Public Property iLinia As Integer
        Public Property Kier As String
        Public Property Przyst As String
        Public Property Mins As String
        Public Property PlanTime As String
        Public Property ActTime As String
        Public Property tripId As String

        Public Property vehicleId As String
        Public Property TimeSec As Integer
        ''' <summary>
        ''' przepisany argument iMinSec z wywołania WczytajTabliczke (odległość od przystanku) - po zmianie z sekund na minuty
        ''' </summary>
        Public Property odlMin As Integer
        ''' <summary>
        ''' JSON oryginału
        ''' </summary>
        Public Property sRawData As String

    End Class


    Public Class OpoznieniaStat
        Inherits pkar.BaseStruct

        Public Property itemsCount As Integer
        Public Property noRealTimeCount As Integer
        Public Property onTimeCount As Integer
        Public Property DelayMin As Integer
        Public Property DelayMax As Integer
        Public Property DelayAvg As Integer
        Public Property DelaySum As Integer
        Public Property DelayMedian As Integer

    End Class



End Namespace
