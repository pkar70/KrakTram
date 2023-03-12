﻿
Nuget, 'wrappujący' dostęp do danych MPK.

Dwa główne poziomy:
1) MpkMain: bezpośredni dostęp do serwerów MPK, i ichniejszy format danych
2) MpkWrapper: własny format danych, cache'owanie

# MpkMain

        Public Property UriTram As String = "http://www.ttss.krakow.pl/"
        Public Property UriBus As String = "http://91.223.13.70/"
        Public Function GetUriBase(isBus As Boolean) As String
        Public Property errMessage As String = ""
        Public Async Function DownloadListaPrzystankowAsync(Optional bTram As Boolean = True, Optional bBus As Boolean = True, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of MpkPrzystanek))
        Public Async Function WczytajTabliczkeAsync(isBus As Boolean, sId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkTabliczka)
        Public Async Function DownloadDalszaTrasaAsync(isBus As Boolean, tripId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkDalszaTrasa)
        Public Async Function DownloadTrasaLiniiAsync(linia As String, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of String))
        Public Async Function DownloadZmianyAsync(Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of MpkZmiana))
        Public Async Function DownloadVehiclesState(isBus As Boolean, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkVehiclesState)
        Public Async Function DownloadTrasaNaMapieVehicle(isBus As Boolean, sId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo)
        Public Async Function DownloadTrasaNaMapieRoute(isBus As Boolean, sId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkPathInfo)
        Public Async Function DownloadRouteTrasaAsync(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkRouteStops)

 oraz wewnętrzna funkcja, dla wygody upubliczniona:

        Public Async Function ReadRest(isBus As Boolean, sCmd As String, Optional iTimeoutSecs As Integer = 10) As Task(Of String)

## struktury danych

Klasy te odpowiadają temu co zwracane jest przez serwery

        Public Class MpkPrzystanek
        Public Class MpkTabliczka
        Public Class MpkOdjazd
        Public Class MpkRoute
        Public Class MpkDalszaTrasa
        Public Class MpkDalszyStop
        Public Class MpkDalszyStop_Stop
        Public Class MpkZmiana
        Public Class MpkVehiclesState
        Public Class MpkVehicleState
        Public Class MpkVehicleStatePath
        Public Class MpkWayPoint
        Public Class MpkPath
        Public Class MpkPathInfo
        Public Class MpkRouteStops
        Public Class MpkRouteStop

# MpkWrap

 Wszystkie klasy wykorzystują MpkMain jako mechanizm dostępu do serwerów. Dane są (zwykle) cache'owane.

## Przystanki

 Własny format informacji o przystankach, zapisywanie do JSON o nazwie "stops2.json"

    Public Class Przystanek
        Public Property IsBus As Boolean
        Public Property Geo As BasicGeopos
        Public Property Name As String
        Public Property id As String
        Public Sub New(Optional wersjaMPK As MpkMain.MpkPrzystanek = Nothing)

    Public Class Przystanki
        Public Sub New(sFolder As String, Optional maxDaysCache As Integer = 30)
        Public Property ZmianyPrzystankow As String = ""
        Public Async Function LoadOrImport(bForceLoad As Boolean, bNetAvail As Boolean, Optional bDoCompare As Boolean = False) As Task(Of Boolean)
        Public Function GetItem(sName As String, Optional isBus As Boolean = False) As Przystanek
        Public Overloads Function GetList(Optional sCat As String = "tram") As List(Of Przystanek)


