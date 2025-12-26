
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

            ' jeśli nie mamy czego szukać, to nie szukamy
            If String.IsNullOrWhiteSpace(vehicleId) Then
                Return New VehicleData With {.type = "", .low = 0, .num = "???"}
            End If

            If vehicleId.Length > 7 Then
                ' dotychczasowy sposób - z długimi ID
                Dim key As String = If(isBus, "b", "t") & vehicleId
                If Not ContainsKey(key) Then Return Nothing
                Return Item(key)
            End If

            ' nowy sposób - mając nr boczny.
            ' nie ma dostępu do wnętrza słownika niestety, więc nie będzie na 100 %
            Select Case vehicleId.Substring(1, 1)
                Case "A"
                    Return New VehicleData With {.type = "Autosan M09LE", .low = 2, .num = vehicleId}
                Case "C"
                    Return New VehicleData With {.type = "Mercedes Conecto G", .low = 2, .num = vehicleId}
                Case "E"
                    If "DE637 DE638 DE639 DE640".Contains(vehicleId) Then
                        Return New VehicleData With {.type = "Irizar ie bus 12", .low = 2, .num = vehicleId}
                    End If
                    ' uwaga! tu też są DE63!
                    Return New VehicleData With {.type = "Solaris Urbino 12 IV Electric", .low = 2, .num = vehicleId}
                Case "F"
                    If "RF310 RF311 RF312 RF313".Contains(vehicleId) Then
                        Return New VehicleData With {.type = "GT8C", .low = 1, .num = vehicleId}
                    End If
                    ' uwaga! tu też są RF31!
                    Return New VehicleData With {.type = "GT8N", .low = 1, .num = vehicleId}

                Case "G"
                    If vehicleId = "HG999" Then
                        Return New VehicleData With {.type = "405N", .low = 1, .num = vehicleId}
                    End If
                    Return New VehicleData With {.type = "2014N", .low = 2, .num = vehicleId}
                Case "H"
                    If "BH421 BH420 BH419 BH418 BH416 BH412 BH410 BH415 BH413 BH417 BH411".Contains(vehicleId) Then
                        Return New VehicleData With {.type = "Solaris Urbino 18 III Hybrid", .low = 2, .num = vehicleId}
                    End If
                    Return New VehicleData With {.type = "Volvo 7900A Hybrid", .low = 2, .num = vehicleId}
                Case "K"
                    If "HK460 HK461 HK459".Contains(vehicleId) Then
                        Return New VehicleData With {.type = "N8S-NF", .low = 1, .num = vehicleId}
                    End If
                    Return New VehicleData With {.type = "N8C-NF", .low = 1, .num = vehicleId}
                Case "L"
                    Return New VehicleData With {.type = "EU8N", .low = 1, .num = vehicleId}
                Case "M"
                    Return New VehicleData With {.type = "MAN Lion's Intercity 13", .low = 2, .num = vehicleId}
                Case "N"
                    Return New VehicleData With {.type = "Solaris Urbino 18 IV Electric", .low = 2, .num = vehicleId}
                Case "O"
                    If vehicleId = "DO201" Then
                        Return New VehicleData With {.type = "Mercedes Conecto II", .low = 2, .num = vehicleId}
                    End If
                    If vehicleId.StartsWith("PO") Then
                        Return New VehicleData With {.type = "Mercedes O530 C2 Hybrid", .low = 2, .num = vehicleId}
                    End If
                    Return New VehicleData With {.type = "Mercedes O530 C2", .low = 2, .num = vehicleId}
                Case "P"
                    ' są tu (1), (2), (3) - pewnie różne dostawy
                    Return New VehicleData With {.type = "NGT6", .low = 2, .num = vehicleId}
                Case "R"
                    Return New VehicleData With {.type = "Solaris Urbino 18 IV", .low = 2, .num = vehicleId}
                Case "U"
                    Return New VehicleData With {.type = "Solaris Urbino 12 IV", .low = 2, .num = vehicleId}
                Case "W"
                    Return New VehicleData With {.type = "E1", .low = 0, .num = vehicleId}
                Case "Y"
                    If vehicleId = "RY899" Then
                        Return New VehicleData With {.type = "126N", .low = 2, .num = vehicleId}
                    End If
                    If "RY804 RY812 RY818 RY813 RY821 RY823 RY811 RY807 RY819 RY824 RY805 RY806 RY802 RY801 RY815 RY809 RY810 RY817 RY822 RY820 RY816 RY808 RY803 RY814".Contains(vehicleId) Then
                        Return New VehicleData With {.type = "NGT8", .low = 2, .num = vehicleId}
                    End If
                    If "HY866 HY859 HY857 HY853 HY842 RY837 RY835 RY825 RY827 RY833 RY828 HY870 RY832 HY872 HY856 HY864 HY851 HY852 HY869 HY848 HY854 HY868 HY840 HY865 RY826 RY839 HY841 HY867 HY873 HY858 HY871 RY829 HY850 HY845 HY847 HY844 RY838 HY846 RY834 HY860 RY836 HY861 HY874 HY862 HY855 HY849 HY843 RY831 RY830 HY863".Contains(vehicleId) Then
                        Return New VehicleData With {.type = "Stadler Tango", .low = 2, .num = vehicleId}
                    End If
                    Return New VehicleData With {.type = "Stadler Tango II", .low = 2, .num = vehicleId}

                Case "Z"
                    Return New VehicleData With {.type = "105N", .low = 0, .num = vehicleId}
            End Select

            Return New VehicleData With {.type = "(" & vehicleId & ")", .low = 0, .num = vehicleId}

        End Function


    End Class

    Public Class VehicleData
        Inherits pkar.BaseStruct

        Public Property num As String
        Public Property type As String
        Public Property low As Nullable(Of Integer)
    End Class

End Namespace
