Imports System.Net

Namespace MpkMain

    Friend Class MpkMain_Ztp
        Inherits MPK_Common
        ''' <summary>
        ''' Wczytaj listę zmian w trasach
        ''' </summary>
        ''' <returns>NULL w razie błędu (i wtedy zobacz errMessage)</returns>
        Public Shared Async Function DownloadZmianyAsync(Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of MpkZmiana))

            Dim sPage As String
            errMessage = ""
            SetHttpTimeout(iTimeoutSecs)

            ' można byłoby przejść na RSS, ale... System.ServiceModel.Syndication jest dopiero od .Net 2 :(
            ' https://ztp.krakow.pl/feed?post_type=komunikat
            ' oraz w tym nie ma linii

            ' przechodzę na HtmlAgilityPack i zjadanie strony

            Debug.WriteLine("wczytuje")

            Try
                ' Android: "Ssl error:1000007d:SSL routines:OPENSSL_internal:CERTIFICATE_VERIFY_FAILED\n  at /Users/builder/jenki…"
                sPage = Await oHttp.GetStringAsync(New Uri("https://ztp.krakow.pl/transport-publiczny/komunikacja-miejska/komunikaty"))
            Catch
                errMessage = "resErrorGetHttp"
                Return Nothing
            End Try

            Debug.WriteLine("wczytane jako string")

            Dim moItemy As New List(Of MpkZmiana)

            Dim oHtmlDoc As New HtmlAgilityPack.HtmlDocument()
            oHtmlDoc.LoadHtml(sPage)

            ' iterujemy <div class="card">

            Debug.WriteLine("wczytane jako HTML")

            Dim entries As HtmlAgilityPack.HtmlNodeCollection = oHtmlDoc.DocumentNode.SelectNodes("//div[@class='card']")
            If entries Is Nothing Then
                errMessage = "Cos nie tak, nie powinno byc null - jakies komunikaty jednak powinny być"
                Return Nothing
            End If

            For Each entry As HtmlAgilityPack.HtmlNode In entries
                Dim oNew As New MpkZmiana

                ' UWAGA! "." jest istotna, bez niej idzie od DocumentRoot a nie wewnątrz entry!
                oNew.sCzas = entry.SelectSingleNode(".//div[@class='date']").InnerText.Trim
                oNew.sLinie = entry.SelectSingleNode(".//div[@class='lines']").InnerText.Trim
                oNew.sTytul = entry.SelectSingleNode(".//div[@class='message-title']").InnerText.Trim
                oNew.sInfo = entry.SelectSingleNode(".//a[@class='message-btn']").InnerText.Trim
                oNew.sLink = entry.SelectSingleNode(".//a").Attributes("href").Value

                moItemy.Add(oNew)
            Next

            Return moItemy
        End Function


    End Class

End Namespace
