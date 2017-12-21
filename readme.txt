Run TelnetDoor.exe for some usage information.  Additional parameters not listed there:

-S		Specify the server (hostname or ip address) to connect to
-P		Override the default port number (which is 23)

-R		Use rlogin instead of telnet
-X		Override the default client username (which is the user's alias)
-Y		Override the default server username (which is the user's alias)
-Z		Override the default terminal type (which is ansi-bbs)

-E		Turn on local echo (default is for server to echo)

-M		Specify a different menu file (default is TelnetDoor.ini)

-W      Wait up to a specified number of seconds before quitting (user can hit a key to quit sooner).  Default is 5 seconds
        If you don't want to display a "Hit any key to quit" prompt, pass 0 as the number of seconds


Examples:

TelneteDoor.exe -L -Sbbs.ftelnet.ca -P23
Connecto to bbs.ftelnet.ca:23 in local mode

TelnetDoor.exe -D*DOOR32
Display the contents of TelnetDoor.ini (-D passes the dropfile, *DOOR32 is how GameSrv would pass the dropfile path)

TelnetDoor.exe -D*DOOR32 -MAmigaBBSes
Display the contents of AmigaBBSes.ini instead of TelnetDoor.ini


Notes for displaying a list of servers:

If a TelnetDoor-Header.ans exists, this header will be displayed before the list of servers.

If a TelnetDoor.ans exists, this screen will be displayed INSTEAD OF the list of servers.  This
means TelnetDoor.ans will have to include the list of servers.