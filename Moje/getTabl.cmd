@ECHO OFF
SET sStop=632
IF %1. == . GOTO lDefault
SET sStop=%1

:lDefault
cscript c:\bat\getHtml.vbs http://www.ttss.krakow.pl/internetservice/services/passageInfo/stopPassages/stop?mode=departure^&stop=%sStop%