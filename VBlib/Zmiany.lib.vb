
Public Class JednaInfo
    Public Property sLinie As String
    Public Property sCzas As String
    Public Property sTytul As String
    Public Property sInfo As String
End Class

Public Class Zmiany

    Public moItemy As ObjectModel.Collection(Of JednaInfo)
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

        Dim sPage = ""
        Dim bError = False

        Using oHttp As Net.Http.HttpClient = New Net.Http.HttpClient()
            oHttp.Timeout = TimeSpan.FromSeconds(10)
            Try
                sPage = Await oHttp.GetStringAsync(New Uri("http://kmkrakow.pl/"))
            Catch
                Return GetLangString("resErrorGetHttp")
            End Try
        End Using


        moItemy = New ObjectModel.Collection(Of JednaInfo)()
        Dim iInd As Integer
        iInd = sPage.IndexOf("<div class=""linie")

        While iInd > 0
            Dim oNew = New JednaInfo()
            sPage = sPage.Substring(iInd)
            iInd = sPage.IndexOf("</")
            oNew.sLinie = RemoveHtmlTags(sPage.Substring(0, iInd))
            iInd = sPage.IndexOf("<div class=""przedz")
            sPage = sPage.Substring(iInd)
            iInd = sPage.IndexOf("</")
            oNew.sCzas = RemoveHtmlTags(sPage.Substring(0, iInd))
            iInd = sPage.IndexOf("<h2 class=""tyt")
            sPage = sPage.Substring(iInd)
            iInd = sPage.IndexOf("</")
            oNew.sTytul = RemoveHtmlTags(sPage.Substring(0, iInd))
            iInd = sPage.IndexOf("<div class=""hide")
            sPage = sPage.Substring(iInd)
            iInd = sPage.IndexOf("</div")
            oNew.sInfo = sPage.Substring(0, iInd) & "</div>"
            moItemy.Add(oNew)
            iInd = sPage.IndexOf("<div class=""linie")
        End While

        Return ""
    End Function

End Class
