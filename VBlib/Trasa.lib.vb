
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
            If Not bNetAvail Then Return GetLangString("resErrorNoNetwork")
        End If

        If _nuget.Count < 1 Then Return "OK"

        moItemy.Clear()
        Dim iCnt As Integer = 0
        For Each stopek As String In _nuget.GetList
            moItemy.Add(New JedenStop With {.Przyst = stopek, .Num = iCnt})
            iCnt += 1
        Next

        ' policz który to numer przystanku
        Dim iStopNo = 0

        If msStop <> "" Then
            For Each oStop In moItemy
                Dim sPrzystName As String = NazwaBezSlupka(oStop.Przyst.ToLower())
                If sPrzystName = msStop.ToLower Then Exit For
                iStopNo = iStopNo + 1
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

#If NUGET Then

    ''' <summary>
    ''' ret="" nie ma w cache/za stare; "dd/mm/yyyy pliku
    ''' </summary>
    Private Function TryLoadTrasaCache() As String

        If Not IO.File.Exists(msDataFilePath) Then Return ""
        If IO.File.GetCreationTime(msDataFilePath).AddDays(MAX_CACHE_DAYS) < Date.Now Then Return ""  ' za stare

        Dim sTxt As String = System.IO.File.ReadAllText(msDataFilePath)

        Try
            moItemy = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObjectModel.Collection(Of JedenStop)))
        Catch ex As Exception
            sLastError = "ERROR deserializiong file?"
            Return ""
        End Try

        If moItemy.Count < 1 Then Return ""   ' co z tego ze jest plik jak nie ma trasy?

        Return IO.File.GetCreationTime(msDataFilePath).ToString("dd/MM/yyyy")
    End Function

    Private Sub Save()
        IO.File.Delete(msDataFilePath)         ' bez tego kasowania create timestamp jest stary!; nie ma Exc gdy nie ma pliku
        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(moItemy, Newtonsoft.Json.Formatting.Indented)
        System.IO.File.WriteAllText(msDataFilePath, sTxt)
    End Sub

    ''' <summary>
    ''' ret="" OK, ; <> "" error message
    ''' </summary>
    Private Async Function DownloadTrasa() As Task(Of String)

        Dim sPage = ""

        Dim oUri As Uri = New Uri("https://rozklady.mpk.krakow.pl/?lang=PL&linia=" & msLinia) ' http://rozklady.mpk.krakow.pl/?lang=PL&linia=50

        ' to nie działa - robi i tak redirect do błędnego Location - błąd po stronie serwera MPK
        'HttpPageSetAgent("KrakTram")
        'HttpPageReset(False)
        'sPage = Await HttpPageAsync(oUri)

        Using oHCH As Net.Http.HttpClientHandler = New Net.Http.HttpClientHandler()

            oHCH.AllowAutoRedirect = False

            Using oHttp As Net.Http.HttpClient = New Net.Http.HttpClient(oHCH)

                oHttp.Timeout = TimeSpan.FromSeconds(10)

                ' oHttp.DefaultRequestHeaders.Add("Referer", )
                Try
                    Dim oHttResp = Await oHttp.GetAsync(oUri)

                    If oHttResp.StatusCode = Net.HttpStatusCode.Found Then
                        oHttResp = Await oHttp.GetAsync(oHttResp.RequestMessage.RequestUri) ' tak, to jest == oUri!
                    End If
                    If oHttResp.IsSuccessStatusCode Then sPage = Await oHttResp.Content.ReadAsStringAsync()
                Catch
                    Return GetLangString("resErrorGetHttp")
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

        ' tak wycinam, bo nie ma żadnych ID ani nic takiego, co pozwoliłoby wyciąć
        Dim iInd As Integer
        iInd = sPage.IndexOf("Trasa:")
        If iInd < 10 Then Return "Bad file structure1"
        iInd = sPage.IndexOf("Przystanki", iInd)    ' samo przystanki jest trzy razy
        If iInd < 10 Then Return "Bad file structure2"
        sPage = sPage.Substring(iInd)
        iInd = sPage.IndexOf("<table")
        If iInd < 10 Then Return "Bad file structure3"
        If iInd > 250 Then Return "Bad file structure4"
        sPage = sPage.Substring(iInd)
        iInd = sPage.IndexOf("</table")
        If iInd < 10 Then Return "Bad file structure5"
        sPage = sPage.Substring(0, iInd) & "<table>"

        Dim oHtmlDoc As New HtmlAgilityPack.HtmlDocument()
        oHtmlDoc.LoadHtml(sPage)

        Dim entries As HtmlAgilityPack.HtmlNodeCollection = oHtmlDoc.DocumentNode.SelectNodes("//tr")
        If entries Is Nothing Then
            DumpMessage("Cos nie tak, nie widze przystankow")
            Return ""
        End If

        Dim iNum = 0

        moItemy.Clear()

        For Each entry As HtmlAgilityPack.HtmlNode In entries
            Dim oNew As New JedenStop
            oNew.Linia = msLinia
            oNew.Przyst = entry.SelectSingleNode(".//td").InnerText.Trim
            oNew.iMin = 0   ' ewentualnie pozniej liczyc czas przejazdu
            'oNew.sMin = ""
            oNew.Num = iNum
            iNum += 1


            moItemy.Add(oNew)
        Next

        Save()
        Return ""
    End Function
#End If

End Class

