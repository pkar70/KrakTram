'GetHTMLSource.vbs
args = WScript.Arguments.Count
if args <> 1 then
  Wscript.Echo "usage: GetHTMLSource.vbs URL"
  wscript.Quit
end if
URL = WScript.Arguments.Item(0)
'wscript.echo URL
Set WshShell = WScript.CreateObject("WScript.Shell")
'Set http = CreateObject("Microsoft.XmlHttp")
Set http = CreateObject("WinHttp.WinHttpRequest.5.1")
http.open "GET", URL, FALSE
http.send ""
WScript.Echo http.responseText
set WshShell = nothing
set http = nothing
 
