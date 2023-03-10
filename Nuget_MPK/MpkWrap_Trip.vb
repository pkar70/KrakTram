

Namespace MpkWrap

    Public Class DalszaTrip

        Private Shared oMpk As New MpkMain.MPK

        Public Async Function GetTrasa(bBus As Boolean, tripId As String) As Task(Of List(Of DalszyStop))

            Dim dalszaTrasa As MpkMain.MpkDalszaTrasa = Await oMpk.DownloadDalszaTrasaAsync(bBus, tripId)
            If dalszaTrasa Is Nothing Then Return Nothing

            Dim ret As New List(Of DalszyStop)
            For Each oItem As MpkMain.MpkDalszyStop In dalszaTrasa.actual
                ret.Add(New DalszyStop(oItem))
            Next

            Return ret
        End Function

    End Class


    Public Class DalszyStop
        Inherits pkar.BaseStruct

        Public Property actualTime As String
        Public Property status As String
        Public Property name As String
        Public Property shortName As String

        Public Sub New(dalszyMpk As MpkMain.MpkDalszyStop)
            actualTime = dalszyMpk.actualTime
            status = dalszyMpk.status
            name = dalszyMpk.stop.name
            shortName = dalszyMpk.stop.shortName
        End Sub
    End Class

End Namespace
