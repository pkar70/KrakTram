
Imports System.Linq ' bez tego nie ma .Where

Public Class PrzystanekOpoznienie
    Inherits pkar.MpkWrap.Przystanek

    Public Property delays As pkar.MpkWrap.OpoznieniaStat

End Class


Public Class PrzystankiOpoznione

    Private _przystanki As List(Of PrzystanekOpoznienie)
    Private _oMPK As New pkar.MpkWrap.Tabliczka

    Public Function GetList() As List(Of PrzystanekOpoznienie)
        Return _przystanki
    End Function

    Public Sub New(przystanki As List(Of pkar.MpkWrap.Przystanek))

        _przystanki = New List(Of PrzystanekOpoznienie)
        For Each oItem As pkar.MpkWrap.Przystanek In przystanki
            Dim oNew As New PrzystanekOpoznienie
            oItem.CopyTo(oNew)
            _przystanki.Add(oNew)
        Next

    End Sub

    ''' <summary>
    ''' ret="" gdy Error, see sLastError; ret=string - lista opoznien
    ''' </summary>
    Public Async Function OpoznieniaFromHttpAsync(isBus As Boolean) As Task(Of String)
        ' z Opoznienia.Xaml.cs, 1 raz

        ' policz
        Dim sTxt = ""

        For Each oItem As PrzystanekOpoznienie In _przystanki
            ' wczytaj dane przystanku
            If oItem.IsBus <> isBus Then Continue For

            oItem.delays = Await _oMPK.GetDelayStats(isBus, oItem.id)

            sTxt = sTxt & "Przystanek: " & oItem.Name & vbTab & oItem.delays.DelaySum & vbCrLf

        Next

        Return sTxt
    End Function

    Public Function OpoznieniaGetStat(isBus As Boolean) As pkar.MpkWrap.OpoznieniaStat

        Dim delays As New pkar.MpkWrap.OpoznieniaStat
        Dim currList As List(Of PrzystanekOpoznienie) = _przystanki.Where(Function(s) s.IsBus = isBus)

        delays.itemsCount = currList.Count
        delays.noRealTimeCount = (From c In currList Select c.delays.noRealTimeCount).Sum
        delays.onTimeCount = (From c In currList Select c.delays.onTimeCount).Sum
        delays.DelayMin = (From c In currList Select c.delays.DelayMin).Min
        delays.DelayMax = (From c In currList Select c.delays.DelayMax).Max
        delays.DelayAvg = (From c In currList Select c.delays.DelayAvg).Average
        delays.DelaySum = (From c In currList Select c.delays.DelaySum).Sum
        delays.DelayMedian = (From c In currList Select c.delays.DelayMedian).Average

        Return delays
    End Function

End Class

