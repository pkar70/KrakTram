﻿Namespace MpkWrap

    ''' <summary>
    ''' wrapper dla MPKinternal, format danych: mój
    ''' </summary>

    Public Class Linia
        Inherits pkar.BaseList(Of String)

        Private _maxDays As Integer = 30
        Private Shared oMpk As New MpkMain.MPK
        Private _linia As String = ""


        Public Sub New(cacheFolder As String, sLinia As String, Optional maxDaysCache As Integer = 30)
            MyBase.New(cacheFolder, "line" & sLinia & ".json")
            _maxDays = maxDaysCache
            _linia = sLinia
        End Sub

        ''' <summary>
        ''' kasuje numer słupka na końcu nazwy przystanku
        ''' </summary>
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
        ''' ret="OKxxx" OK, z datą pliku cache (np. teraz ściągniętego);  else error message
        ''' </summary>
        Public Async Function LoadOrImport(bForceLoad As Boolean, bNetAvail As Boolean) As Task(Of String)

            If IsObsolete(_maxDays) Then bForceLoad = True

            Dim bReaded = False
            If Not bForceLoad Then bReaded = Load()  ' True gdy udane wczytanie; nie ma sensu czytac gdy wymuszamy import

            If bReaded AndAlso Count() < 1 Then bReaded = False      ' jak jest puste w ogole

            If bReaded Then
                If Not bForceLoad OrElse Not bNetAvail Then
                    Return "OK" & GetFileDate.ToString("yyyy.MM.dd")
                End If
            End If

            If Not bNetAvail Then Return "resErrorNoNetwork"

            _lista = Await oMpk.DownloadTrasaLiniiAsync(_linia)
            If _lista Is Nothing Then Return "emptyLinia"

            Save()

            Return "OK" & GetFileDate.ToString("yyyy.MM.dd")

        End Function

    End Class

End Namespace
