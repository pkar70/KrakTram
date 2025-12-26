Imports System.Reflection
Imports Newtonsoft.Json.Linq
Imports pkar.DotNetExtensions
Imports pkar.MpkWrap

Namespace MpkMain

    Public Class MPK_Amistad
        Inherits MPK_Common

        Protected Const URI_BASE As String = "https://services.mpk.amistad.pl/mpk/"

        ' tylko z MpkWrap_Przystanki, bez parametrów
        Public Shared Async Function DownloadListaPrzystankowAsync(Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of Przystanek))

            If _amistops.Count < 1 Then
                ' wczytaj jak nie ma - bo może osobno czytanie tram, osobno bus - ale nie u mnie :)
                Dim stopsJson As String = Await GetCborAsString("schedule/stops")
                _amistops.Import(stopsJson)
            End If

            Dim ret As New List(Of Przystanek)
            Dim oNew As New Przystanek

            ' założenie: JSON jest alfabetycznie ułożony
            For Each oAmiStop In _amistops
                Dim bezSlupka As String = MpkWrap.Linia.NazwaBezSlupka(oAmiStop.name)

                If oAmiStop.positions.Count < 1 Then
                    Debug.WriteLine($"Nie mam geo przy {oAmiStop.name} (id: {oAmiStop.id}")
                    Continue For
                End If

                If oNew.Name = bezSlupka Then
                    ' jeśli ta sama nazwa, to dodajemy ID
                    oNew.Ami_Ids &= "|" & oAmiStop.id
                    oNew.Ami_Count += 1
                    Continue For
                End If

                ' jeśli nowa nazwa, to
                ' dodajemy poprzednio utworzony - gdy nie pusty (znaczy nie pierwsza iteracja)
                If Not String.IsNullOrEmpty(oNew.Name) Then ret.Add(oNew)
                ' tworzymy nowy oNew

                oNew = New Przystanek With {
                        .Name = bezSlupka,
                        .id = oAmiStop.id,
                        .Ami_Ids = "|" & oAmiStop.id,
                        .Ami_Count = 1,
                        .IsBus = "true",
                .Geo = New BasicGeopos(oAmiStop.positions(0).lat, oAmiStop.positions(0).lng),
                        .Ami_AlsoTram = CzyTakzeTram(bezSlupka)}

            Next

            Return ret
        End Function


        Public Shared Async Function WczytajTabliczkeAsync(isBus As Boolean, sId As String, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkTabliczka)
            ' wczytanie tabliczki z ID (czyli konkretny słupek)

            ' a nieprawda, nie ma CBOR, tylko JSON, i to chyba tylko rozkładowy czas - Ami_Schedule_Stop
            ' https://services.mpk.amistad.pl/mpk/schedule/stop/1458
            Dim departuresyText As String = Await GetCborAsString($"schedule/stop/{sId}", iTimeoutSecs)
            Dim departuresy As New BaseList(Of Ami_Departure)("")
            departuresy.Import(departuresyText)

        End Function

        Public Shared Async Function WczytajTabliczkeAsync(stopek As Przystanek, Optional iTimeoutSecs As Integer = 10) As Task(Of MpkTabliczka)
            ' wczytanie tabliczek z ID branymi z przystanku

            ' jeśli nie mamy IDsów, to może przynajmniej jeden ocalał - wczytujemy ten jeden
            If String.IsNullOrEmpty(stopek.Ami_Ids) Then
                ' i nie wiemy czemu jest takie wywołanie, więc na wszelki wypadek TRAM i BUS osobno
                Dim ret As MpkTabliczka = Await WczytajTabliczkeAsync(False, stopek.id)
                If ret IsNot Nothing AndAlso ret.actual.Count > 0 Then Return ret
                Return Await WczytajTabliczkeAsync(True, stopek.id)
            End If

            Dim retSum As New MpkTabliczka With {.stopName = stopek.Name}
            retSum.actual = New List(Of MpkOdjazd)

            For Each stopid As String In stopek.Ami_Ids.Split("|")
                ' czyli idziemy wg Ami a ten ma tabliczkę bez rozdziału bus/tram
                Dim ret As MpkTabliczka = Await WczytajTabliczkeAsync(False, stopid.Replace("|", "").Trim)
                ' dodawanie
            Next

            Return retSum

        End Function

        Public Shared Async Function DownloadTrasaLiniiAsync(linia As String, Optional iTimeoutSecs As Integer = 10) As Task(Of List(Of String))
            errMessage = ""

            Dim oUri As New Uri("https://services.mpk.amistad.pl/mpk/schedule/variantGroup/" & linia & "-1")

            Dim sPage As String = Await oHttp.GetStringAsync(oUri)

            Dim oLiniaStops As MpkLiniaStops
            Try
                oLiniaStops = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(MpkLiniaStops))
            Catch ex As Exception
                Return Nothing
            End Try

            Dim stopy As New List(Of String)

            For Each oPrzyst As MpkLiniaElementInside In oLiniaStops.elements.Select(Function(e) e.element)
                Dim nazwa As String = oPrzyst.name
                If oPrzyst.onDemand Then nazwa &= " (n/ż)"
                stopy.Add(nazwa)
            Next

            Return stopy
        End Function


        Private Shared Async Function GetCborAsString(urik As String, Optional iTimeoutSecs As Integer = 10) As Task(Of String)
            If oHttp.Timeout.TotalSeconds <> iTimeoutSecs Then
                oHttp = New Net.Http.HttpClient
                oHttp.Timeout = TimeSpan.FromSeconds(iTimeoutSecs)
            End If

            ' Pobierz CBOR jako bajty
            Dim data As Byte() = Await oHttp.GetByteArrayAsync(URI_BASE & urik)

            ' Zdekoduj CBOR → obiekt CBORObject
            Dim cbor = PeterO.Cbor.CBORObject.DecodeFromBytes(data)

            ' Konwersja do JSON
            Dim json As String = cbor.ToJSONString()

            Return json

        End Function

        Private Shared _amistops As New BaseList(Of Ami_Stop)("")

        Protected Class Ami_Stop
            Public Property id As Integer
            Public Property name As String
            Public Property positions() As Ami_Position()
            Public Property street As String
            ' Public Property city As String ' i tak jest zawsze puste, także dla Wieliczka 
        End Class

        Protected Class Ami_Position
            Public Property lat As Single
            Public Property lng As Single
        End Class

        Private Shared Function CzyTakzeTram(name As String) As Boolean
            Return TRAM_STOPS.ContainsCI("|" & name & "|")
        End Function

        ' skrócona wersja wczytywania megabajtów GTFS i ichniejszej analizy
        Private Const TRAM_STOPS As String = "|Łagiewniki|Łagiewniki SKA|Łagiewniki ZUS|Św. Gertrudy|Św. Wawrzyńca|Ćwiklińskiej|AKF / PK|Bardosa|Białoprądnicka|Białucha|Bieżanowska|Bieńczycka|Biprostal|Bociana|Borek Fałęcki|Borsucza|Brama nr 4 (nż)|Brama nr 5 (nż)|Bratysławska|Brożka (nż)|Bronowice|Bronowice Małe|Bronowice SKA|Cienista|Cmentarz Podgórski|Cmentarz Rakowicki|Cystersów|Czerwone Maki P+R|Czyżyny|Dąbie|Darwina|Dauna|DH Wanda|Dunikowskiego|Dworcowa|Dworzec Główny Tunel|Dworzec Główny Zachód|Dworzec Płaszów Estakada|Dworzec Towarowy|Elektromontaż (nż)|Fabryczna|Filharmonia|Fort Mogiła (nż)|Górka Narodowa P+R|Górnickiego|Głowackiego|Gałczyńskiego|Giedroycia (nż)|Grodzki Urząd Pracy|Gromadzka|Grota-Roweckiego|Hala Targowa|Jarzębiny|Jubilat|Kabel|Kampus UJ|Kampus UP JP II|Kapelanka|Klasztorna|Kleeberga|Klimeckiego|Kościuszkowców|Kobierzyńska|Koksochemia (nż)|Kombinat|Komorowskiego|Kopiec Wandy|Korona|Krowodrza Górka P+R|Kuźnicy Kołłątajowskiej|Kuklińskiego|Kurdwanów P+R|Limanowskiego|Lipińskiego|Lipska|Lubicz|Mały Płaszów P+R|Meksyk (nż)|Miśnieńska|Miodowa|Muzeum Fotografii|Muzeum Lotnictwa Polskiego|Norymberska|Nowosądecka|Nowy Bieżanów P+R|Nowy Kleparz|Nowy Prokocim|Nullo|Ofiar Dąbia|Ogród Doświadczeń|Orzeszkowej|Os. Kolorowe|Os. Na Skarpie|Os. Piastów|Os. Złotego Wieku|Os. Zgody|Pędzichów|Pachońskiego P+R|Papierni Prądnickich|PH|Piaski Nowe|Piasta Kołodzieja|Plac Bohaterów Getta|Plac Centralny im. R.Reagana|Plac Inwalidów|Plac Wolnica|Plac Wszystkich Świętych|Pleszów|Poczta Główna|Podgórze SKA|Politechnika|Prokocim|Prokocim Szpital|PT|Rondo 308. Dywizjonu|Rondo Czyżyńskie|Rondo Grunwaldzkie|Rondo Grzegórzeckie|Rondo Hipokratesa|Rondo Kocmyrzowskie im. Ks. Gorzelanego|Rondo Matecznego|Rondo Mogilskie|Rondo Piastowskie|Ruczaj|Rzebika|Rzemieślnicza|Słomiana|Salwator|Sanktuarium Bożego Miłosierdzia|Siewna Wiadukt|Smolki|Solvay|Starowiślna|Stary Kleparz|Stefana Batorego|Stella-Sawickiego|Stradom|Struga|Suche Stawy|Szpital Narutowicza|Szwedzka|TAURON Arena Kraków al. Pokoju|TAURON Arena Kraków Wieczysta|Teatr Bagatela|Teatr Ludowy|Teatr Słowackiego|Teatr Variété|Teligi|Turowicza|UKEN|Uniwersytet Ekonomiczny|Urzędnicza|Wańkowicza|Wawel|Wesele|Witosa|Wlotowa|Wzgórza Krzesławickie|Zabłocie|Zajezdnia Nowa Huta|Zalew Nowohucki|"


        Public Class Ami_Departure
            Public Property line As String
            Public Property direction As String
            Public Property planned As String
            Public Property estimated As String
            Public Property delay As Integer
        End Class


        Protected Class Ami_Schedule_Stop
            Public Property _stop As Ami_Schedule_Stop_Stop
            Public Property lines() As Ami_Schedule_Stop_Line
        End Class

        Protected Class Ami_Schedule_Stop_Stop
            Public Property id As Integer
            Public Property name As String
            Public Property positions() As Ami_Position
            Public Property street As String
            Public Property city As String
        End Class

        Protected Class Ami_Schedule_Stop_Line
            Public Property line As Ami_Schedule_Stop_Line1
            Public Property variants() As Ami_Schedule_Stop_Variant
        End Class

        Protected Class Ami_Schedule_Stop_Line1
            Public Property id As String
            Public Property vehicle As String
            Public Property colors As Ami_Schedule_Stop_Colors
            Public Property suitableForDisabled As Boolean
            Public Property detour As Boolean
            Public Property suspended As Boolean
            Public Property validFrom As String
        End Class

        Protected Class Ami_Schedule_Stop_Colors
            Public Property cardColor As String
            Public Property textColor As String
            Public Property cardHeaderColor As String
            Public Property borderColor As String
            Public Property applyOpacity As Boolean
        End Class

        Protected Class Ami_Schedule_Stop_Variant
            Public Property departureId As Ami_Schedule_Stop_Departureid
            Public Property header As Ami_Schedule_Stop_Header
            Public Property departures() As Ami_Schedule_Stop_Departure
        End Class

        Protected Class Ami_Schedule_Stop_Departureid
            Public Property variantId As String
            Public Property stopId As Integer
        End Class

        Protected Class Ami_Schedule_Stop_Header
            Public Property id As String
            Public Property name As String
            Public Property streets As String
        End Class

        Protected Class Ami_Schedule_Stop_Departure
            Public Property hour As Integer
            Public Property minute As String
        End Class




    End Class

End Namespace