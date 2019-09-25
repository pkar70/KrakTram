
Public NotInheritable Class Trasa
    Inherits Page

    Dim msLinia As String = ""
    Dim msKier As String = ""
    Dim msStop As String = ""

    Public Class JedenStop
        Public Property Linia As String
        Public Property Przyst As String
        Public Property iMin As Integer
        Public Property sMin As String
        Public Property Num As Integer
    End Class

    Private moStopsy As Collection(Of JedenStop)

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        Dim aParams As String() = e.Parameter.ToString.Split("|")
        If aParams.GetUpperBound(0) > -1 Then msLinia = aParams(0)
        If aParams.GetUpperBound(0) > 0 Then msKier = aParams(1)
        If aParams.GetUpperBound(0) > 1 Then msStop = aParams(2)
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiTitle.Text = GetLangString("resTrasa") & " " & msLinia
        If Not Await TryLoadTrasaCache(msLinia) Then
            Dim dVal As Double
            dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
            uiProcesuje.Width = dVal
            uiProcesuje.Height = dVal
            uiProcesuje.Visibility = Visibility.Visible
            uiProcesuje.IsActive = True

            Await WczytajTrase(msLinia)

            uiProcesuje.IsActive = False
            uiProcesuje.Visibility = Visibility.Collapsed
        End If

        If moStopsy.Count < 1 Then Exit Sub

        ' policz numer przystanku
        Dim iStopNo As Integer = 0
        If msStop <> "" Then
            For Each oStop As JedenStop In moStopsy
                If oStop.Przyst.ToLower = msStop.ToLower Then Exit For
                iStopNo = iStopNo + 1
            Next
        End If

        ' dopisanie numeru przystanku
        For Each oStop As JedenStop In moStopsy
            If moStopsy.Item(0).Przyst = msKier Then
                ' iNum od 0 .. iStopNo .. max -> iMin= -iCount+iStopNo .. 0 .. iCount-iStopNo
                oStop.iMin = moStopsy.Count - oStop.Num - (moStopsy.Count - iStopNo)
            Else
                ' iNum od 0 .. iStopNo .. max -> iMin= -iStopNo .. 0 .. iCount-iStopNo
                oStop.iMin = oStop.Num - iStopNo
            End If

            oStop.sMin = oStop.iMin.ToString
        Next

        uiListStops.ItemsSource = From c In moStopsy Order By c.iMin
        ' uiListStops.ItemsSource = moStopsy
    End Sub

    Private Async Function TryLoadTrasaCache(sLinia As String) As Task(Of Boolean)
        Dim oObj As Windows.Storage.StorageFile
        oObj = Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("line" & sLinia & ".xml")
        If oObj Is Nothing Then Return False

        Dim oFile As Windows.Storage.StorageFile = TryCast(oObj, Windows.Storage.StorageFile)

        If oFile.DateCreated.AddDays(30) < Date.Now Then Return False    ' za stare
        uiFileDate.Text = oFile.DateCreated.ToString("dd/MM/yyyy")

        Dim oSer As Xml.Serialization.XmlSerializer
        oSer = New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenStop)))
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Try
            moStopsy = TryCast(oSer.Deserialize(oStream), Collection(Of JedenStop))
        Catch ex As Exception
            Return False
        End Try

        uiReload.IsEnabled = True

        Return True

    End Function

    Private Async Function Save(sLinia As String) As Task
        Dim oFile As Windows.Storage.StorageFile
        oFile = Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(
                "line" & sLinia & ".xml")
        If oFile IsNot Nothing Then Await oFile.DeleteAsync()
        ' bez tego kasowania create timestamp jest stary!

        oFile = Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync(
                "line" & sLinia & ".xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)

        If oFile Is Nothing Then Exit Function

        Dim oSer As Xml.Serialization.XmlSerializer
        oSer = New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenStop)))
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        oSer.Serialize(oStream, moStopsy)
        oStream.Dispose()   ' == fclose
    End Function

    Private Async Function WczytajTrase(sLinia As String) As Task(Of Boolean)

        If Not Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
            Await DialogBoxRes("resErrorNoNetwork")
            Return ""
        End If

        Dim oHttp As Windows.Web.Http.HttpClient = New Windows.Web.Http.HttpClient

        Dim sPage As String = ""

        Dim bError As Boolean = False

        'oHttp.Timeout = TimeSpan.FromSeconds(8)
        ' timeout jest tylko w System.Net.http, ale tam nie działa ("The HTTP redirect request failed")

        Try
            sPage = Await oHttp.GetStringAsync(New Uri("http://rozklady.mpk.krakow.pl/?lang=PL&linia=" & sLinia))
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            Await DialogBoxRes("resErrorGetHttp")
            Return ""
        End If

        Dim iInd As Integer
        iInd = sPage.IndexOf("Trasa:")
        If iInd < 10 Then Return False
        iInd = sPage.IndexOf("Przystanki", iInd)
        If iInd < 10 Then Return False
        sPage = sPage.Substring(iInd)
        iInd = sPage.IndexOf("<table")
        If iInd < 10 Then Return False
        If iInd > 250 Then Return False
        sPage = sPage.Substring(iInd)
        iInd = sPage.IndexOf("</table")
        If iInd < 10 Then Return False
        sPage = sPage.Substring(0, iInd - 1)

        sPage = RemoveHtmlTags(sPage)
        Dim aArr As String() = sPage.Split(vbLf)
        Dim iNum As Integer = 0

        moStopsy = New Collection(Of JedenStop)

        For Each sLine As String In aArr
            sLine = sLine.Trim
            If sLine.Length > 2 Then
                Dim oNew As JedenStop = New JedenStop
                oNew.Linia = sLinia
                oNew.Przyst = sLine
                oNew.iMin = 0   ' ewentualnie pozniej liczyc czas przejazdu
                oNew.sMin = ""
                oNew.Num = iNum
                moStopsy.Add(oNew)
                iNum += 1
            End If
        Next

        uiFileDate.Text = ""
        uiReload.IsEnabled = False
        Await Save(sLinia)
        Return True

    End Function

    Private Sub ShowTabliczka(sStop As String)
        For Each oStop As Przystanek In App.oStops.GetList
            If oStop.Name = sStop Then
                App.mbGoGPS = False
                App.mMaxOdl = GetSettingsInt("treatAsSameStop", 150)
                App.mdLat = oStop.Lat
                App.mdLong = oStop.Lon
                App.moOdjazdy.Clear()
                Me.Frame.Navigate(GetType(Odjazdy))
            End If
        Next

    End Sub

    Private Sub uiClose_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.GoBack()
    End Sub

    Private Sub uiGoPrzystanek_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Exit Sub
        Dim oItem As JedenStop = TryCast(oMFI.DataContext, JedenStop)
        If oItem Is Nothing Then Exit Sub
        ShowTabliczka(oItem.Przyst)
    End Sub

    Private Async Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)
        uiReload.IsEnabled = False
        Await WczytajTrase(msLinia)
        uiListStops.ItemsSource = From c In moStopsy Order By c.iMin
    End Sub
End Class
