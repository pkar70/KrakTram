
''' <summary>
''' simple "database class", using dictionary JSON file
''' </summary>
''' <typeparam name="TYP">Type of elements</typeparam>
Public Class BaseDict(Of TKEY, TVALUE)
    Protected _lista As Dictionary(Of TKEY, TVALUE)
    Private _filename As String

    ''' <summary>
    ''' create new list, and use given file in given folder as data backing
    ''' </summary>
    Public Sub New(sFolder As String, Optional sFileName As String = "items.json")

        If String.IsNullOrWhiteSpace(sFolder) OrElse String.IsNullOrWhiteSpace(sFileName) Then
            Throw New ArgumentException("you have to provide both folder and filename")
        End If
        _lista = New Dictionary(Of TKEY, TVALUE)
        _filename = IO.Path.Combine(sFolder, sFileName)
    End Sub


    ''' <summary>
    ''' This method is called when Load ends with empty list - override it to fill default entries
    ''' </summary>
    Protected Overridable Sub InsertDefaultContent()

    End Sub


#Region "operations on data file"

    ''' <summary>
    ''' load list from file
    ''' </summary>
    ''' <returns>False: no or empty file, True: something was read</returns>
    Public Overridable Function Load() As Boolean

        Dim sTxt As String = ""
        If IO.File.Exists(_filename) Then
            sTxt = IO.File.ReadAllText(_filename)
        End If

        If sTxt Is Nothing OrElse sTxt.Length < 5 Then
            Clear()
            InsertDefaultContent()
            Return False
        End If

        Try
            _lista = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(Dictionary(Of TKEY, TVALUE)))
            Return True
        Catch ex As Exception
        End Try

        Try
            ' if we simply Append to file, it can have last "]" missing, so we try adding it
            _lista = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt & "]", GetType(Dictionary(Of TKEY, TVALUE)))
            Return True
        Catch ex As Exception
        End Try

        Return False
    End Function

    ''' <summary>
    ''' you can use it as a constructor of list item from JSON string
    ''' </summary>
    ''' <param name="sJSON"></param>
    ''' <returns></returns>
    Public Function LoadItem(sJSON As String) As KeyValuePair(Of TKEY, TVALUE)
        Try
            Return Newtonsoft.Json.JsonConvert.DeserializeObject(sJSON, GetType(KeyValuePair(Of TKEY, TVALUE)))
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ''' <summary>
    '''  save data to file
    ''' </summary>
    ''' <param name="bIgnoreNulls">if NullValueHandling.Ignore and DefaultValueHandling.Ignore should be true (shorten file)</param>
    ''' <returns></returns>
    Public Overridable Function Save(Optional bIgnoreNulls As Boolean = False) As Boolean

        If _lista Is Nothing Then
            Return False
        End If
        If _lista.Count < 1 Then
            Return False
        End If

        Dim sTxt As String
        If bIgnoreNulls Then
            Dim oSerSet As New Newtonsoft.Json.JsonSerializerSettings With {.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore, .DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore}
            sTxt = Newtonsoft.Json.JsonConvert.SerializeObject(_lista, Newtonsoft.Json.Formatting.Indented, oSerSet)
        Else
            sTxt = Newtonsoft.Json.JsonConvert.SerializeObject(_lista, Newtonsoft.Json.Formatting.Indented)
        End If

        IO.File.WriteAllText(_filename, sTxt)

        Return True
    End Function

    ''' <summary>
    ''' GetLastWriteTime of data file, or 1 I 1970 if file doesn't exist (so it would seem as very old) 
    ''' </summary>
    Public Function GetFileDate() As Date
        If IO.File.Exists(_filename) Then
            Return IO.File.GetLastWriteTime(_filename)
        Else
            Return New Date(1970, 1, 1)
        End If
    End Function

    ''' <summary>
    ''' check if file last write was more than iDays  ago
    ''' </summary>
    Public Function IsObsolete(iDays As Integer)
        If GetFileDate.AddDays(iDays) < Date.Now Then Return True
        Return False
    End Function
#End Region

#Region "proxies for internal list"
    ''' <summary>
    ''' get internal list of items
    ''' </summary>
    ''' <returns></returns>
    Public Function GetDictionary() As Dictionary(Of TKEY, TVALUE)
        Return _lista
    End Function

    ''' <summary>
    ''' get the number of elements containted in list
    ''' </summary>
    Public Function Count() As Integer
        Return _lista.Count
    End Function

    ''' <summary>
    '''  removes all elements from list
    ''' </summary>
    Public Sub Clear()
        _lista.Clear()
    End Sub

    ''' <summary>
    ''' add new item to the end of list
    ''' </summary>
    ''' <param name="oNew"></param>
    Public Function TryAdd(oNew As KeyValuePair(Of TKEY, TVALUE)) As Boolean
        Return TryAdd(oNew.Key, oNew.Value)
    End Function

    Public Function ContainsKey(key As TKEY) As Boolean
        Return _lista.ContainsKey(key)
    End Function

    ''' <summary>
    ''' add new item to the end of list
    ''' </summary>
    Public Function TryAdd(key As TKEY, value As TVALUE) As Boolean
        If ContainsKey(key) Then Return False
        _lista.Add(key, value)
        Return True
    End Function

    ''' <summary>
    ''' remove first occurence of given item
    ''' </summary>
    Public Sub Remove(oDel As KeyValuePair(Of TKEY, TVALUE))
        _lista.Remove(oDel.Key)
    End Sub

    ''' <summary>
    ''' remove first occurence of given item
    ''' </summary>
    Public Sub Remove(key As TKEY)
        _lista.Remove(key)
    End Sub

    ''' <summary>
    ''' Searches for an element that matches the conditions defined by the specified predicate, and returns the first occurrence within the entire list
    ''' e.g., in VB: Find(Function(x) x.PartName.Contains("seat"))
    ''' </summary>
    Public Function Item(key As TKEY) As TVALUE
        Return _lista.Item(key)
    End Function

    Public Function TryGetValue(key As TKEY, ByRef value As TVALUE) As Boolean
        Return _lista.TryGetValue(key, value)
    End Function
#End Region


End Class




