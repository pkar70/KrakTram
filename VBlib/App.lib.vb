
Public Class App

    Public Shared sLastError As String = ""


    Public Shared Async Function WczytajTabliczke(sCat As String, sErrData As String, iId As Integer) As Task(Of Newtonsoft.Json.Linq.JObject)
        Dim sUrl As String

        If Equals(If(sCat, ""), "bus") Then
            sUrl = "http://91.223.13.70"
        Else
            sUrl = "http://www.ttss.krakow.pl"
        End If

        sUrl = sUrl & "/internetservice/services/passageInfo/stopPassages/stop?mode=departure&stop="
        Dim sPage As String = Await WebPageAsync(sUrl & iId.ToString(), sErrData)
        If sPage = "" Then Return Nothing

        Dim oJson As Newtonsoft.Json.Linq.JObject

        Try
            oJson = Newtonsoft.Json.Linq.JObject.Parse(sPage)
        Catch
            sLastError = "ERROR: JSON parsing error - tablica in " & sErrData
            Return Nothing
        End Try

        Return oJson
    End Function

    Public Shared Async Function WebPageAsync(sUri As String, sErrData As String) As Task(Of String)

        ' string sTmp = "";

        'If Not Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
        '    p.k.DialogBoxRes("resErrorNoNetwork", sErrData)
        '    Return ""
        'End If

        Dim bError = False
        Dim sPage = ""


        Using oHttp As New Net.Http.HttpClient

            oHttp.Timeout = TimeSpan.FromSeconds(8)

            Try
                sPage = Await oHttp.GetStringAsync(New Uri(sUri))
            Catch
                bError = True
            End Try

            oHttp.Dispose()
        End Using

        If bError Then
            sLastError = GetLangString("resErrorGetHttp") & vbCrLf & sErrData
            Return ""
        End If

        Return sPage
    End Function

End Class

