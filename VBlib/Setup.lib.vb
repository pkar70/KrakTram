

Public Class BliskiStop
        Public Property sNazwa As String
        Public Property sDane As String
        Public Property iOdl As Integer
    End Class

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


    Public Shared Function ListaBliskichPrzystankowListView(oPos As pkar.BasicGeopos, dMaxOdl As Double, dWalkSpeed As Double, olStops As List(Of pkar.MpkWrap.Przystanek)) As List(Of BliskiStop)
        Dim iOdl As Integer

        Dim iMinOdl = 100000
        Dim oItemy As New List(Of BliskiStop)

        For Each oNode As pkar.MpkWrap.Przystanek In olStops
            iOdl = oPos.DistanceTo(oNode.Geo)

            If iOdl < dMaxOdl Then
                Dim oNew As New BliskiStop With {
                    .sNazwa = oNode.Name,
                    .iOdl = iOdl
                }

                oNew.sNazwa += If(oNode.IsBus, " (A)", " (T)")

                oNew.sDane = iOdl & " m (" & CInt(60 * iOdl / (dWalkSpeed * 1000)) & " min)"
                oItemy.Add(oNew)
            End If

            iMinOdl = Math.Min(iMinOdl, iOdl)
        Next

        If oItemy.Count < 1 Then
            Dim oNew As New BliskiStop()
            Dim sMsg As String = pkar.Localize.GetResManString("resNearestStop")

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
