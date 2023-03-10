
Imports pkar.MpkMain

Namespace MpkWrap


    Public Class MpkWrap_TrasaMapa

        Private Shared oMpk As New MpkMain.MPK

        Public Async Function GetTrasaNaMapieVehicle(isBus As Boolean, vehicleId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of TrasaNaMape))
            Dim temp As MpkMain.MpkPathInfo = Await oMpk.DownloadTrasaNaMapieVehicle(isBus, vehicleId, iTimeoutSecs)
            If temp Is Nothing Then Return Nothing

            Return ConvertPathToMapa(temp)
        End Function

        Public Async Function GetTrasaNaMapieRoute(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of TrasaNaMape))
            Dim temp As MpkMain.MpkPathInfo = Await oMpk.DownloadTrasaNaMapieRoute(isBus, routeId, iTimeoutSecs)
            If temp Is Nothing Then Return Nothing

            Return ConvertPathToMapa(temp)
        End Function

        Private Function ConvertPathToMapa(pathInfo As MpkPathInfo) As List(Of TrasaNaMape)

            Dim ret As New List(Of TrasaNaMape)

            For Each oPath In pathInfo.paths
                Dim oTrasa As New TrasaNaMape
                oTrasa.color = oPath.color
                oTrasa.wayPoints = New List(Of BasicGeopos)

                For Each waypoint As MpkMain.MpkWayPoint In From c In oPath.wayPoints Order By c.seq
                    oTrasa.wayPoints.Add(Przystanek.BasicGeoposFromMpk(waypoint.lat, waypoint.lon))
                Next
            Next

            Return ret
        End Function
    End Class


    Public Class TrasaNaMape
        Inherits pkar.BaseStruct

        Public Property color As String
        Public Property wayPoints As List(Of BasicGeopos)
    End Class

End Namespace

