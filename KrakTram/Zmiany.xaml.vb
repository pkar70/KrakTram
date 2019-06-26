
Public Class JednaInfo
    Public Property sLinie As String
    Public Property sCzas As String
    Public Property sTytul As String
    Public Property sInfo As String
End Class


Public NotInheritable Class Zmiany
    Inherits Page

    Private moLista As Collection(Of JednaInfo)

    Const FILENAME As String = "zmiany.xml"

    Private Sub uiClose_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.GoBack()
    End Sub

    Private Async Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)
        uiReload.IsEnabled = False
        Await WczytajTrase()
        uiLista.ItemsSource = From c In moLista
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        If Not Await TryLoadCache() Then
            Dim dVal As Double
            dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
            uiProcesuje.Width = dVal
            uiProcesuje.Height = dVal
            uiProcesuje.Visibility = Visibility.Visible
            uiProcesuje.IsActive = True

            Await WczytajTrase()

            uiProcesuje.IsActive = False
            uiProcesuje.Visibility = Visibility.Collapsed
        End If

        If moLista.Count < 1 Then Exit Sub

        uiLista.ItemsSource = From c In moLista ' Order By c.iMin
        ' uiListStops.ItemsSource = moStopsy

    End Sub


    Private Async Function TryLoadCache() As Task(Of Boolean)
        Dim oObj As Windows.Storage.StorageFile
        oObj = Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(FILENAME)
        If oObj Is Nothing Then Return False

        Dim oFile As Windows.Storage.StorageFile = TryCast(oObj, Windows.Storage.StorageFile)

        If oFile.DateCreated.AddDays(14) < Date.Now Then Return False    ' za stare
        uiFileDate.Text = oFile.DateCreated.ToString("dd/MM/yyyy")

        Dim oSer As Xml.Serialization.XmlSerializer
        oSer = New Xml.Serialization.XmlSerializer(GetType(Collection(Of JednaInfo)))
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Try
            moLista = TryCast(oSer.Deserialize(oStream), Collection(Of JednaInfo))
        Catch ex As Exception
            Return False
        End Try

        uiReload.IsEnabled = True

        Return True

    End Function
    Private Async Function Save() As Task
        Dim oFile As Windows.Storage.StorageFile
        oFile = Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(FILENAME)
        If oFile IsNot Nothing Then Await oFile.DeleteAsync()
        ' bez tego kasowania create timestamp jest stary!

        oFile = Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync(
                FILENAME, Windows.Storage.CreationCollisionOption.ReplaceExisting)

        If oFile Is Nothing Then Exit Function

        Dim oSer As Xml.Serialization.XmlSerializer
        oSer = New Xml.Serialization.XmlSerializer(GetType(Collection(Of JednaInfo)))
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        oSer.Serialize(oStream, moLista)
        oStream.Dispose()   ' == fclose
    End Function

    Private Async Function WczytajTrase() As Task(Of Boolean)

        If Not Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
            Await App.DialogBoxRes("resErrorNoNetwork")
            Return ""
        End If

        Dim oHttp As Windows.Web.Http.HttpClient = New Windows.Web.Http.HttpClient

        Dim sPage As String = ""

        Dim bError As Boolean = False

        'oHttp.Timeout = TimeSpan.FromSeconds(8)
        ' timeout jest tylko w System.Net.http, ale tam nie działa ("The HTTP redirect request failed")

        Try
            sPage = Await oHttp.GetStringAsync(New Uri("http://kmkrakow.pl/"))
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            Await App.DialogBoxRes("resErrorGetHttp")
            Return ""
        End If

        moLista = New Collection(Of JednaInfo)

        Dim iInd As Integer
        iInd = sPage.IndexOf("<div class=""linie")
        While iInd > 0
            Dim oNew As JednaInfo = New JednaInfo

            sPage = sPage.Substring(iInd)
            iInd = sPage.IndexOf("</")
            oNew.sLinie = App.RemoveHtmlTags(sPage.Substring(0, iInd))

            iInd = sPage.IndexOf("<div class=""przedz")
            sPage = sPage.Substring(iInd)
            iInd = sPage.IndexOf("</")
            oNew.sCzas = App.RemoveHtmlTags(sPage.Substring(0, iInd))

            iInd = sPage.IndexOf("<h2 class=""tyt")
            sPage = sPage.Substring(iInd)
            iInd = sPage.IndexOf("</")
            oNew.sTytul = App.RemoveHtmlTags(sPage.Substring(0, iInd))

            iInd = sPage.IndexOf("<div class=""hide")
            sPage = sPage.Substring(iInd)
            iInd = sPage.IndexOf("</div")
            oNew.sInfo = sPage.Substring(0, iInd) & "</div>"

            moLista.Add(oNew)

            iInd = sPage.IndexOf("<div class=""linie")
        End While

        uiFileDate.Text = ""
        uiReload.IsEnabled = False
        Await Save()
        Return True

    End Function

    Private Sub uiPokaz_Click(sender As Object, e As TappedRoutedEventArgs)
        Dim oItem As JednaInfo = TryCast(sender, Grid).DataContext

        Dim sHtml As String = oItem.sInfo
        sHtml = "<html>
            <head><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
            <body>" & sHtml & "</body></html>"

        uiWebView.NavigateToString(sHtml)

    End Sub



    ' góra: webbrowser na zmianę (uiWebView)
    ' dół: <listview , lista zmian
    ' guzik refresh, oraz pokazywanie z kiedy ma cache - uiCacheDate.Text = "(" & data & ")"
End Class
