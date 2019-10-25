
' ewentualnie:
' progressbar przy czytaniu kolejnych tabliczek
' wiecej mozliwosci 'isthismoje' oraz 'biggerpermission': Aska, Gibala, etc?

' 2019.10.25
' mainpage: buttony Pin/Unpin przy Combo, a nie w BottomAppBar
' mainpage: buttony z lupką (search)
' czytanie danych: przy błędzie pokazuje o który przystanek chodzi
' czytanie danych: DialogBox 'zero kursow' nie jest pokazywany przy wczytywaniu tabliczek pojedynczych, tylko raz (globalnie)

' BUG: 20190822 dlaczego pokazuje 'no tram in next hour' przy każdym przystanku tramwajowym?
' 2019.08.04 wczytanie przystanków tram JSON 0 objects - nie Cancel, tylko próbuje wczytac autobusowe

' 2019.07.27
' migracja do pkarmodule
' dla IsThisMoje, statystyka opóźnień - na razie tekstowa


' 4.1907 (29 VI)
' 2019.06.26 odjazdy:kierunek:contextMenu tylko ten, albo usun ten, kierunek z listy

' 2019.04.02
' 1. viewport w HEAD przy pokazywaniu zmian/reroutes zeby bylo czytelne
' 2019.04.09
' 1. pokazywanie częściowej listy (co kazde wczytanie tabliczki)
' 2019.04.19
' 1. zoom mode w historii sieci
' 2. zmiana domyślnego 30 dni na 14 dni w ważności cache objazdów

' 2019.03.16
' 1. przygotowanie: app korzysta z App.GetSettingsInt("treatAsSameStop", 150), ale nie ma ustawiania tego
' 2. wyszukiwanie przystanków wedle mask
' 3. pamietanie sposobu sortowania
' 2019.03.18
' 1. progressring przy wczytywaniu trasy
' 2019.03.21
' 1. strona Zmiany/Reroutes, cache'owalna
' 2. poprawka - po refresh trasy nie było refresh (tylko do pliku zapisywalo nowe)


Public NotInheritable Class MainPage
    Inherits Page

    Private msStopName As String = ""

#If False Then
    Private Sub KontrolaSzerokosci()
        ' kontrola szerokosci dla pola lewego (linia, typ)
        Dim iWidthLine, iWidthTyp, iWidthTime As Integer

        uiTesterTyp.Visibility = Visibility.Visible
        iWidthTyp = uiTesterTyp.ActualWidth  ' typ
        uiTesterTyp.Visibility = Visibility.Collapsed

        uiTesterLinia.Visibility = Visibility.Visible
        iWidthLine = uiTesterLinia.ActualWidth  'linia
        uiTesterLinia.Visibility = Visibility.Collapsed

        uiTesterCzas.Visibility = Visibility.Visible
        iWidthTime = uiTesterCzas.ActualWidth  ' czas
        uiTesterCzas.Visibility = Visibility.Collapsed

        'uiTester.FontSize = 9
        'uiTester.Text = "2014N"
        'iWidth = uiTester.ActualWidth   'typ
        'uiTester.FontSize = 20
        'uiTester.Text = "22 min"
        'iWidth2 = uiTester.ActualWidth  'linia

        'uiTester.FontSize = 28
        'uiTester.FontWeight = Windows.UI.Text.FontWeights.Bold
        'uiTester.Text = "50"
        'uiTester.Visibility = Visibility.Collapsed

        SetSettingsInt("widthCol0", Math.Max(iWidthLine, iWidthTyp))
        SetSettingsInt("widthCol3", iWidthTime)
    End Sub
