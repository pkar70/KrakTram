v.3.1: Added bus support (you can enable this feature in Setup).

v.2.1:
a) history of tram network in Kraków
b) stop numbers (multiply by 2 to approximate travel time in minutes)

v2.0.4: small correction for "%UNIT_MIN%" string from server, and cache of stops for line is longer.

Version 2.0 (Windows 10 Anniversary Edition required):
 Reworked code, and added features:
* night mode (app respect system theme)
* when showing data from favourites, or from selected stop, GPS is not consulted (previously app tries to get location anyway, although not use it)
* go to stop/favourites is much more convenient
* you can show stops' list of selected line (this info is cached for 24 hours)


Version 1.5:
 App can show you stops' list, and you can add them to list of your favourite places.
 Also some minor fixes.

Version 1.2.1:
 Better handling of unexpected.
 Also you can see in top-right corner of screen icon telling you what is happening: "o" means waiting for location service; "." means iterating of stops, and any of "\|/-" means getting data for next stop.


Wersja 2.0.4: usuniêcie "%UNIT_MIN%" z danych z serwera, oraz cache przystanków linii jest d³u¿ej wa¿ny (mo¿na wymusiæ aktualizacjê).



Wersja 2.0 (wymaga Windows 10 Anniversary Edition):
 Du¿o przeredagowanego kodu, i dodane takie funkcje jak::
* tryb nocny (zgodnie z ustawieniami systemowymi)
* gdy ma pokazaæ dane tabliczek z Ulubionych, albo dla wybranego przystanku, nie korzysta z GPS (poprzednio odwo³ywa³ siê do GPS, mimo ¿e z tych danych i tak nie korzysta³)
* korzystanie z listy ulubionych i listy przystanków jest wygodniejsze
* mo¿na zobaczyæ trasê wybranej linii (listê przystanków) - te dane s¹ pamiêtane przez 24 godziny.
* mo¿na zobaczyæ opóŸnienie tramwaju



zikit numer od ttss: 12 616 74 45


getbinhttp "http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/stopinfo/stops?left=-648000000&bottom=-324000000&right=648000000&top=324000000" przyst.txt

left=-648000000&
bottom=-324000000&
right=648000000&
top=324000000" przyst.txt


Aska:
19.9186
50.0536

236 Salwator
266 Komorowskiego


Ja:
19.97842
50.01992

178 Dauna
411 Biezanowska




Widok ~500
Franciszkanska ~500
Bernardynska ~200
GIS
Klimek

Powinny wystarczyc 4 pozycje po przecinki (1° na rowniku ma 112 km, czyli 0.0001 to 11 m, a SQRT(2)*11m to 16 m - przek¹tna)
