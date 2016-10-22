/*
  TelnetDoor: A BBS door to allow outgoing telnet or rlogin connections
  Copyright (C) 2016  Rick Parrish, R&M Software

  This file is part of TelnetDoor.

  TelnetDoor is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  any later version.

  TelnetDoor is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with TelnetDoor.  If not, see <http://www.gnu.org/licenses/>.
*/
using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace RandM.TelnetDoor
{
    class Program
    {
        static string _HostName = "";
        static string _MenuFile = "TelnetDoor";
        static int _Port = 0;
        static bool _RLogin = false;
        static string _RLoginClientUserName = "";
        static string _RLoginServerUserName = "";
        static string _RLoginTerminalType = "ansi-bbs";
        static TcpConnection _Server;

        static void Main(string[] args)
        {
            try
            {
                Door.Startup();
                Door.StripLF = false;
                Door.TextAttr(7);
                Door.ClrScr();
                Door.GotoXY(1, 1);
                Door.KeyPressed(); // Ensures statusbar gets drawn before connecting (which blocks updates)

                // Default values (could be overridden when handling CLPs)
                _RLoginClientUserName = Door.DropInfo.Alias;
                _RLoginServerUserName = Door.DropInfo.Alias;

                // Parse CLPs
                HandleCLPs(args);

                // Check if we're being told where to connect, or should display a menu
                if (string.IsNullOrEmpty(_HostName))
                {
                    // No hostname, display a menu
                    Menu();
                }
                else
                {
                    // Have a hostname, connect to it
                    Connect();

                    // Pause before quitting
                    Door.ClearBuffers();
                    Door.WriteLn();
                    Door.TextAttr(15);
                    Door.Write(new string(' ', 30) + "Hit any key to quit");
                    Door.TextAttr(7);
                    Door.ReadKey();
                }

            }
            catch (Exception ex)
            {
                FileUtils.FileAppendAllText("ex.log", ex.ToString() + Environment.NewLine);
            }
            finally
            {
                Door.Shutdown();
            }
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

        private static void Connect()
        {
            Door.WriteLn();
            Door.Write(" Connecting to remote server...");

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
                        Door.WriteLn(" Looks like the remote server doesn't accept RLogin connections.");
                    }
                }

                if (CanContinue)
                {
                    Door.WriteLn("connected!");

                    if (Door.Local)
                    {
                        Ansi.ESC5nEvent += new EventHandler(Ansi_ESC5nEvent);
                        Ansi.ESC6nEvent += new EventHandler(Ansi_ESC6nEvent);
                        Ansi.ESC255nEvent += new EventHandler(Ansi_ESC255nEvent);
                    }

                    Door.PipeWrite = false;
                    bool UserAborted = false;
                    while (!UserAborted && Door.Carrier && _Server.Connected)
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
                            while (Door.KeyPressed())
                            {
                                byte B = (byte)Door.ReadByte();
                                if (B == 29)
                                {
                                    // Ctrl-]
                                    _Server.Close();
                                    UserAborted = true;
                                    break;
                                }
                                else
                                {
                                    ToSend += (char)B;
                                }
                            }
                            _Server.Write(ToSend);
                            if (Door.LocalEcho) Door.Write(ToSend);

                            Yield = false;
                        }

                        // See if we need to yield
                        if (Yield) Crt.Delay(1);
                    }
                    Door.PipeWrite = true;

                    if (UserAborted)
                    {
                        Door.WriteLn();
                        Door.WriteLn();
                        Door.WriteLn(" User hit CTRL-] to disconnect from server.");
                    }
                    else if ((Door.Carrier) && (!_Server.Connected))
                    {
                        Door.WriteLn();
                        Door.WriteLn();
                        Door.WriteLn(" Remote server closed the connection.");
                    }
                }
            }
            else
            {
                Door.WriteLn("failed!");
                Door.WriteLn();
                Door.WriteLn(" Looks like the remote server isn't online, please try back later.");
            }
        }

        private static void HandleCLPs(string[] args)
        {
            foreach (string Arg in args)
            {
                if ((Arg.Length >= 2) && ((Arg[0] == '/') || (Arg[0] == '-')))
                {
                    char Key = Arg.ToUpper()[1];
                    string Value = Arg.Substring(2);

                    switch (Key)
                    {
                        case 'E':
                            Door.LocalEcho = true;
                            break;
                        case 'M':
                            _MenuFile = Path.GetFileNameWithoutExtension(Value); // Just in case they specify .ini
                            break;
                        case 'P':
                            if (!int.TryParse(Value, out _Port)) _Port = 0;
                            break;
                        case 'R':
                            _RLogin = true;
                            break;
                        case 'S':
                            WebUtils.ParseHostPort(Value, ref _HostName, ref _Port);
                            break;
                        case 'X':
                            _RLogin = true;
                            _RLoginClientUserName = Value;
                            break;
                        case 'Y':
                            _RLogin = true;
                            _RLoginServerUserName = Value;
                            break;
                        case 'Z':
                            _RLogin = true;
                            _RLoginTerminalType = Value;
                            break;
                    }
                }
            }
        }

        private static void Menu()
        {
            string AnsiHeader = StringUtils.PathCombine(ProcessUtils.StartupPath, _MenuFile + "-Header.ans");
            string AnsiMenu = StringUtils.PathCombine(ProcessUtils.StartupPath, _MenuFile + ".ans");
            SortedDictionary<char, string> Servers = new SortedDictionary<char, string>();
            string ServersIni = StringUtils.PathCombine(ProcessUtils.StartupPath, _MenuFile + ".ini");

            if (File.Exists(ServersIni))
            {
                using (IniFile Ini = new IniFile(ServersIni))
                {
                    char HotKey = 'A';
                    string[] Sections = Ini.ReadSections();
                    foreach (string Section in Sections)
                    {
                        Servers.Add(HotKey, Section);
                        HotKey = (char)(HotKey + 1);
                    }

                    // Going to repeatedly display this menu until the user hits ESC to quit
                    while (true)
                    {
                        // Reset display for re-display of menu
                        Door.TextAttr(7);
                        Door.ClrScr();
                        Door.GotoXY(1, 1);

                        // Check whether we display a custom menu or the canned menu
                        if (File.Exists(AnsiMenu))
                        {
                            // Custom menu
                            Door.DisplayFile(AnsiMenu, 0);
                        }
                        else
                        {
                            // Canned menu, check if we display a custom header
                            if (File.Exists(AnsiHeader))
                            {
                                // Custom header
                                Door.DisplayFile(AnsiHeader, 0);
                            }
                            Door.WriteLn();

                            // Menu options
                            foreach (KeyValuePair<char, string> KVP in Servers)
                            {
                                Door.WriteLn("  |0F[|0E" + KVP.Key.ToString() + "|0F]|07 " + KVP.Value);
                            }
                            Door.WriteLn();
                            Door.Write("  |0FYour choice (|0EESC|0F to abort):|07 ");
                        }

                        char? Ch = Door.ReadKey();
                        if (Ch == null)
                        {
                            // Must have timed out waiting for input
                            return;
                        }
                        else if ((char)Ch == '\x1B')
                        {
                            // Aborting
                            return;
                        }
                        else
                        {
                            // Ensure key is valid
                            HotKey = char.ToUpper((char)Ch);
                            if (Servers.ContainsKey(HotKey))
                            {
                                // Valid option, set the parameters and connect
                                List<string> Args = new List<string>();
                                string[] Keys = Ini.ReadSection(Servers[HotKey]);
                                foreach (string Key in Keys)
                                {
                                    Args.Add("-" + Key + Ini.ReadString(Servers[HotKey], Key, ""));
                                }
                                HandleCLPs(Args.ToArray());

                                // Clear the screen and connect
                                Door.ClrScr();
                                Door.GotoXY(1, 1);
                                Connect();

                                // Disconnectd, pause to show the user the disconnect message
                                Door.ClearBuffers();
                                Door.WriteLn();
                                Door.TextAttr(15);
                                Door.Write(new string(' ', 30) + "Hit any key to continue");
                                Door.TextAttr(7);
                                Door.ReadKey();
                            }
                        }
                    }
                }
            }
            else
            {
                Door.WriteLn();
                Door.WriteLn(" Oops, your SysOp didn't set this door up correctly!");
                Door.WriteLn();
                Door.WriteLn(" Tell them to either pass the -S parameter, or create a " + _MenuFile + ".ini file");
                Door.WriteLn();

                Door.ClearBuffers();
                Door.TextAttr(15);
                Door.Write(new string(' ', 30) + "Hit any key to quit");
                Door.TextAttr(7);
                Door.ReadKey();
            }
        }
    }
}