#End If

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        GetSettingsInt("selectMode", 0)  ' pokazywanie tabliczki: 0: punkt, 1: przystanek id ?
        ' KontrolaSzerokosci()

        ' zeby nie bylo widac...
        uiBusStopList.Visibility = Visibility.Collapsed
        uiGoBusStop.Visibility = Visibility.Collapsed
        HideAppPins()
        HideSearchButtons()

        Await App.LoadFavList
        uiFavList.ItemsSource = From c In App.oFavour.GetList Order By c.Name Select c.Name

        Await App.CheckLoadStopList()
        uiStopList.ItemsSource = From c In App.oStops.GetList("tram") Order By c.Name Select c.Name

        If GetSettingsBool("settingsAlsoBus") Then
            uiBusStopList.Visibility = Visibility.Visible
            uiGoBusStop.Visibility = Visibility.Visible
            uiBusStopList.ItemsSource = From c In App.oStops.GetList("bus") Order By c.Name Select c.Name
        End If

    End Sub

    Private Sub HideAppPins()
        uiUnPin.Visibility = Visibility.Collapsed
        uiPinTram.Visibility = Visibility.Collapsed
        uiPinBus.Visibility = Visibility.Collapsed
        uiAppSep.Visibility = Visibility.Collapsed
    End Sub

    Private Sub HideSearchButtons()
        uiSearchTram.Visibility = Visibility.Collapsed
        If uiPinTram.Visibility = Visibility.Collapsed Then
            uiSearchTram.Visibility = Visibility.Visible
        End If
        If Not GetSettingsBool("settingsAlsoBus") Then Return

        uiSearchBus.Visibility = Visibility.Collapsed
        If uiPinBus.Visibility = Visibility.Collapsed Then
            uiSearchBus.Visibility = Visibility.Visible
        End If

    End Sub

    Private Sub uiFavList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiFavList.SelectionChanged
        HideAppPins()
        uiUnPin.Visibility = Visibility.Visible
    End Sub

    Private Sub uiUnPin_Click(sender As Object, e As RoutedEventArgs)
        ' usun z Fav
        Dim sName As String = uiFavList.SelectedItem
        App.oFavour.Del(sName)
        App.oFavour.Save(False)
        uiFavList.ItemsSource = From c In App.oFavour.GetList Order By c.Name Select c.Name
        ' HideAppPins()
        uiUnPin.Visibility = Visibility.Collapsed
    End Sub


    Private Sub uiPin_Click(sender As Object, e As RoutedEventArgs)
        If msStopName = "" Then Exit Sub

        ' dodaj do Fav
        ' Dim sName As String = uiStopList.SelectedItem
        Dim oPrzyst As Przystanek = App.oStops.GetItem(msStopName)
        If oPrzyst Is Nothing Then Exit Sub

        App.oFavour.Add(msStopName, oPrzyst.Lat, oPrzyst.Lon, 150)  ' odl 150, zeby byl tram/bus
        App.oFavour.Save(False)

        msStopName = "" ' powtorka buttonu nie zadziała

        uiFavList.ItemsSource = From c In App.oFavour.GetList Order By c.Name Select c.Name
        HideAppPins()
    End Sub


    Private Sub uiStopList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiStopList.SelectionChanged, uiBusStopList.SelectionChanged
        Dim oCombo As ComboBox = TryCast(sender, ComboBox)
        HideAppPins()

        msStopName = TryCast(sender, ComboBox).SelectedItem

        If oCombo.Name = "uiBusStopList" Then
            uiSearchBus.Visibility = Visibility.Collapsed
            uiPinBus.Visibility = Visibility.Visible
        Else
            uiSearchTram.Visibility = Visibility.Collapsed
            uiPinTram.Visibility = Visibility.Visible
        End If

    End Sub

    Private Sub uiPinBus_Click(sender As Object, e As RoutedEventArgs)
        uiPin_Click(Nothing, Nothing)
    End Sub

    Private Sub uiPinTram_Click(sender As Object, e As RoutedEventArgs)
        uiPin_Click(Nothing, Nothing)
    End Sub

