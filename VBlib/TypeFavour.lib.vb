
' *TODO* konwersja do BasicGeoPos oraz Inherits BasicList

Imports pkar

Public Class FavStop
    Public Property Geo As pkar.BasicGeopos
    Public Property Name As String
    Public Property maxOdl As Integer
End Class


Partial Public Class FavStopList
    Inherits pkar.BaseList(Of FavStop)

    Private msDataFilePath As String = ""
    Private bDirty As Boolean = False

    Public Sub New(sRootPath As String)
        MyBase.New(sRootPath, "favs2.json")
        ' Windows.Storage.ApplicationData.Current.LocalFolder 
        msDataFilePath = sRootPath
    End Sub

    Public Overloads Sub Add(sName As String, oGeo As pkar.BasicGeopos, iMaxOld As Integer)

        ' jesli jest (np. tram), to nie dodawaj (np. bus)
        For Each oNode In Me
            If oNode.Name.ToLowerInvariant = sName.ToLowerInvariant Then Return
        Next

        ' nie ma, to dodaj
        Me.Add(New FavStop() With {
            .Geo = oGeo,
            .Name = sName,
            .maxOdl = iMaxOld
        })
    End Sub

    Public Sub Del(sName As String)
        Dim oToDel As FavStop = Me.FirstOrDefault(Function(x) x.Name.ToLowerInvariant = sName.ToLowerInvariant)
        If oToDel Is Nothing Then Return
        MyBase.Remove(oToDel)
    End Sub

    Protected Overrides Sub InsertDefaultContent()
        Dim oldLista As New pkar.BaseList(Of FavStopOld)(msDataFilePath, "favs.json")

        If Not oldLista.Load Then Return

        Me.Clear()
        For Each oldFav As FavStopOld In oldLista
            Dim oNew As New FavStop
            oNew.Name = oldFav.Name
            oNew.maxOdl = oldFav.maxOdl
            oNew.Geo = New pkar.BasicGeopos(oldFav.Lat, oldFav.Lon)
            Me.Add(oNew)
        Next

        Save()

    End Sub

    Protected Class FavStopOld
        Public Property Lat As Double
        Public Property Lon As Double
        Public Property Name As String
        Public Property maxOdl As Integer
    End Class

End Class
