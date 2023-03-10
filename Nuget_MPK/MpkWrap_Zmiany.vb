Namespace MpkWrap

    ''' <summary>
    ''' wrapper dla MPKinternal, format danych: mój
    ''' </summary>


    Public Class Zmiany
        Inherits pkar.BaseList(Of MpkMain.MpkZmiana)

        Private _maxDays As Integer = 30


        Public Sub New(cacheFolder As String, Optional maxDaysCache As Integer = 7)
            MyBase.New(cacheFolder, "zmiany.json")
            _maxDays = maxDaysCache
        End Sub

        ''' <summary>
        ''' spróbuj wczytać dane, jeśli za stare - gdy netAvail to import
        ''' </summary>
        Public Async Function LoadOrImport(bForceLoad As Boolean, bNetAvail As Boolean) As Task(Of Boolean)

            If IsObsolete(_maxDays) Then bForceLoad = True

            Dim bReaded = False
            If Not bForceLoad Then bReaded = Load()  ' True gdy udane wczytanie; nie ma sensu czytac gdy wymuszamy import

            If bReaded AndAlso Count() < 1 Then bReaded = False      ' jak jest puste w ogole

            If bReaded Then
                If Not bForceLoad Then Return True
                If Not bNetAvail Then Return True
            End If

            If Not bNetAvail Then Return False

            Dim oMPK As New MpkMain.MPK
            Dim noweItemyMPK As List(Of MpkMain.MpkZmiana) = Await oMPK.DownloadZmianyAsync

            Save()

            Return True

        End Function
    End Class



End Namespace
