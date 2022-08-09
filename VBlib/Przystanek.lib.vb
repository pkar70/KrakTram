
Imports System.Linq ' bez tego nie ma .Where


Public Class Przystanek
    Public Property Cat As String
    Public Property Lat As Double
    Public Property Lon As Double
    Public Property Name As String
    Public Property id As String
    <Newtonsoft.Json.JsonIgnore>
    Public Property iSumDelay As Integer
    <Newtonsoft.Json.JsonIgnore>
    Public Property iMaxDelay As Integer
    <Newtonsoft.Json.JsonIgnore>
    Public Property iEntriesCount As Integer
    <Newtonsoft.Json.JsonIgnore>
    Public Property iEntriesTotal As Integer
End Class

Public Class Przystanki
    Public sLastError As String

    Private moItemy As ObjectModel.Collection(Of Przystanek) = New ObjectModel.Collection(Of Przystanek)()
    Private msDataFilePath As String = ""

    Public Sub New(sRootPath As String)
        ' Windows.Storage.ApplicationData.Current.LocalCacheFolder
        msDataFilePath = System.IO.Path.Combine(sRootPath, "stops.json")
    End Sub

    Private Sub Add(sCat As String, dLatTtss As Double, dLonTtss As Double, sName As String, sId As String)
        Dim oNew = New Przystanek()
        oNew.Cat = sCat
        oNew.id = sId
        oNew.Name = sName
        oNew.Lat = dLatTtss / 3600000.0
        oNew.Lon = dLonTtss / 3600000.0
        moItemy.Add(oNew) ' błąd resource; dodawanie pustego ("" oraz 0) też powoduje error
    End Sub

    Private Sub Save()
        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(moItemy, Newtonsoft.Json.Formatting.Indented)
        System.IO.File.WriteAllText(msDataFilePath, sTxt)
    End Sub

    ''' <summary>
    ''' ret=-1 error; see LastError; albo =0 (nie ma pliku), albo = 1
    ''' </summary>
    Private Function Load() As Integer
        ' ret=false gdy nie jest wczytane

        If Not System.IO.File.Exists(msDataFilePath) Then Return 0
        Dim sTxt As String = System.IO.File.ReadAllText(msDataFilePath)

        Try
            moItemy = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObjectModel.Collection(Of Przystanek)))
        Catch ex As Exception
            sLastError = "ERROR reading stops file?"
            Return -1
        End Try

        Return 1

    End Function

    ''' <summary>
    ''' ret="" ok, lub =komunikat błędu
    ''' </summary>
    Private Function ImportNewtonsoftJSON(sPage As String) As String
        Dim oJson As Newtonsoft.Json.Linq.JObject = Nothing

        Try
            oJson = Newtonsoft.Json.Linq.JObject.Parse(sPage)
        Catch
            Return "ERROR: JSON parsing error"
        End Try

        Dim oJsonStops As Newtonsoft.Json.Linq.JArray = New Newtonsoft.Json.Linq.JArray()

        Try
            oJsonStops = CType(oJson("stops"), Newtonsoft.Json.Linq.JArray)
        Catch
            Return "ERROR: JSON ""stops"" array missing"
        End Try


        If oJsonStops.Count = 0 Then Return "ERROR: JSON 0 obiektów"

        Try

            For Each oVal As Newtonsoft.Json.Linq.JObject In oJsonStops
                Dim sName As String
                Dim sCat As String
                Dim sShortName As String
                sName = oVal("name")
                sCat = oVal("category")
                sShortName = oVal("shortName")
                Dim dLat As Double
                Dim dLon As Double
                dLat = oVal("latitude")
                dLon = oVal("longitude")
                If sName.Length > 2 Then Add(sCat, dLat, dLon, sName, sShortName)
            Next

        Catch
            Return "ERROR: at JSON converting"
        End Try

        Return ""
    End Function


    ''' <summary>
    ''' ret="" ok, lub =komunikat błędu
    ''' </summary>
    Private Async Function ImportMain(sUrl As String) As Task(Of String)

        Dim sTmp = ""

        Using oHttp As Net.Http.HttpClient = New Net.Http.HttpClient()
            oHttp.Timeout = System.TimeSpan.FromSeconds(10)
            Try
                sTmp = Await oHttp.GetStringAsync(sUrl)
            Catch
                Return GetLangString("resErrorGetHttp")
            End Try
        End Using

        ' {"stops": [
        ' {
        ' "category": "tram",
        ' "id": "6350927454370005230",
        ' "latitude": 180367133,
        ' "longitude": 72043450,
        ' "name": "Os.Piastów",
        ' "shortName": "378"
        ' },

        If sTmp.IndexOf("""stops""") < 0 Then Return GetLangString("resErrorBadTTSSstops")
        Return ImportNewtonsoftJSON(sTmp)
    End Function

    ''' <summary>
    ''' ret="" gdy OK, lub komunikat błędu
    ''' </summary>
    Private Async Function Import() As Task(Of String)
        ' tylko z poniższej

        moItemy.Clear()
        Dim sRetMsg As String = Await ImportMain("http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000")
        ' sRetMsg - błąd z tramwajów
        If sRetMsg <> "" Then sRetMsg &= vbCrLf
        ' normalnie tu był message box z pierwszym błędem, ale zrobimy to później

        ' wczytujemy zawsze - bo mozna potem włączyć...
        sRetMsg = sRetMsg & Await ImportMain("http://91.223.13.70/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000")

        Save()

        Return sRetMsg  ' informacje o błędach ze ściągania listy przystanków
    End Function

    ''' <summary>
    ''' ret="" gdy Load, "OK" gdy po Import, "OKxx" gdy cos do pokazania, lub komunikat błędu
    ''' </summary>
    Public Async Function LoadOrImport(bForceLoad As Boolean, bNetAvail As Boolean) As Task
        ' tylko z App

        Dim iHowOld As Integer
        Try ' 20171108: czasem przy starcie wylatuje, może tu?
            Dim iCurrDate As Integer
            If Not Integer.TryParse(System.DateTime.Now.ToString("yyMMdd"), iCurrDate) Then iCurrDate = 0
            iHowOld = iCurrDate - GetSettingsInt("LastLoadStops")
        Catch
            iHowOld = 99
        End Try

        Dim bReaded = False
        If Not bForceLoad Then bReaded = Load()  ' True gdy udane wczytanie; nie ma sensu czytac gdy wymuszamy import

        ' 2019.10.26: gdy lista pusta, to jednak wczytaj...
        If bReaded AndAlso moItemy.Count < 1 Then bReaded = False      ' jak jest puste w ogole
        If bReaded AndAlso GetList().Count < 1 Then bReaded = False    ' jak są puste tramwaje (a autobusy są)

        If Not bForceLoad Then
            If bReaded AndAlso iHowOld < 30 Then Return
        End If

        'fragmenty z import, żeby samo Import mogło być w VBlib
        If Not bNetAvail Then
            Await DialogBoxResAsync("resErrorNoNetwork")
            Return
        End If

        ' kiedys to bylo po testach, teraz - przed wczytaniem
        Dim oItemyOld = moItemy  ' czy to skopiuje zawartosc?

        Dim sMsg As String = Await Import()
        Dim iLastLoad = 0
        If Not Integer.TryParse(System.DateTime.Now.ToString("yyMMdd"), iLastLoad) Then iLastLoad = 0
        SetSettingsInt("LastLoadStops", iLastLoad)

        If GetSettingsBool("pkarmode") Then sMsg &= Compare(oItemyOld, moItemy)
        If sMsg <> "" Then Await DialogBoxAsync(sMsg)
    End Function

    Public Function GetItem(sName As String, Optional sCat As String = "tram") As Przystanek
        For Each oItem In moItemy
            If oItem.Name = sName AndAlso oItem.Cat = sCat Then Return oItem
        Next

        Return Nothing
    End Function

    Public Function GetList(Optional sCat As String = "tram") As List(Of Przystanek)
        Select Case sCat
            Case "all"
                Return moItemy.ToList
            Case "bus"
                ' Return From c In moItemy Where sCat = "bus"
                Return moItemy.Where(Function(s) Equals(s.Cat, "bus")).ToList
            Case Else
                Return moItemy.Where(Function(s) Equals(s.Cat, "tram")).ToList
        End Select
    End Function

    Private Shared Function Compare(oOld As ObjectModel.Collection(Of Przystanek), oNew As ObjectModel.Collection(Of Przystanek)) As String
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

        Return GetLangString("resChangesInStopList") & vbLf & sDiffsDel & vbLf & sDiffsNew
    End Function

    ''' <summary>
    ''' ret="" gdy Error, see sLastError; ret=string - lista opoznien
    ''' </summary>
    Public Async Function OpoznieniaFromHttpAsync(iType As Integer) As Task(Of String)
        ' z Opoznienia.Xaml.cs, 1 raz
        ' policzenie opóźnień, b0 = bus, b1 = tram

        Dim sCat As String

        Select Case iType
            Case 1
                sCat = "tram"
            Case 2
                sCat = "bus"
            Case Else
                ' praktycznie nie ma prawa się zdarzyć
                sLastError = "Bad type of Opoznienia: " & iType
                Return ""
        End Select

        ' policz
        Dim sTxt = ""

        For Each oItem In moItemy
            ' wczytaj dane przystanku
            If oItem.Cat <> sCat Then Continue For
            sTxt = sTxt & oItem.id & vbTab & "Przystanek: " & oItem.Name & vbLf
            Dim iId = 0
            If Not Integer.TryParse(oItem.id, iId) Then iId = 0
            Dim oJson As Newtonsoft.Json.Linq.JObject = Await App.WczytajTabliczke(oItem.Cat, oItem.Name, iId)
            Dim bError = False
            Dim oJsonStops As Newtonsoft.Json.Linq.JArray = New Newtonsoft.Json.Linq.JArray()

            Try
                oJsonStops = CType(oJson("actual"), Newtonsoft.Json.Linq.JArray)
            Catch
                bError = True
            End Try

            If bError Then
                ' bylo zakomentowane przed przenoszeniem do VB
                ' DialogBox("ERROR: JSON ""actual"" array missing")
                Continue For
                ' return false;
            End If

            oItem.iEntriesCount = 0
            oItem.iEntriesTotal = oJsonStops.Count
            oItem.iSumDelay = 0
            oItem.iMaxDelay = 0
            If oJsonStops.Count = 0 Then Continue For

            ' policz...
            For Each oVal As Newtonsoft.Json.Linq.JObject In oJsonStops
                oItem.iEntriesTotal += 1
                ' jesli PREDICTED (a nie np. PLANNED), to znaczy że liczymy
                Dim sPlanTime = "!ERR!"
                Dim sActTime = "!ERR!"
                Dim sTypCzasu = "!ERR!"
                Dim sPattTxt = "!ERR!"
                Dim sDirect = "!error!"

                Try
                    sPlanTime = CStr(oVal("plannedTime"))
                Catch
                End Try

                If String.IsNullOrEmpty(sPlanTime) Then sPlanTime = "!ERR!"
                ' ewentualnie:
                ' sPlanTime &&= "!ERR!"
                ' ale to bedzie tylko dla null, a  nie dla empty

                Try
                    sActTime = oVal("actualTime")
                Catch
                End Try

                If String.IsNullOrEmpty(sActTime) Then sActTime = "!ERR!"

                Try
                    sTypCzasu = oVal("status")
                Catch
                End Try

                If String.IsNullOrEmpty(sTypCzasu) Then sTypCzasu = "!ERR!"

                Try
                    sPattTxt = oVal("patternText")
                Catch
                End Try

                If String.IsNullOrEmpty(sPattTxt) Then sPattTxt = "!ERR!"

                Try
                    sDirect = oVal("direction")
                Catch
                End Try

                If String.IsNullOrEmpty(sDirect) Then sDirect = "!error!"
                sTxt = sTxt & oItem.id & vbTab & sPattTxt & vbTab & sDirect & vbTab & sTypCzasu & vbTab & sPlanTime & vbTab & sActTime

                If Equals(sTypCzasu, "PREDICTED") Then
                    If Not Equals(sPlanTime, "!ERR!") AndAlso Not Equals(sActTime, "!ERR!") Then
                        Dim iAct = 0
                        Dim iPlan = 0

                        Try
                            Dim iMin = 0
                            Dim iHrs = 0
                            If Not Integer.TryParse(sActTime.Substring(0, 2), iHrs) Then iHrs = 0
                            If Not Integer.TryParse(sActTime.Substring(3, 2), iMin) Then iMin = 0
                            iAct = iHrs * 60 + iMin
                            If Not Integer.TryParse(sPlanTime.Substring(0, 2), iHrs) Then iHrs = 0
                            If Integer.TryParse(sPlanTime.Substring(3, 2), iMin) Then iMin = 0
                            iPlan = iHrs * 60 + iMin
                        Catch
                        End Try

                        If iAct > 0 AndAlso iPlan > 0 Then
                            Dim iDelay = iAct - iPlan
                            sTxt = sTxt & vbTab & iDelay
                            oItem.iEntriesCount += 1
                            oItem.iSumDelay = oItem.iSumDelay + iDelay
                            oItem.iMaxDelay = System.Math.Max(oItem.iMaxDelay, iDelay)
                        End If
                    End If
                End If

                sTxt = sTxt & vbLf
            Next

            sTxt = sTxt & vbLf
        Next

        ' ClipPut(sTxt)
        ' sygnalizacja kiedy bylo ostatnie
        ' mdOpoznLastDate = System.DateTime.Now
        Return sTxt
    End Function

    Public Function OpoznieniaGetStat(iType As Integer, ByRef iSumDelay As Integer, ByRef cItems As Integer, ByRef iMaxDelay As Integer) As Boolean
        ' iType: 1: tram, 2:bus
        Dim sCat As String

        Select Case iType
            Case 1
                sCat = "tram"
            Case 2
                sCat = "bus"
            Case Else
                Return False
        End Select

        iSumDelay = moItemy.Where(Function(s) Equals(s.Cat, sCat)).Sum(Function(s) s.iSumDelay)
        cItems = moItemy.Where(Function(s) Equals(s.Cat, sCat)).Sum(Function(s) s.iEntriesCount)
        iMaxDelay = moItemy.Where(Function(s) Equals(s.Cat, sCat)).Max(Function(s) s.iMaxDelay)
        Return True    ' error
    End Function

End Class

