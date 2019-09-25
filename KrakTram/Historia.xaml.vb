' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class Historia
    Inherits Page

    Const MIN_ROK As Integer = 1882
    Const MAX_ROK As Integer = 2015

    Dim mbBlockSlider As Boolean = True

    Private Sub UstawSlider()
        uiSlider.Minimum = MIN_ROK
        uiSlider.Maximum = MAX_ROK

        ' Dim iMin As Integer = Date.Now.Year
        ' w petli probujemy otwierac
        ' i najnowszy dodajemy - ale prosciej wpisac na sztywno :)
    End Sub

    Private Sub UstawTitle(iRok As Integer)
        uiTitle.Text = GetLangString("resHistoriaTitle") & " " & iRok
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        If IsThisMoje() Then uiCommandBar.Visibility = Visibility.Visible

        UstawSlider()
        mbBlockSlider = False

        uiSlider.Value = MAX_ROK

    End Sub

    Private Async Function WczytajPicek(iRok As Integer) As Task(Of Boolean)
        If iRok > MAX_ROK Then Return False
        If iRok < MIN_ROK Then Return False

        Dim oPicUri As Uri = New Uri("ms-appx:///Assets/" & iRok & ".gif")

        Dim oFile As Windows.Storage.StorageFile
        Try
            oFile = Await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(oPicUri)
        Catch ex As Exception
            Return False
        End Try

        Dim oBmp As BitmapImage = New BitmapImage
        oBmp.UriSource = oPicUri
        uiPic.Source = oBmp
        Return True
    End Function

    Private Async Sub UiSlider_ValueChanged(sender As Object, e As RangeBaseValueChangedEventArgs) Handles uiSlider.ValueChanged
        If mbBlockSlider Then Exit Sub

        Dim iRok As Integer = uiSlider.Value

        ' w petli probuj ustawic obrazek, az bedzie
        While iRok <= MAX_ROK
            If Await WczytajPicek(iRok) Then Exit While
            iRok = iRok + 1
        End While

        'If iRok <> uiSlider.Value Then
        '    mbBlockSlider = True
        '    uiSlider.Value = iRok
        '    mbBlockSlider = False
        'End If

        UstawTitle(iRok)

    End Sub

    Private Sub uiOpoznienia_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Opoznienia))
    End Sub
End Class
