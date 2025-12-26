
Public Class JedenStop ' musi byc public, bo inaczej serializer nie dziala
    'Public Property Linia As String
    Public Property Przyst As String
    Public Property iMin As Integer
    Public Property Num As Integer
End Class

Public Class Trasa

    'Private ReadOnly msLinia As String = ""
    Private ReadOnly msKier As String = ""
    Private ReadOnly msStop As String = ""

    Public moItemy As New List(Of JedenStop)
    Private ReadOnly msDataFilePath As String = ""
    Private Const MAX_CACHE_DAYS As Integer = 30
    Public sLastError As String = ""

    Private _nuget As pkar.MpkWrap.Linia

    Public Sub New(sRootPath As String, sLinia As String, sKier As String, sStop As String)
        ' Windows.Storage.ApplicationData.Current.LocalCacheFolder 
        _nuget = New pkar.MpkWrap.Linia(sRootPath, sLinia)
        ' msLinia = sLinia
        msKier = sKier
        msStop = sStop.Replace("(nż)", "").Trim
    End Sub


    Public Shared Function NazwaBezSlupka(sNazwaZeSlupkiem As String) As String
        Dim iInd As Integer = sNazwaZeSlupkiem.LastIndexOf(" ")
        Dim iTest As Integer = 0
        If iInd > 1 AndAlso Integer.TryParse(sNazwaZeSlupkiem.Substring(iInd).Trim, iTest) Then
            ' zapewne będzie to wstrętny numer słupka - to go nie nie chcemy
            sNazwaZeSlupkiem = sNazwaZeSlupkiem.Substring(0, iInd).Trim
        End If
        Return sNazwaZeSlupkiem
    End Function

    ''' <summary>
    ''' ret="OKxxx" OK, z datą pliku cache; else error message
    ''' </summary>
    Public Async Function PrepareTrasa(bForceRefresh As Boolean, bNetAvail As Boolean) As Task(Of String)

        Dim ret As String = Await _nuget.LoadOrImport(bForceRefresh, bNetAvail)
        If Not ret.StartsWith("OK") Then
            If Not bNetAvail Then Return pkar.Localize.GetResManString("resErrorNoNetwork")
        End If

        If _nuget.Count < 1 Then Return "OK"

        moItemy.Clear()
        Dim iCnt As Integer = 0
        For Each stopek As String In _nuget
            moItemy.Add(New JedenStop With {.Przyst = stopek, .Num = iCnt})
            iCnt += 1
        Next

        ' policz który to numer przystanku
        Dim iStopNo = 0

        If msStop <> "" Then
            For Each oStop In moItemy
                Dim sPrzystName As String = NazwaBezSlupka(oStop.Przyst.ToLower())
                If sPrzystName = msStop.ToLower Then Exit For
                iStopNo += 1
            Next
        End If

        ' dopisanie numeru przystanku
        For Each oStop In moItemy

            ' starts, bo msKier nie ma numeru słupka a Przyst - ma
            If moItemy.Item(0).Przyst.StartsWith(msKier) Then
                ' iNum od 0 .. iStopNo .. max -> iMin= -iCount+iStopNo .. 0 .. iCount-iStopNo
                oStop.iMin = moItemy.Count - oStop.Num - (moItemy.Count - iStopNo)
            Else
                ' iNum od 0 .. iStopNo .. max -> iMin= -iStopNo .. 0 .. iCount-iStopNo
                oStop.iMin = oStop.Num - iStopNo
            End If

            'oStop.sMin = oStop.iMin.ToString()
        Next

        Return ret
    End Function


End Class

