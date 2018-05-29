' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class ListaPrzystankow
    Inherits Page

    Private Sub bOk_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.GoBack()
    End Sub

    Private Sub bLoadStops_Click(sender As Object, e As RoutedEventArgs) Handles uiReloadStop.Click
        uiReloadStop.IsEnabled = False
        App.CheckLoadStopList(True)
        uiReloadStop.IsEnabled = True
    End Sub

    Private Sub uiItem_Tapped(sender As Object, e As TappedRoutedEventArgs)
        ' moBazaItems.Delete(TryCast(TryCast(sender, MenuFlyoutItem).DataContext, BazaItem).Nazwa)
        Dim sTxt = App.GetSettingsString("favPlaces", "<places></places>")

        Dim oTBox = TryCast(sender, TextBlock)
        If oTBox Is Nothing Then Exit Sub
        Dim oItem = TryCast(oTBox.DataContext, App.Przystanek)

        If sTxt.IndexOf("'" & oItem.Name & "'") > 0 Then Exit Sub ' jest juz taka nazwa

        Dim sItem = "<place name='" & oItem.Name & "'" &
                " long='" & oItem.Lon & "'" &
                " lat='" & oItem.Lat & "'" &
                " maxOdl='" & App.GetSettingsInt("gpsPrec", 75) & "' />" & vbCrLf
        sTxt = sTxt.Replace("</places>", sItem & "</places>")

        App.SetSettingsString("favPlaces", sTxt)

    End Sub

    Private Sub uiPage_Loaded(sender As Object, e As RoutedEventArgs)
        ListItems.ItemsSource = From c In App.oStops.GetList Order By c.Name
    End Sub
    ' pokazanie listy przystankow
    ' mozliwosc dodania do Favourites
End Class
