
Public NotInheritable Class Opoznienia
    Inherits Page

    Private mdDelayMinsTram As Double = 0
    Private miDelayCntTram As Integer = 0
    Private mdDelayMinsBus As Double = 0
    Private miDelayCntBus As Integer = 0

    Private Sub Procesuje(bShow As Boolean)
        If bShow Then
            Dim dVal As Double
            dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
            uiProcesuje.Width = dVal
            uiProcesuje.Height = dVal
            uiProcesuje.Visibility = Visibility.Visible
            uiProcesuje.IsActive = True
            uiMapka.Visibility = Visibility.Collapsed
        Else
            uiProcesuje.IsActive = False
            uiProcesuje.Visibility = Visibility.Collapsed
            uiMapka.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub PokazDelayStat(iDelay As Integer, iCount As Integer, iMaxDelay As Integer, uiDelay As TextBlock, uiData As TextBlock)
        If iCount = 0 Then
            uiDelay.Text = "--"
            uiData.Text = "--"
            Return
        End If

        Dim dDelay As Double = iDelay / iCount
        uiDelay.Text = dDelay.ToString("####0.#") & " mins"
        Dim sTmp As String = "(" & iDelay & "/" & iCount
        If iMaxDelay > 0 Then sTmp = sTmp & ", max " & iMaxDelay
        sTmp = sTmp & ")"
        uiData.Text = sTmp

    End Sub

    Private Sub PokazTotalDelay()
        PokazDelayStat(mdDelayMinsTram + mdDelayMinsBus, miDelayCntTram + miDelayCntBus, -1, uiTotalDelay, uiTotalCount)
    End Sub

    Private Async Sub uiTramReload_Click(sender As Object, e As RoutedEventArgs)
        Dim iDelay As Double
        Dim iCount, iMaxDelay As Integer

        Procesuje(True)
        Dim bRet As Boolean = Await App.oStops.OpoznieniaFromHttpAsync(1)
        If bRet Then bRet = App.oStops.OpoznieniaGetStat(1, iDelay, iCount, iMaxDelay)
        Procesuje(False)
        If Not bRet Then Return

        mdDelayMinsTram = iDelay
        miDelayCntTram = iCount
        PokazDelayStat(mdDelayMinsTram, miDelayCntTram, iMaxDelay, uiTramDelay, uiTramCount)
        PokazTotalDelay()
    End Sub

    Private Async Sub uiBusReload_Click(sender As Object, e As RoutedEventArgs)
        Dim iDelay As Double
        Dim iCount, iMaxDelay As Integer

        Procesuje(True)
        Dim bRet As Boolean = Await App.oStops.OpoznieniaFromHttpAsync(2)
        If bRet Then bRet = App.oStops.OpoznieniaGetStat(2, iDelay, iCount, iMaxDelay)
        Procesuje(False)
        If Not bRet Then Return

        mdDelayMinsBus = iDelay
        miDelayCntBus = iCount
        PokazDelayStat(mdDelayMinsBus, miDelayCntBus, iMaxDelay, uiBusDelay, uiBusCount)

        PokazTotalDelay()

    End Sub

    Private Sub uiTotalReshow_Click(sender As Object, e As RoutedEventArgs)
        ' 4. info: aktualna średnia, liczba wpisów, liczba wpisów z minutami (a nie rozkładowe)
        ' 5. pokazywanie na mapie, kółka z kolorami
        App.oStops.OpoznieniaDoMapy(uiTramCB.IsChecked, uiBusCB.IsChecked, uiMapka)
    End Sub

    Private Sub uiMapka_Loaded(sender As Object, e As RoutedEventArgs) Handles uiMapka.Loaded

        Dim oPosition As Windows.Devices.Geolocation.BasicGeoposition
        oPosition = New Windows.Devices.Geolocation.BasicGeoposition
        oPosition.Latitude = 50.061389  ' współrzędne wedle Wiki
        oPosition.Longitude = 19.938333
        'Dim oPoint As Windows.Devices.Geolocation.Geopoint
        'oPoint = New Windows.Devices.Geolocation.Geopoint(oPosition)

        uiMapka.Center = New Windows.Devices.Geolocation.Geopoint(oPosition)
        uiMapka.ZoomLevel = 12
        uiMapka.Style = Maps.MapStyle.Road
        'uiMapka.MapProjection = MapProjection.WebMercator  ; od 15063
    End Sub
End Class
