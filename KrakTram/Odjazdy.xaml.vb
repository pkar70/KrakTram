
Public NotInheritable Class Odjazdy
    Inherits Page

    'Private miSortMode As Integer = 0


    Private Sub bSetup_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Setup), "ODJAZD")
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ' App.moOdjazdy.Clear() - pietro wyzej to jest zrobione
        WczytajPokazDane(False)
    End Sub

    Private Sub uiGetData_Click(sender As Object, e As RoutedEventArgs)
        WczytajPokazDane(True)
    End Sub

#Region "tabliczki"

    Private Async Function WczytajPokazDane(bForce As Boolean) As Task

        If Not Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
            Await DialogBoxRes("resErrorNoNetwork")
            Exit Function
        End If

        uiGoTtssBar.IsEnabled = False

        If App.moOdjazdy.Count = 0 OrElse bForce Then Await CzytanieTabliczek()

        WypiszTabele()

        uiGoTtssBar.IsEnabled = True

    End Function

    Private Async Function CzytanieTabliczek() As Task
        If App.mbGoGPS Then
            ' wedle GPS
            App.mMaxOdl = GetSettingsInt("maxOdl", 1000)
            ' ustaw wspolrzedne
            uiWorking.Text = "o"
            Dim oPoint As Point = Await App.GetCurrentPoint
            App.mdLat = oPoint.X
            App.mdLong = oPoint.Y
            uiWorking.Text = " "
        Else
            ' wedle punktu z App.mdLat, mdLong
        End If

        App.moOdjazdy.Clear()

        ' ustalony jest skąd szukamy przystanków i jak daleko
        Await WczytajTabliczkiWOdleglosci(App.mdLat, App.mdLong, App.mMaxOdl)

        App.moOdjazdy.OdfiltrujMiedzytabliczkowo()

    End Function

    Private Async Function WczytajTabliczkiWOdleglosci(dLat As Double, dLon As Double, dOdl As Double) As Task
        Dim iWorking As Integer = 0
        Dim iOdl As Integer

        Dim sFilter As String = "tram"
        If GetSettingsBool("settingsAlsoBus") Then sFilter = "all"

        For Each oNode As Przystanek In App.oStops.GetList(sFilter)
            uiWorking.Text = "."
            iOdl = App.GPSdistanceDwa(dLat, dLon, oNode.Lat, oNode.Lon)
            If iOdl < dOdl Then
                iWorking += 1
                Select Case iWorking Mod 4
                    Case 1
                        uiWorking.Text = "/"
                    Case 2
                        uiWorking.Text = "-"
                    Case 3
                        uiWorking.Text = "\"
                    Case 0
                        uiWorking.Text = "|"
                End Select

                Await App.moOdjazdy.WczytajTabliczke(oNode.Cat, CInt(oNode.id), iOdl)

                WypiszTabele()  ' w trakcie - pokazujemy na raty, zeby cos sie dzialo
            End If
        Next
    End Function

    Private Sub WypiszTabele()

        If App.moOdjazdy.Count < 1 Then
            DialogBoxRes("resZeroKursow")
            Exit Sub
        End If

        Select Case GetSettingsInt("sortMode")
            Case 1  ' stop/czas/dir
                uiListItems.ItemsSource = From c In App.moOdjazdy.GetItems Order By c.Przyst, c.TimeSec, c.Kier Where c.bShow = True
            Case 2  ' dir/stop/czas
                uiListItems.ItemsSource = From c In App.moOdjazdy.GetItems Order By c.Kier, c.Przyst, c.TimeSec Where c.bShow = True
            Case 3   ' czas/line
                uiListItems.ItemsSource = From c In App.moOdjazdy.GetItems Order By c.TimeSec, c.iLinia Where c.bShow = True
            Case Else   ' czyli takze domyslne zero; linia/kierunek/czas
                uiListItems.ItemsSource = From c In App.moOdjazdy.GetItems Order By c.iLinia, c.Kier, c.TimeSec Where c.bShow = True
        End Select

    End Sub
#End Region

#Region "przelaczanie sortowania"


    Private Sub SetSortMode(bInit As Boolean, iMode As Integer)

        uiSortKier.IsChecked = False
        uiSortLine.IsChecked = False
        uiSortStop.IsChecked = False
        uiSortCzas.IsChecked = False

        If bInit Then iMode = GetSettingsInt("sortMode")

        Select Case iMode
            Case 0  ' line
                uiSortLine.IsChecked = True
            Case 1  ' stop
                uiSortStop.IsChecked = True
            Case 2  ' kier
                uiSortKier.IsChecked = True
            Case Else
                ' czas
                iMode = 3
                uiSortCzas.IsChecked = True
        End Select

        If Not bInit Then
            SetSettingsInt("sortMode", iMode)
            WypiszTabele()
        End If

    End Sub

    Private Sub bSortByLine_Click(sender As Object, e As RoutedEventArgs)
        SetSortMode(False, 0)
        'miSortMode = 0
        'uiSortKier.IsChecked = False
        'uiSortLine.IsChecked = True
        'uiSortStop.IsChecked = False
        'uiSortCzas.IsChecked = False
        'WypiszTabele()
    End Sub

    Private Sub bSortByStop_Click(sender As Object, e As RoutedEventArgs)
        SetSortMode(False, 1)
    End Sub

    Private Sub bSortByKier_Click(sender As Object, e As RoutedEventArgs)
        SetSortMode(False, 2)
    End Sub

    Private Sub bSortByCzas_Click(sender As Object, e As RoutedEventArgs)
        SetSortMode(False, 3)
    End Sub
#End Region

    Private Sub uiShowStops_Click(sender As Object, e As RoutedEventArgs)

        ' sender = grid
        'uiGrid.BorderThickness = 1
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        Dim oItem As JedenOdjazd = TryCast(oMFI.DataContext, JedenOdjazd)

        Dim sParam As String
        sParam = oItem.Linia & "|" & oItem.Kier & "|" & oItem.Przyst
        Me.Frame.Navigate(GetType(Trasa), sParam)

    End Sub

    Private Sub uiRawData_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        Dim oItem As JedenOdjazd = TryCast(oMFI.DataContext, JedenOdjazd)

        DialogBox(oItem.sRawData)
    End Sub

    Private Sub uiExcludeKier_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JedenOdjazd = TryCast(oMFI.DataContext, JedenOdjazd)
        If oItem Is Nothing Then Return

        App.moOdjazdy.FiltrWedleKierunku(True, oItem.Kier)
        WypiszTabele()
    End Sub

    Private Sub uiOnlyThisKier_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JedenOdjazd = TryCast(oMFI.DataContext, JedenOdjazd)
        If oItem Is Nothing Then Return

        App.moOdjazdy.FiltrWedleKierunku(False, oItem.Kier)
        WypiszTabele()
    End Sub
End Class
