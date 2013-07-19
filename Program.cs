using RandM.RMLib;
using System;

namespace RandM.TelnetDoor
{
    class Program
    {
        static string _HostName = "";
        static int _Port = 0;
        static bool _RLogin = false;
        static string _RLoginClientUserName = "";
        static string _RLoginServerUserName = "";
        static string _RLoginTerminalType = "";
        static TcpConnection _Server;

        static void Main(string[] args)
        {
            Door.OnCLP += OnCLP;
            Door.Startup(args);
            Door.StripLF = false;

            if (string.IsNullOrEmpty(_HostName))
            {
                Door.WriteLn("Your SysOp didn't tell me where to connect you!");
                Door.WriteLn();
                Door.WriteLn("(Tell him he forgot the -S parameter!)");
            }
            else
            {
                Door.Write("Connecting to remote server...");

                if (_RLogin)
                {
                    _Server = new RLoginConnection();
                }
                else
                {
                    _Server = new TelnetConnection();
                }

                // Sanity check on the port
                if ((_Port < 1) || (_Port > 65535))
                {
                    _Port = (_RLogin) ? 513 : 23;
                } 

                if (_Server.Connect(_HostName, _Port))
                {
                    bool CanContinue = true;
                    if (_RLogin)
                    {
                        // Send rlogin header
                        _Server.Write("\0" + _RLoginClientUserName + "\0" + _RLoginServerUserName + "\0" + _RLoginTerminalType + "\0");

                        // Wait up to 5 seconds for a response
                        char? Ch = _Server.ReadChar(5000);
                        if ((Ch == null) || (Ch != '\0'))
                        {
                            CanContinue = false;
                            Door.WriteLn("failed!");
                            Door.WriteLn();
                            Door.WriteLn("Looks like the remote server doesn't accept RLogin connections.");
                        }
                    }

                    if (CanContinue)
                    {
                        Door.WriteLn("connected!");

                        if (Door.Local())
                        {
                            Ansi.ESC5nEvent += new EventHandler(Ansi_ESC5nEvent);
                            Ansi.ESC6nEvent += new EventHandler(Ansi_ESC6nEvent);
                            Ansi.ESC255nEvent += new EventHandler(Ansi_ESC255nEvent);
                        }

                        while ((Door.Carrier) && (_Server.Connected))
                        {
                            bool Yield = true;

                            // See if the server sent anything to the client
                            if (_Server.CanRead())
                            {
                                Door.Write(_Server.ReadString());
                                Yield = false;
                            }

                            // See if the client sent anything to the server
                            if (Door.KeyPressed())
                            {
                                string ToSend = "";
                                while (Door.KeyPressed()) ToSend += Door.ReadKey();
                                _Server.Write(ToSend);

                                Yield = false;
                            }

                            // See if we need to yield
                            if (Yield) Crt.Delay(1);
                        }

                        if ((Door.Carrier) && (!_Server.Connected))
                        {
                            Door.WriteLn();
                            Door.WriteLn("Remote server closed the connection.");
                        }
                    }
                }
                else
                {
                    Door.WriteLn("failed!");
                    Door.WriteLn();
                    Door.WriteLn("Looks like the remote server isn't online, please try back later.");
                }
            }

            Door.WriteLn();
            Door.TextAttr(15);
            Door.Write(new string(' ', 30) + "Hit any key to Quit");
            Door.TextAttr(7);
            Door.ReadKey();
            Door.Shutdown();
        }

        static void Ansi_ESC5nEvent(object sender, EventArgs e)
        {
            if (_Server.Connected)
            {
                _Server.Write("\x1b" + "[0n");
            }
        }

        static void Ansi_ESC6nEvent(object sender, EventArgs e)
        {
            if (_Server.Connected)
            {
                _Server.Write(Ansi.CursorPosition());
            }
        }

        static void Ansi_ESC255nEvent(object sender, EventArgs e)
        {
            if (_Server.Connected)
            {
                _Server.Write(Ansi.CursorPosition(Crt.ScreenCols, Crt.ScreenRows));
            }
        }

        static void OnCLP(object sender, CommandLineParameterEventArgs e)
        {
            if (e.Key == 'E')
            {
                Door.LocalEcho = true;
            }
            else if (e.Key == 'P')
            {
                if (!int.TryParse(e.Value, out _Port)) _Port = 0;
            }
            else if (e.Key == 'S')
            {
                _HostName = e.Value;
            }
            else if (e.Key == 'X')
            {
                _RLogin = true;
                _RLoginClientUserName = e.Value;
            }
            else if (e.Key == 'Y')
            {
                _RLogin = true;
                _RLoginServerUserName = e.Value;
            }
            else if (e.Key == 'Z')
            {
                _RLogin = true;
                _RLoginTerminalType = e.Value;
            }
        }
    }
}
