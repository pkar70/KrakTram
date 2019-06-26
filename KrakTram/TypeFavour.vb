Imports System.Xml.Serialization

<XmlType("stop")>
Public Class FavStop
    <XmlAttribute()>
    Public Property Lat As Double
    <XmlAttribute()>
    Public Property Lon As Double
    <XmlAttribute()>
    Public Property Name As String
    <XmlAttribute()>
    Public Property maxOdl As Integer
End Class

Public Class FavStopList

    Private Itemy As Collection(Of FavStop) = New Collection(Of FavStop)
    Private bDirty As Boolean = False

    Public Sub Add(sName As String, dLat As Double, dLon As Double, iMaxOld As Integer)

        ' jesli jest (np. tram), to nie dodawaj (np. bus)
        For Each oNode As FavStop In Itemy
            If oNode.Name = sName Then Exit Sub
        Next

        ' nie ma, to dodaj
        Dim oNew As FavStop = New FavStop
        oNew.Lat = dLat
        oNew.Lon = dLon
        oNew.Name = sName
        oNew.maxOdl = iMaxOld
        Itemy.Add(oNew)
        bDirty = True
    End Sub

    Public Sub Del(sName As String)
        For Each oItem As FavStop In Itemy
            If oItem.Name = sName Then
                Itemy.Remove(oItem)
                Exit Sub
            End If
        Next
    End Sub

    Public Sub InitPkar()
        Add("domek", 50.0198, 19.9785, 500)
        Add("meiselsa", 50.0513642, 19.9432361, 600)
        Add("franc3", 50.059781, 19.9339632, 500)
        Add("widok", 50.0789713, 19.8816113, 500)
        Save(False)
        App.SetSettingsBool("pkarmode", True, True) ' roaming data
    End Sub

    ' Load
    Private Async Function Load() As Task(Of Boolean)
        ' ret=false gdy nie jest wczytane

        Dim oObj As Windows.Storage.StorageFile =
            Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("favs.xml")
        If oObj Is Nothing Then Return False
        Dim oFile As Windows.Storage.StorageFile = TryCast(oObj, Windows.Storage.StorageFile)

        Dim oSer As XmlSerializer = New XmlSerializer(GetType(Collection(Of FavStop)))
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Itemy = TryCast(oSer.Deserialize(oStream), Collection(Of FavStop))
        bDirty = False
        Return True
    End Function

    Public Async Function Save(bForce As Boolean) As Task

        If Not bForce And Not bDirty Then Exit Function

        Dim oFile As Windows.Storage.StorageFile =
            Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync(
                "favs.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)

        If oFile Is Nothing Then Exit Function

        Dim oSer As XmlSerializer = New XmlSerializer(GetType(Collection(Of FavStop)))
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        oSer.Serialize(oStream, Itemy)
        oStream.Dispose()   ' == fclose
        bDirty = False
    End Function

    Private Async Function Import() As Task(Of Boolean)
        Dim sOldVers As String = App.GetSettingsString("favPlaces")
        If sOldVers.Length < 25 Then    ' "<places></places>
            App.SetSettingsString("favPlaces", "")
            Return False
        End If

        ' skoro jest zmienna, to nalezy to zaimportowac
        Dim bError As Boolean = False
        Dim oXmlPlaces As New Windows.Data.Xml.Dom.XmlDocument
        Try
            oXmlPlaces.LoadXml(sOldVers)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            App.DialogBox("ERROR loading favourites list")
            Return False
        End If

        For Each oPlace As Windows.Data.Xml.Dom.IXmlNode In oXmlPlaces.DocumentElement.SelectNodes("//place")
            Add(oPlace.SelectSingleNode("@name").NodeValue,
                oPlace.SelectSingleNode("@lat").NodeValue,
                oPlace.SelectSingleNode("@long").NodeValue,
                oPlace.SelectSingleNode("@maxOdl").NodeValue)
        Next

        Await Save(True)
        App.SetSettingsString("favPlaces", "")
        App.DialogBoxRes("resImportedOldFav")
        Return True
    End Function


    Public Async Function LoadOrImport() As Task

        If Await Load() Then Exit Function
        Import()

    End Function

    Public Function GetList() As ICollection(Of FavStop)
        Return Itemy
    End Function
End Class
