﻿
' ponieważ NIE DZIAŁA pod Uno.Android wczytywanie pliku (apk nie jest rozpakowany?),
' to w ten sposób przekazywanie zawartości pliku INI
' wychodzi na to samo, edycja pliku defaults.ini albo defsIni.lib.vb

Public Class IniLikeDefaults

    Public Const sIniContent As String = "
[main]
# jesli nastepny pojazd tej samej linii jest wczesniej niz tyle minut po poprzednim, to tez pokaze
alsoNext=5
# prędkość marszu (w km/h), przy odległości od przystanku
walkSpeed=4
# odległość od przystanku
maxOdl=1000
# odległość traktowanych przystanków jako jeden, w metrach 
treatAsSameStop=150
# remark
' remark
; remark
// remark

[debug]
key=value # remark

[app]
; lista z app (bez ustawiania)
widthCol0=0     # szerokosc pola linia, typ pojazdu
widthCol3=0     # szerokosc pola czas odjazdu
LastLoadStops=yymmdd    # data ostatniego wczytania
pkarmode=false
gpsPrec=0       # dokładność, przeliczana z Android oraz UWP
settingsAlsoBus=true   # bus/tram, juz zawsze True - jakby gdzieś jeszcze się odwoływało...
sortMode=3      # sortowanie odjazdów: *:linia, 1:przystanek, 2:kierunek, 3:time
androAutoTram=false
favPlaces=      # dawna wersja listy fav, xml w zmiennej
uiAndroAutoTram=true

[libs]
; lista z pkarmodule
remoteSystemDisabled=false
appFailData=
offline=false
lastPolnocnyTry=
lastPolnocnyOk=

"

End Class
