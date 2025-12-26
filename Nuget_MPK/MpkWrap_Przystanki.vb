
Imports pkar.MpkMain

Namespace MpkWrap

    ''' <summary>
    ''' wrapper dla MPKinternal, format danych: mój
    ''' </summary>
    Public Class Przystanki
        Inherits BaseList(Of Przystanek)

        Private oMPK As New MPK_Merged

        Private _maxDays As Integer = 30

        Public Sub New(cacheFolder As String, Optional maxDaysCache As Integer = 30)
            MyBase.New(cacheFolder, "stops2.json")
            _maxDays = maxDaysCache
        End Sub


        ''' <summary>
        ''' lista zmian przystanków, jeśli wczytywano przystanki
        ''' </summary>
        ''' <returns></returns>
        Public Property ZmianyPrzystankow As String = ""

        ''' <summary>
        ''' spróbuj wczytać dane, jeśli za stare - gdy netAvail to import
        ''' </summary>
        ''' <param name="bForceLoad"></param>
        ''' <param name="bNetAvail"></param>
        ''' <returns></returns>
        Public Async Function LoadOrImport(bForceLoad As Boolean, bNetAvail As Boolean, Optional bDoCompare As Boolean = False) As Task(Of Boolean)

            If IsObsolete(_maxDays) Then bForceLoad = True

            Dim bReaded = False
            If Not bForceLoad Then bReaded = Load()  ' True gdy udane wczytanie; nie ma sensu czytac gdy wymuszamy import

            If bReaded AndAlso Count() < 1 Then bReaded = False      ' jak jest puste w ogole

            If bReaded Then
                If Not bForceLoad Then Return True
                If Not bNetAvail Then Return True
            End If

            If Not bNetAvail Then Return False

            Dim nowaLista As List(Of Przystanek) = Await oMPK.DownloadListaPrzystankowAsync
            'Dim noweItemyMPK As List(Of Przystanek) = Await oMPK.DownloadListaPrzystankowAsync

            'Dim nowaLista As New List(Of Przystanek)
            'For Each oNowyMpk As MpkMain.MpkPrzystanek In noweItemyMPK
            '    If oNowyMpk.category.ToLowerInvariant = "other" Then Continue For

            '    nowaLista.Add(New Przystanek(oNowyMpk))
            'Next

            If bDoCompare Then
                ZmianyPrzystankow = Compare(Me, nowaLista)
            End If

            Me.Clear()
            Me.AddRange(nowaLista)

            Save(True)

            Return True

        End Function

        Public Function GetItem(sName As String, Optional isBus As Boolean = False) As Przystanek
            For Each oItem In Me
                If oItem.Name = sName AndAlso oItem.IsBus = isBus Then Return oItem
            Next

            Return Nothing
        End Function

        Public Overloads Function GetList(Optional sCat As String = "tram") As List(Of Przystanek)
            Select Case sCat
                Case "all"
                    Return Me
                Case "bus"
                    ' Return From c In moItemy Where sCat = "bus"
                    Return Where(Function(s) Equals(s.IsBus, True)).ToList
                Case Else
                    Return Where(Function(s) Equals(s.IsBus, False)).ToList
            End Select
        End Function

        Private Shared Function Compare(oOld As List(Of Przystanek), oNew As List(Of Przystanek)) As String
            Dim sDiffsDel = ""

            For Each oItemOld In oOld
                Dim bDalej = False

                For Each oItemNew In oNew

                    If Equals(If(oItemNew.Name, ""), If(oItemOld.Name, "")) Then
                        bDalej = True
                        Exit For
                    End If
                Next

                If Not bDalej Then sDiffsDel = sDiffsDel & oItemOld.Name & vbLf
            Next

            Dim sDiffsNew = ""

            For Each oItemNew In oNew
                Dim bNowe = True

                For Each oItemOld In oOld

                    If Equals(If(oItemNew.Name, ""), If(oItemOld.Name, "")) Then
                        bNowe = False
                        Exit For
                    End If
                Next

                If bNowe Then sDiffsNew = sDiffsNew & oItemNew.Name & vbLf
            Next

            If Not String.IsNullOrEmpty(sDiffsNew) Then sDiffsNew = "Nowe:" & vbLf & sDiffsNew
            If Not String.IsNullOrEmpty(sDiffsDel) Then sDiffsDel = "Usunięte:" & vbLf & sDiffsDel

            If String.IsNullOrEmpty(sDiffsNew) AndAlso String.IsNullOrEmpty(sDiffsDel) Then Return ""

            Return sDiffsDel & vbCrLf & sDiffsNew
        End Function
    End Class

    Public Class Przystanek
        Inherits BaseStruct

        Public Property IsBus As Boolean
        Public Property Geo As BasicGeopos
        Public Property Name As String
        Public Property id As String

        ' dla AMIstad
        Public Property Ami_AlsoTram As Boolean
        Public Property Ami_Ids As String
        Public Property Ami_Count As Integer

        ' tak by mógł wczytać JSON :)
        Public Sub New(Optional wersjaMPK As MpkMain.MpkPrzystanek = Nothing)
            If wersjaMPK IsNot Nothing Then
                IsBus = (wersjaMPK.category.ToLowerInvariant <> "tram")
                Geo = BasicGeoposFromMpk(wersjaMPK.latitude, wersjaMPK.longitude)
                Name = wersjaMPK.name
                id = wersjaMPK.shortName
            End If
        End Sub

        Public Shared Function BasicGeoposFromMpk(latitude As Integer, longitude As Integer) As BasicGeopos
            Return New BasicGeopos(latitude / 3600000.0, longitude / 3600000.0)
        End Function
    End Class
End Namespace

