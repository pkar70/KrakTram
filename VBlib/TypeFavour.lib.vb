

Public Class FavStop
    Public Property Lat As Double
    Public Property Lon As Double
    Public Property Name As String
    Public Property maxOdl As Integer
End Class

Partial Public Class FavStopList
    Public sLastError As String

    Private moItemy As ObjectModel.Collection(Of FavStop) = New ObjectModel.Collection(Of FavStop)()
    Private msDataFilePath As String = ""
    Private bDirty As Boolean = False

    Public Sub New(sRootPath As String)
        ' Windows.Storage.ApplicationData.Current.LocalFolder 
        msDataFilePath = System.IO.Path.Combine(sRootPath, "favs.json")
    End Sub

    Public Sub Add(sName As String, dLat As Double, dLon As Double, iMaxOld As Integer)

        ' jesli jest (np. tram), to nie dodawaj (np. bus)
        For Each oNode In moItemy
            If Equals(If(oNode.Name, ""), If(sName, "")) Then Return
        Next

        ' nie ma, to dodaj
        Dim oNew = New FavStop()
        oNew.Lat = dLat
        oNew.Lon = dLon
        oNew.Name = sName
        oNew.maxOdl = iMaxOld
        moItemy.Add(oNew)
        bDirty = True
    End Sub

    Public Sub Del(sName As String)
        For Each oItem In moItemy

            If oItem.Name = sName Then
                moItemy.Remove(oItem)
                Return
            End If
        Next
    End Sub

    ''' <summary>
    ''' ret=-1 error; see LastError; albo =0 (nie ma pliku), albo = 1
    ''' </summary>
    Public Function Load() As Integer
        ' ret=false gdy nie jest wczytane

        If Not System.IO.File.Exists(msDataFilePath) Then Return 0
        Dim sTxt As String = System.IO.File.ReadAllText(msDataFilePath)

        Try
            moItemy = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObjectModel.Collection(Of FavStop)))
        Catch ex As Exception
            sLastError = "ERROR reading fav file?"
            Return -1
        End Try

        Return 1

    End Function

    Public Sub Save(bForce As Boolean)
        If Not bForce And Not bDirty Then Return
        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(moItemy, Newtonsoft.Json.Formatting.Indented)
        System.IO.File.WriteAllText(msDataFilePath, sTxt)
    End Sub

    Public Function GetList() As ObjectModel.Collection(Of FavStop)
        Return moItemy
    End Function
End Class
