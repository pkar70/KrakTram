
Imports pkar

Public Class proba

    ''' <summary>
    ''' Return GeoCenter point for list of locations (using Latitude, Longitude and Altitude)
    ''' </summary>
    Public Shared Function GetCenter(locations As List(Of BasicGeopos)) As BasicGeopos
        Return GetCornersAndCenter(locations).Item(2)
    End Function

    ''' <summary>
    ''' return NorthWest and SouthEast corners for list of locations. Also return altitude minimum (in NW) and maximum (in SE). It can be used for creating UWP GeoboundingBox.
    ''' </summary>
    Public Shared Function GetCorners(locations As List(Of BasicGeopos)) As List(Of BasicGeopos)

        Dim oSE As New BasicGeopos(90, -180, -6378000)
        Dim oNW As New BasicGeopos(-90, 360, 100000)

        For Each loc As BasicGeopos In locations
            oNW.Altitude = Math.Min(oNW.Altitude, loc.Altitude)
            oNW.Latitude = Math.Max(oNW.Latitude, loc.Latitude)
            oNW.Longitude = Math.Min(oNW.Longitude, loc.Longitude)

            oSE.Altitude = Math.Max(oSE.Altitude, loc.Altitude)
            oSE.Latitude = Math.Min(oSE.Latitude, loc.Latitude)
            oSE.Longitude = Math.Max(oSE.Longitude, loc.Longitude)

        Next

        Return New List(Of BasicGeopos) From {oNW, oSE}

    End Function

    ''' <summary>
    ''' return NorthWest, SouthEast corners and center point for list of locations. Also return altitude minimum (in NW) and maximum (in SE)
    ''' </summary>
    Public Shared Function GetCornersAndCenter(locations As List(Of BasicGeopos)) As List(Of BasicGeopos)

        Dim corners As List(Of BasicGeopos) = GetCorners(locations)
        Dim GeoNW As BasicGeopos = corners.Item(0)
        Dim GeoSE As BasicGeopos = corners.Item(1)

        Dim GeoCenter As New BasicGeopos(
            GeoSE.Latitude + (GeoNW.Latitude - GeoSE.Latitude) / 2,
            GeoNW.Longitude + (GeoSE.Longitude - GeoNW.Longitude) / 2,
            GeoNW.Altitude + (GeoSE.Altitude - GeoNW.Altitude) / 2)


        Return New List(Of BasicGeopos) From {GeoNW, GeoSE, GeoCenter}

    End Function

End Class

#If PRENUGET Then

Public Class App

    Public Shared sLastError As String = ""


    Public Shared Async Function WczytajTabliczke(sCat As String, sErrData As String, iId As Integer) As Task(Of Newtonsoft.Json.Linq.JObject)
        Dim sUrl As String

        If Equals(If(sCat, ""), "bus") Then
            sUrl = "http://91.223.13.70"
        Else
            sUrl = "http://www.ttss.krakow.pl"
        End If

        sUrl = sUrl & "/internetservice/services/passageInfo/stopPassages/stop?mode=departure&stop="
        Dim sPage As String = Await WebPageAsync(sUrl & iId.ToString(), sErrData)
        If sPage = "" Then Return Nothing

        Dim oJson As Newtonsoft.Json.Linq.JObject

        Try
            oJson = Newtonsoft.Json.Linq.JObject.Parse(sPage)
        Catch
            sLastError = "ERROR: JSON parsing error - tablica in " & sErrData
            Return Nothing
        End Try

        Return oJson
    End Function

    Public Shared Async Function WebPageAsync(sUri As String, sErrData As String) As Task(Of String)

        ' string sTmp = "";

        'If Not Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
        '    p.k.DialogBoxRes("resErrorNoNetwork", sErrData)
        '    Return ""
        'End If

        Dim bError = False
        Dim sPage = ""


        Using oHttp As New Net.Http.HttpClient

            oHttp.Timeout = TimeSpan.FromSeconds(8)

            Try
                sPage = Await oHttp.GetStringAsync(New Uri(sUri))
            Catch
                bError = True
            End Try

            oHttp.Dispose()
        End Using

        If bError Then
            sLastError = GetLangString("resErrorGetHttp") & vbCrLf & sErrData
            Return ""
        End If

        Return sPage
    End Function

End Class

#End If