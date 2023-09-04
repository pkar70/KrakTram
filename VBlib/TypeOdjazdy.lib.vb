
Imports System.Net
Imports System.Threading

Partial Public Class JedenOdjazd

    Public Property Odjazd As pkar.MpkWrap.Odjazd
    Public Property isBus As Boolean
    Public Property stopId As String
    Public Property Delay As Integer
    Public Property Odl As Integer
    Public Property bShow As Boolean = True
    'Public Property uiCol1 As Integer
    'Public Property uiCol3 As Integer
    Public Property sPrzystCzas As String
    Public Property bPkarMode As Boolean

    Public Property vehicleType As String
    Public Property vehicleInfoUri As Uri
    Public Property vehicleInwalida As Integer

End Class


Partial Public Class ListaOdjazdow
    Public sLastError As String = ""

    Private moOdjazdy As New List(Of JedenOdjazd)

    Private Shared _pojazdy As pkar.MpkWrap.MpkWrap_Vehicles

    Public Sub New(cacheFolder As String)
        _pojazdy = New pkar.MpkWrap.MpkWrap_Vehicles(cacheFolder)
    End Sub

    Public Function GetItems() As List(Of JedenOdjazd)
        Return moOdjazdy
    End Function

    Public Function Count() As Integer
        Return moOdjazdy.Count
    End Function

    Public Sub Clear()
        If moOdjazdy Is Nothing Then moOdjazdy = New List(Of JedenOdjazd)
        moOdjazdy.Clear()
    End Sub

    Private Shared _nuget As New pkar.MpkWrap.Tabliczka

    Public Async Function WczytajTabliczke(isBus As Boolean, sErrData As String, sId As String, iOdl As Integer, mSpeed As Double, bPkarMode As Boolean) As Task(Of String)
        ' mSpeed = App.mSpeed
        ' Dim bPkarMode As Boolean = p.k.GetSettingsBool("pkarmode", p.k.IsThisMoje())
        'oNew.uiCol1 = GetSettingsInt("widthCol0")  ' linia, typ pojazdu
        'oNew.uiCol3 = GetSettingsInt("widthCol3")  ' czas odjazdu
        'If iCurrSec > iOldSec + 60 * GetSettingsInt("alsoNext", 5) Then


        Dim iMinSec As Integer

        If mSpeed < 1 Then
            iMinSec = 0
        Else
            iMinSec = 3.6 * iOdl / mSpeed
        End If

        If _pojazdy.Count < 1 Then Await _pojazdy.LoadOrImport(False, True)

        Dim nugetOdjazdy As List(Of pkar.MpkWrap.Odjazd) = Await _nuget.WczytajTabliczke(isBus, sId, iMinSec)
        If nugetOdjazdy Is Nothing Then Return "ERROR"

        For Each odjazd As pkar.MpkWrap.Odjazd In nugetOdjazdy
            Dim oNew As New JedenOdjazd
            oNew.Odjazd = odjazd
            oNew.isBus = isBus
            oNew.stopId = sId
            oNew.Odl = iOdl

            If odjazd.vehicleId IsNot Nothing Then
                ' no bo się zdarza że jest NULL!

                Dim danePojazdu As pkar.MpkWrap.VehicleData = _pojazdy.GetItem(odjazd.vehicleId, isBus)
                If danePojazdu Is Nothing Then
                    Await _pojazdy.LoadOrImport(True, True)
                    danePojazdu = _pojazdy.GetItem(odjazd.vehicleId, isBus)
                End If

                If danePojazdu IsNot Nothing Then
                    oNew.vehicleType = SkrocTypPojazdu(isBus, danePojazdu.type)
                    oNew.vehicleInfoUri = GetInfoUri(isBus, oNew.vehicleType)
                    If danePojazdu.low.HasValue Then
                        oNew.vehicleInwalida = danePojazdu.low.Value
                    Else
                        oNew.vehicleInwalida = 0
                    End If
                End If
            End If

            ' tylko to, czego nie zrobił Nuget
            'oNew.Typ = "!ERR!" '  VehicleId2VehicleType(oVal.GetObject().GetNamedString("vehicleId", "!ERR!"));
            'Try
            '    oNew.Typ = CStr(oVal("vehicleId"))
            '    oNew.Typ = VehicleId2VehicleType(oNew.Typ)
            'Catch
            'End Try

            'oNew.uiCol1 = GetSettingsInt("widthCol0") ' linia, typ pojazdu
            'oNew.uiCol3 = GetSettingsInt("widthCol3") ' czas odjazdu
            oNew.sPrzystCzas = $"{oNew.Odjazd.Przyst} ({oNew.Odl} m, {oNew.Odjazd.odlMin} min)"
            oNew.bPkarMode = bPkarMode

            ' oNode.SetAttribute("numer",
            ' oVal.GetObject.GetNamedString("vehicleId", "12345678901234599999").Substring(15))
            ' oNode.SetAttribute("odlSec", iMinSec)

            Dim bBylo = False

            For Each oTmp In moOdjazdy

                If oTmp.Odjazd.Kier = oNew.Odjazd.Kier AndAlso oTmp.Odjazd.Linia = oNew.Odjazd.Linia Then
                    Dim iOldSec = oTmp.Odjazd.TimeSec

                    If oNew.Odjazd.TimeSec > iOldSec + 60 * GetSettingsInt("alsoNext") Then '  GetSettingsInt("alsoNext", 5) 
                        bBylo = True
                        Exit For
                    End If
                End If
            Next

            If Not bBylo Then moOdjazdy.Add(oNew)
        Next

        Return ""   ' wszystko OK
    End Function

    Private Function SkrocTypPojazdu(isBus As Boolean, typek As String) As String

        Dim ret As String = typek

        If isBus Then

            ' Autosan M09LE
            ret = ret.Replace("Autosan", "A")

            'MAN Lion's Intercity 13
            ret = ret.Replace("MAN Lion's Intercity 13", "MAN13")

            'Mercedes Conecto II
            'Mercedes O530 C2 Hybrid
            'Mercedes O530 C2
            ret = ret.Replace("Mercedes", "M").Replace("Hybrid", "H").Replace("Connecto", "C")

            'Solaris Urbino 18 IV Electric
            'Solaris Urbino 18 III Hybrid
            'Solaris Urbino 12 IV
            'Solaris Urbino 12, 9 III Hybrid
            'Solaris Urbino 18 IV
            'Solaris Urbino 8, 9LE Electric
            'Solaris Urbino 12 IV Electric
            'Solaris Urbino 18 III
            'Solaris Urbino 18 MetroStyle
            'Solaris Urbino 12 III
            ret = ret.Replace("Solaris", "S").Replace(" Urbino", "U").Replace("Hybrid", "H").Replace("Electric", "E").Replace("MetroStyle", "M")

            'Volvo 7900A Hybrid
            ret = ret.Replace("Volvo", "V")

        Else
            ' Stadler Tango
            ' Stadler Tango II
            ret = ret.Replace("Stadler", "")
        End If

        ret = ret.Replace(" ", "")

        Return ret
    End Function

    Public Shared Function GetInfoUri(isBus As Boolean, vehicleType As String) As Uri

        If String.IsNullOrWhiteSpace(vehicleType) Then Return Nothing

        ' zob. też SkrocTypPojazdu

        Select Case vehicleType

            ' tramwaje

            Case "EU8N"
                Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie/655-eu8n")
            Case "105N"
                Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie/537-105na")

            Case "N8S"
                Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie/191-n8s")
            Case "N8C"
                Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie/916-n8c")

            ' Stadler Tango
            ' Stadler Tango II
            Case "Tango"
                Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie/3331-stadler-tango-lajkonik")
            Case "TangoII"
                Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie/3331-stadler-tango-lajkonik")

            Case "GT8N"
            Case "GT8C"
            Case "GT8S"
                Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie/233-gt8s-gt8c-gt8n")

            Case "NGT8"
                Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie/109-ngt8")

            Case "NGT6"
            Case "NGT6(3)"
                Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie/547-ngt6")

            Case "2014N"
                Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie/1507-pesa-2014n")

                'autobusy

                ' Autosan M09LE
            Case "AM09LE"
                Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie/255-autosan-sancity-m09le")

                'MAN Lion's Intercity 13
                'Case "MAN13"

                'Mercedes Conecto II
                'Mercedes O530 C2 Hybrid
                'Mercedes O530 C2
                'Case "MCII"
                'Case "M0530C2H"
            Case "M0530C2"
                Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie/2601-mercedes-o530-c2")

                'Solaris Urbino 18 IV Electric
                'Solaris Urbino 18 III Hybrid
                'Solaris Urbino 12 IV
                'Solaris Urbino 12, 9 III Hybrid
                'Solaris Urbino 18 IV
                'Solaris Urbino 8, 9LE Electric
                'Solaris Urbino 12 IV Electric
                'Solaris Urbino 18 III
                'Solaris Urbino 18 MetroStyle
                'Solaris Urbino 12 III
            Case "SU8,9LEE"
                Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie/1614-solaris-urbino-8-9-le-electric")
                'Case "SU9IIIH"
                'Return New Uri("")
                'Case "SU12IIIH"
                'Return New Uri("")
                'Case "SU12III"
                'Return New Uri("")
            Case "SU12IV"
                Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie/2069-solaris-urbino-18-iv")
            Case "SU18IVE"
                Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie/2089-solaris-urbino-18-iv-electric")
                'Case "SU18IIIH"
                'Return New Uri("")
            Case "SU18IV"
                Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie/2069-solaris-urbino-18-iv")
            Case "SU18III"
                Return New Uri("")
            Case "SU18M"
                Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie/1000-solaris-urbino-18-metro-style")
            Case "SU18IVE"
                Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie/2089-solaris-urbino-18-iv-electric")

                'Volvo 7900A Hybrid
            Case "V7900H"
                Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie/2817-volvo-7900-hybrid")
            Case "V7900AH"
                Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie/2473-volvo-7900a-hybrid")

        End Select

        If isBus Then
            Return New Uri("https://psmkms.krakow.pl/autobusy/autobusy-w-krakowie?limitstart=0")
        Else
            Return New Uri("https://psmkms.krakow.pl/tramwaje/w-krakowie")

        End If

    End Function



    Public Sub OdfiltrujMiedzytabliczkowo()
        ' usuwa z oXml to co powinien :) - czyli te same tramwaje z dalszych przystankow
        ' <root><item ..>
        ' o tabliczce: stop, odl, odlMin, odlSec - nazwa, odleglosc: metry, minuty, sec
        ' o tramwaju: line, dir, time, timSec, typ, numer - linia, kierunek, mixedTime, sekundy, typ (eu8n), numer wozu

        For Each oNode In moOdjazdy

            If oNode.bShow Then
                For Each oNode1 In moOdjazdy

                    If oNode.Odjazd.Linia = oNode1.Odjazd.Linia Then
                        If oNode.Odjazd.odlMin < oNode1.Odjazd.odlMin Then oNode1.bShow = False
                    End If
                Next
            End If
        Next
    End Sub

    Public Sub FiltrWedleKierunku(ByVal bExclude As Boolean, ByVal sKier As String)
        ' bExclude = True, usun ten kierunek
        ' = False, tylko ten kierunek

        For Each oNode In moOdjazdy
            If bExclude AndAlso oNode.Odjazd.Kier = sKier Then oNode.bShow = False
            If Not bExclude AndAlso oNode.Odjazd.Kier <> sKier Then oNode.bShow = False
        Next
    End Sub

    Public Async Function GetDelayStats(bBus As Boolean, sId As String) As Task(Of pkar.MpkWrap.OpoznieniaStat)
        Return Await _nuget.GetDelayStats(bBus, sId)
    End Function

End Class
