using RandM.RMLib;
using System;

namespace RandM.TelnetDoor
{
    class Program
    {
        static string FHostName = "";
        static int FPort = 23;
        static TelnetConnection FTelnet = new TelnetConnection();

        static void Main(string[] args)
        {
            Door.OnCLP += OnCLP;
            Door.Startup(args);

            if (FHostName == "")
            {
                Door.WriteLn("Your SysOp didn't tell me where to connect you!");
                Door.WriteLn();
                Door.WriteLn("(Tell him he forgot the -S parameter!)");
            }
            else
            {
                Door.Write("Connecting to telnet server...");

                if (FTelnet.Connect(FHostName, FPort))
                {
                    Door.WriteLn("connected!");

                    if (Door.Local())
                    {
                        Ansi.ESC5nEvent += new EventHandler(Ansi_ESC5nEvent);
                        Ansi.ESC6nEvent += new EventHandler(Ansi_ESC6nEvent);
                        Ansi.ESC255nEvent += new EventHandler(Ansi_ESC255nEvent);
                    }

                    while ((Door.Carrier) && (FTelnet.Connected))
                    {
                        bool Yield = true;

                        // See if the server sent anything to the client
                        if (FTelnet.CanRead())
                        {
                            Door.Write(FTelnet.ReadString());
                            Yield = false;
                        }

                        // See if the client sent anything to the server
                        if (Door.KeyPressed())
                        {
                            string ToSend = "";
                            while (Door.KeyPressed()) ToSend += Door.ReadKey();
                            FTelnet.Write(ToSend);

                            Yield = false;
                        }

                        // See if we need to yield
                        if (Yield) Crt.Delay(1);
                    }

                    if ((Door.Carrier) && (!FTelnet.Connected))
                    {
                        Door.WriteLn();
                        Door.WriteLn("Disconnected from telnet server.");
                    }
                }
                else
                {
                    Door.WriteLn("failed!");
                    Door.WriteLn();
                    Door.WriteLn("Looks like the telnet server isn't online, please try back later.");
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
            if (FTelnet.Connected)
            {
                FTelnet.Write("\x1b" + "[0n");
            }
        }

        static void Ansi_ESC6nEvent(object sender, EventArgs e)
        {
            if (FTelnet.Connected)
            {
                FTelnet.Write(Ansi.CursorPosition());
            }
        }

        static void Ansi_ESC255nEvent(object sender, EventArgs e)
        {
            if (FTelnet.Connected)
            {
                FTelnet.Write(Ansi.CursorPosition(Crt.ScreenCols, Crt.ScreenRows));
            }
        }

        static void OnCLP(object sender, CommandLineParameterEventArgs e)
        {
            if (e.Key == 'P')
            {
                int.TryParse(e.Value, out FPort);
                if ((FPort < 1) || (FPort > 65535)) FPort = 23;
            }
            else if (e.Key == 'S')
            {
                FHostName = e.Value;
            }

        }
    }
}