#Region "GoOdjazdy"
    Private Sub bGetGPS_Click(sender As Object, e As RoutedEventArgs)
        App.mbGoGPS = True    ' zgodnie z GPS prosze postapic (jak do tej pory)
        App.moOdjazdy.Clear()
        Me.Frame.Navigate(GetType(Odjazdy))
    End Sub

    Private Sub uiGoFavour_Click(sender As Object, e As RoutedEventArgs)
        If uiFavList.SelectedValue Is Nothing Then Exit Sub

        Dim sStop As String = uiFavList.SelectedValue.ToString
        For Each oStop As FavStop In App.oFavour.GetList
            If oStop.Name = sStop Then
                App.mbGoGPS = False
                App.mMaxOdl = oStop.maxOdl
                App.mdLat = oStop.Lat
                App.mdLong = oStop.Lon
                App.moOdjazdy.Clear()
                Me.Frame.Navigate(GetType(Odjazdy))
            End If
        Next
    End Sub

    Private Sub GoStop(sName As String, sCat As String)
        For Each oStop As Przystanek In App.oStops.GetList(sCat)
            If oStop.Name = sName Then
                App.mbGoGPS = False
                App.mMaxOdl = GetSettingsInt("treatAsSameStop", 150)
                App.mdLat = oStop.Lat
                App.mdLong = oStop.Lon
                App.msCat = oStop.Cat
                App.moOdjazdy.Clear()
                Me.Frame.Navigate(GetType(Odjazdy))
            End If
        Next

    End Sub

    Private Sub uiGoStop_Click(sender As Object, e As RoutedEventArgs)
        If uiStopList.SelectedValue Is Nothing Then Exit Sub
        ' KontrolaSzerokosci()
        Dim sStop As String = uiStopList.SelectedValue.ToString
        GoStop(sStop, "tram")
    End Sub

    Private Sub uiGoBusStop_Click(sender As Object, e As RoutedEventArgs)
        If uiBusStopList.SelectedValue Is Nothing Then Exit Sub
        ' KontrolaSzerokosci()
        Dim sStop As String = uiBusStopList.SelectedValue.ToString
        GoStop(sStop, "bus")
    End Sub
#End Region

#Region "Wyszukiwanie"
    Private Async Sub uiStopList_DoubleTapped(sender As Object, e As DoubleTappedRoutedEventArgs) Handles uiStopList.DoubleTapped
        Dim sMask As String = Await DialogBoxInput("msgEnterName")

        If sMask = "" Then
            sMask = sMask.ToLower
            uiStopList.ItemsSource = From c In App.oStops.GetList("tram") Order By c.Name Select c.Name
        Else
            sMask = sMask.ToLower
            uiStopList.ItemsSource = From c In App.oStops.GetList("tram") Where c.Name.ToLower.Contains(sMask) Order By c.Name Select c.Name
        End If

    End Sub

    Private Async Sub uiBusStopList_DoubleTapped(sender As Object, e As DoubleTappedRoutedEventArgs) Handles uiBusStopList.DoubleTapped
        Dim sMask As String = Await DialogBoxInput("msgEnterName")
        If sMask = "" Then
            sMask = sMask.ToLower

            uiBusStopList.ItemsSource = From c In App.oStops.GetList("bus") Order By c.Name Select c.Name
        Else
            sMask = sMask.ToLower
            uiBusStopList.ItemsSource = From c In App.oStops.GetList("bus") Where c.Name.ToLower.Contains(sMask) Order By c.Name Select c.Name
        End If

    End Sub

    Private Sub uiSearchBus_Click(sender As Object, e As RoutedEventArgs)
        uiBusStopList_DoubleTapped(Nothing, Nothing)
    End Sub

    Private Sub uiSearchTram_Click(sender As Object, e As RoutedEventArgs)
        uiStopList_DoubleTapped(Nothing, Nothing)
    End Sub

#End Region

#Region "AppBar (podstrony)"
    Private Sub uiChanges_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Zmiany))
    End Sub

    Private Sub uiHist_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Historia))
    End Sub

    Private Sub bSetup_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Setup), "MAIN")
    End Sub


#End Region

End Class
