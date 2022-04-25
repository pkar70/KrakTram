
Namespace Global

    Public Class BliskiStop
        Public Property sNazwa As String
        Public Property sDane As String
        Public Property iOdl As Integer
    End Class

End Namespace
Public Class Setup

    Public Shared Function ConvertGpsPrecFromAndroid(iSlider As Integer, bUWP As Boolean) As Integer
        If bUWP Then Return iSlider
        If iSlider < 2 Then Return 75
        If iSlider > 2 Then Return 600
        Return 200
    End Function

    Public Shared Function ConvertGpsPrecToAndroid(iMetr As Integer, bUWP As Boolean) As Integer
        If bUWP Then Return iMetr
        If iMetr < 100 Then Return 1
        If iMetr > 500 Then Return 3
        Return 2
    End Function



    Public Shared Function ListaBliskichPrzystankowListView(dLat As Double, dLon As Double, dMaxOdl As Double, dWalkSpeed As Double, olStops As List(Of Przystanek)) As ObjectModel.Collection(Of BliskiStop)
        Dim iOdl As Integer

        Dim iMinOdl = 100000
        Dim oItemy As ObjectModel.Collection(Of BliskiStop) = New ObjectModel.Collection(Of BliskiStop)()

        For Each oNode As VBlib.Przystanek In olStops
            iOdl = GPSDistance(dLat, dLon, oNode.Lat, oNode.Lon)

            If iOdl < dMaxOdl Then
                Dim oNew As BliskiStop = New BliskiStop()
                oNew.sNazwa = oNode.Name
                oNew.iOdl = iOdl

                If GetSettingsBool("settingsAlsoBus") Then
                    If oNode.Cat = "bus" Then
                        oNew.sNazwa += " (A)"
                    Else
                        oNew.sNazwa += " (T)"
                    End If
                ElseIf oNode.Cat = "bus" Then
                    Continue For
                End If   ' dla tramwajów - tylko tramwajowe ma pokazywać

                oNew.sDane = iOdl & " m (" & CInt(60 * iOdl / (dWalkSpeed * 1000)) & " min)"
                oItemy.Add(oNew)
            End If

            iMinOdl = Math.Min(iMinOdl, iOdl)
        Next

        If oItemy.Count < 1 Then
            Dim oNew As BliskiStop = New BliskiStop()
            Dim sMsg As String = GetLangString("resNearestStop")

            If iMinOdl < 10000 Then
                sMsg = sMsg.Replace("###", iMinOdl.ToString() & " m")
            Else
                sMsg = sMsg.Replace("###", iMinOdl / 1000 & " km")
            End If

            oNew.sNazwa = sMsg
            oItemy.Add(oNew)
        End If

        Return oItemy
    End Function


End Class