## Tabliczka

 Dane nie są cache'owane, zwracana jest lista odjazdów (własny format)

    Public Class Tabliczka
        Public Async Function WczytajTabliczke(bBus As Boolean, sId As String, iMinSec As Integer, Optional HttpTimeoutSecs As Integer = 10) As Task(Of List(Of Odjazd))
        Public Async Function GetDelayStats(bBus As Boolean, sId As String) As Task(Of OpoznieniaStat)
        Public Property LastStat As OpoznieniaStat  // wypełniane przez WczytajTabliczkę

    Public Class Odjazd
        Public Property Linia As String
        Public Property iLinia As Integer
        Public Property Kier As String
        Public Property Przyst As String
        Public Property Mins As String
        Public Property PlanTime As String
        Public Property ActTime As String
        Public Property tripId As String
        Public Property TimeSec As Integer
        Public Property odlMin As Integer
        Public Property sRawData As String

    Public Class OpoznieniaStat
        Public Property itemsCount As Integer
        Public Property noRealTimeCount As Integer
        Public Property onTimeCount As Integer
        Public Property DelayMin As Integer
        Public Property DelayMax As Integer
        Public Property DelayAvg As Integer
        Public Property DelaySum As Integer
        Public Property DelayMedian As Integer



## Dalsze przystanki na trasie

 Dane nie są cache'owane, zwracana jest lista odjazdów (własny format)

    Public Class DalszaTrip
        Public Async Function GetTrasa(bBus As Boolean, tripId As String) As Task(Of List(Of DalszyStop))

    Public Class DalszyStop
        Public Property actualTime As String
        Public Property status As String
        Public Property name As String
        Public Property shortName As String
        Public Sub New(dalszyMpk As MpkMain.MpkDalszyStop)


## Przystanki na linii

 Dane są cache'owane w pliku "liniaXX.json", jest to właściwie lista stringów.

    Public Class Linia
        Public Sub New(sFolder As String, sLinia As String, Optional maxDaysCache As Integer = 30)
        Public Shared Function NazwaBezSlupka(sNazwaZeSlupkiem As String) As String
        Public Async Function LoadOrImport(bForceLoad As Boolean, bNetAvail As Boolean) As Task(Of String)

## Trasa pojazdu / route w wersji dla mapy

 Bez cache danych.

    Public Async Function GetTrasaNaMapieVehicle(isBus As Boolean, vehicleId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of TrasaNaMape))
    Public Async Function GetTrasaNaMapieRoute(isBus As Boolean, routeId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of TrasaNaMape))

    Public Class TrasaNaMape
        Public Property color As String
        Public Property wayPoints As List(Of BasicGeopos)

## Lista zmian (objazdy)

 Dane są cache'owane w pliku "zmiany.json".

    Public Class Zmiany
        Public Sub New(sFolder As String, Optional maxDaysCache As Integer = 7)
        Public Async Function LoadOrImport(bForceLoad As Boolean, bNetAvail As Boolean) As Task(Of Boolean)

# vehicles

 Dane są cache'owalne w pliku 'vehicles.json'. Jest to wrapper dla danych udostępnianych na stronie https://mpk.jacekk.net/vehicles/ 

      Public Class VehiclesData
            Public Sub New(sFolder As String)
            Public Async Function LoadOrImport(bForceLoad As Boolean, bNetAvail As Boolean) As Task(Of Boolean)
            Public Function GetItem(sName As String, Optional isBus As Boolean = False) As VehicleData


# Przykładowe URL:

Przystanki:
http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000

Tramwaj Dauna:
http://www.ttss.krakow.pl/internetservice/services/passageInfo/stopPassages/stop?mode=departure&stop=632

Trip tramwaju:
http://www.ttss.krakow.pl/internetservice/services/tripInfo/tripPassages?tripId=6351558574044469265
&mode=arrival|departure

Przystanki na route:
http://www.ttss.krakow.pl/internetservice/services/routeInfo/routeStops?routeId=8059228650286874683

Trasa na mapie
http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/pathinfo/vehicle?id=-1188950295991395638
http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/pathinfo/route?id=8059228650286874687

Położenie pojazdów:
http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/vehicleinfo/vehicles

Przystanki na linii:
https://rozklady.mpk.krakow.pl/?lang=PL&linia=52

Zmiany tras:
https://ztp.krakow.pl/transport-publiczny/komunikacja-miejska/komunikaty


