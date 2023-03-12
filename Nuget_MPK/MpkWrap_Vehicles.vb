
Namespace MpkWrap

    Public Class MpkWrap_Vehicles
        Inherits BaseDict(Of String, VehicleData)

        Public Sub New(cacheFolder As String)
            MyBase.New(cacheFolder, "vehiclesDict.json")
        End Sub

        Public Async Function LoadOrImport(bForceLoad As Boolean, bNetAvail As Boolean) As Task(Of Boolean)

            Dim bReaded = False

            If Not bForceLoad Then bReaded = Load()  ' True gdy udane wczytanie; nie ma sensu czytac gdy wymuszamy import

            If bReaded AndAlso Count() < 1 Then bReaded = False      ' jak jest puste w ogole

            If bReaded Then
                If Not bForceLoad Then Return True
                If Not bNetAvail Then Return True
            End If
            If Not bNetAvail Then Return False

            Dim oHttp = New Net.Http.HttpClient
            Dim sPage As String

            Try
                sPage = Await oHttp.GetStringAsync("https://mpk.jacekk.net/vehicles/")
            Catch
                Return False
            End Try

            If Not Import(sPage) Then Return False

            Save(True)

            Return True

        End Function
        Public Function GetItem(vehicleId As String, isBus As Boolean) As VehicleData
            Dim key As String = If(isBus, "b", "t") & vehicleId
            If Not ContainsKey(key) Then Return Nothing
            Return Item(key)
        End Function

    End Class


    Public Class VehicleData
        Inherits pkar.BaseStruct

        Public Property num As String
        Public Property type As String
        Public Property low As Nullable(Of Integer)
    End Class

End Namespace
