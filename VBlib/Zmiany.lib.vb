#If PRENUGET Then
Public Class JednaInfo
    Public Property sLinie As String
    Public Property sCzas As String
    Public Property sTytul As String
    Public Property sInfo As String
    Public Property sLink As String

End Class

Public Class Zmiany

    Public moItemy As New ObjectModel.Collection(Of JednaInfo)
    Private ReadOnly msDataFilePath As String = ""
    Private Const MAX_CACHE_DAYS As Integer = 14

    Public sLastError As String = ""

    Public Sub New(sRootPath As String)
        ' Windows.Storage.ApplicationData.Current.LocalCacheFolder 
        msDataFilePath = System.IO.Path.Combine(sRootPath, "zmiany.json")
    End Sub

    ''' <summary>
    ''' ret="" nie ma w cache/za stare; "dd/mm/yyyy pliku
    ''' </summary>
    Public Function TryLoadCache() As String

        If Not IO.File.Exists(msDataFilePath) Then Return ""
        If IO.File.GetCreationTime(msDataFilePath).AddDays(MAX_CACHE_DAYS) < Date.Now Then Return ""  ' za stare

        Dim sTxt As String = System.IO.File.ReadAllText(msDataFilePath)

        Try
            moItemy = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObjectModel.Collection(Of JednaInfo)))
        Catch ex As Exception
            sLastError = "ERROR deserializiong file?"
            Return ""
        End Try

        Return IO.File.GetCreationTime(msDataFilePath).ToString("dd/MM/yyyy")
    End Function

    Public Sub Save()
        IO.File.Delete(msDataFilePath)         ' bez tego kasowania create timestamp jest stary!; nie ma Exc gdy nie ma pliku
        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(moItemy, Newtonsoft.Json.Formatting.Indented)
        System.IO.File.WriteAllText(msDataFilePath, sTxt)
    End Sub

    Public Async Function WczytajZmiany() As Task(Of String)
        DumpCurrMethod()

        Dim sPage = ""
        Dim bError = False

        ' 2022.08
        ' nie działa od jakiegoś czasu, bo zmiany na stronie

        ' można byłoby przejść na RSS, ale... System.ServiceModel.Syndication jest dopiero od .Net 2 :(
        ' https://ztp.krakow.pl/feed?post_type=komunikat
        ' oraz w tym nie ma linii

        ' przechodzę na HtmlAgilityPack i zjadanie strony

        ' własny HTTP, bo robię 10 sekund
        Using oHttp As New Net.Http.HttpClient
            oHttp.Timeout = TimeSpan.FromSeconds(10)
            Try
                ' Android: "Ssl error:1000007d:SSL routines:OPENSSL_internal:CERTIFICATE_VERIFY_FAILED\n  at /Users/builder/jenki…"
                sPage = Await oHttp.GetStringAsync(New Uri("https://ztp.krakow.pl/transport-publiczny/komunikacja-miejska/komunikaty"))
            Catch
                Return GetLangString("resErrorGetHttp")
            End Try
        End Using


        moItemy.Clear()

        Dim oHtmlDoc As New HtmlAgilityPack.HtmlDocument()
        oHtmlDoc.LoadHtml(sPage)

        ' iterujemy <div class="card">

        Dim entries As HtmlAgilityPack.HtmlNodeCollection = oHtmlDoc.DocumentNode.SelectNodes("//div[@class='card']")
        If entries Is Nothing Then
            DumpMessage("Cos nie tak, nie powinno byc null - jakies komunikaty jednak powinny być")
            Return ""
        End If

        For Each entry As HtmlAgilityPack.HtmlNode In entries
            Dim oNew As New JednaInfo()

            ' UWAGA! "." jest istotna, bez niej idzie od DocumentRoot a nie wewnątrz entry!
            oNew.sCzas = entry.SelectSingleNode(".//div[@class='date']").InnerText.Trim
            oNew.sLinie = entry.SelectSingleNode(".//div[@class='lines']").InnerText.Trim
            oNew.sTytul = entry.SelectSingleNode(".//div[@class='message-title']").InnerText.Trim
            oNew.sInfo = entry.SelectSingleNode(".//div[@class='card-body-inner']/div").InnerText.Trim
            oNew.sLink = entry.SelectSingleNode(".//a").Attributes("href").Value

            moItemy.Add(oNew)
        Next

        Return ""
    End Function

End Class
#end if