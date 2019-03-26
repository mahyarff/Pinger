using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Pinger
{
    class Program
    {
        private static int _checkInterval = 5000;
        private static string _host = "4.2.2.4";
        private static bool _hostResolved = false;
        private static readonly string LogfileName = string.Format("pingLog_{0:yyyy-MM-dd_HH-mm}.log", DateTime.Now);
        private static readonly Regex R = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
        private static readonly ContextMenu NiContextMenu = new ContextMenu();
        private static readonly NotifyIcon Ni = new NotifyIcon() { Icon = Resource.pinger2, Text = @"Pinger" };
        private static readonly MenuItem MnuExit = new MenuItem("Exit", (s, e) => { OnExit(); });
        private static bool _flagHadError = true;
        private static Thread _notifyThread = null;

        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(ConsoleCtrlCheck, true);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => { OnExit(); };
            _notifyThread = new Thread(
                () =>
                {
                    NiContextMenu.MenuItems.Add(0, MnuExit);
                    Ni.ContextMenu = NiContextMenu;
                    Ni.Visible = true;
                    Application.Run();
                }
            );
            _notifyThread.Start();

            string text2 = "";
            string text3 = "";
            if (args.Length == 0)
            {
                Console.Write(@"Type a number for Check Interval in millisenconds [5000]: ");
                text2 = Reader.ReadLine(10000);
                if (text2 == null) Console.Write(Environment.NewLine);
                Console.Write(@"Type a Hostname/IP [4.2.2.4]: ");
                text3 = Reader.ReadLine(10000);
            }
            else
            {
                text2 = args[0].Trim();
                if (args.Length > 1)
                    text3 = args[1].Trim();
            }


            Console.Write(Environment.NewLine);

            if (!int.TryParse(text2, out _checkInterval))
                _checkInterval = 5000;

            if (IsValidDomainName(text3))
                _host = text3;



            if (true)
            {
                ////PrintPublicIp();

                //set the ping options, TTL 128
                PingOptions pingOptions = new PingOptions(128, true);

                //create a new ping instance
                Ping ping = new Ping();

                //32 byte buffer (create empty)
                byte[] buffer = new byte[32];

                IPHostEntry iphost = null;

                while (true)
                {
                    try
                    {
                        if (!_hostResolved)
                        {
                            iphost = Dns.GetHostEntry(_host);
                            if (iphost.AddressList.Length == 0)
                                return;
                        }
                        IPAddress address = iphost.AddressList[0];
                        _hostResolved = true;

                        System.Threading.Thread.Sleep(_checkInterval);

                        //send the ping 4 times to the host and record the returned data.
                        //The Send() method expects 4 items:
                        //1) The IPAddress we are pinging
                        //2) The timeout value
                        //3) A buffer (our byte array)
                        //4) PingOptions
                        PingReply pingReply = ping.Send(address, 3000, buffer, pingOptions);

                        //make sure we dont have a null reply
                        if (pingReply != null)
                        {
                            if (pingReply.Status == IPStatus.Success)
                            {
                                int time = Convert.ToInt32(pingReply.RoundtripTime);
                                if (_flagHadError)
                                {
                                    PrintPublicIp();
                                    PingerSuccess();
                                    _flagHadError = false;
                                }
                                LogWrite(string.Format(
                                    "{0:yyyy-MM-dd HH:mm:ss} - Reply from {1}: bytes={2} time={3}ms TTL={4}",
                                    DateTime.Now, address.ToString(), buffer.Length, time,
                                    pingOptions.Ttl));
                            }
                            else
                            {
                                var pingStatus = R.Replace(pingReply.Status.ToString(), " ");
                                LogWrite(string.Format(
                                    "{0:yyyy-MM-dd HH:mm:ss} - {1}",
                                    DateTime.Now, pingStatus));
                                PingerFail(pingStatus);
                                _flagHadError = true;
                            }
                        }
                    }
                    catch (PingException ex)
                    {
                        LogWrite(string.Format(
                            "{0:yyyy-MM-dd HH:mm:ss} - {1}",
                            DateTime.Now, ex.Message));
                        PingerFail();
                        _flagHadError = true;
                    }
                    catch (SocketException ex)
                    {
                        LogWrite(string.Format(
                            "{0:yyyy-MM-dd HH:mm:ss} - {1}",
                            DateTime.Now, ex.Message));
                        PingerFail();
                        _flagHadError = true;
                    }
                }
            }
        }

        private static void PrintPublicIp()
        {
            try
            {
                string externalip = new WebClient() { Proxy = null }.DownloadString("http://icanhazip.com");
                LogWrite(string.Format("{0:yyyy-MM-dd HH:mm:ss} - My Public IP: {1}", DateTime.Now, externalip.Trim()));
            }
            catch (WebException e)
            {
                LogWrite(string.Format("{0:yyyy-MM-dd HH:mm:ss} - No Internet!", DateTime.Now));
                //Console.WriteLine("enter a key...");
                //Console.ReadKey();
                //Environment.Exit(-1);
            }

        }

        private static void LogWrite(string text)
        {
            Console.WriteLine(text);
            File.AppendAllText(LogfileName, text + Environment.NewLine, Encoding.UTF8);
        }

        private static void PingerFail(string message = "")
        {
            Ni.Icon = Resource.pingerFail;
            if (!_flagHadError)
            {
                Ni.BalloonTipText = string.Format("Internet disconnected!\r\n{0}", message);
                Ni.ShowBalloonTip(4000);
            }
        }

        private static void PingerSuccess(string message = "")
        {
            Ni.Icon = Resource.pingerSuccess;
            Ni.BalloonTipText = string.IsNullOrWhiteSpace(message) ? @"Internet connected.." : message;
            Ni.ShowBalloonTip(4000);
        }

        private static bool IsValidDomainName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            return Uri.CheckHostName(name) != UriHostNameType.Unknown;
        }

        private static double PingTimeAverage(string host, int echoNum)
        {
            long totalTime = 0;
            int timeout = 3000;
            Ping pingSender = new Ping();

            for (int i = 0; i < echoNum; i++)
            {
                PingReply reply = pingSender.Send(host, timeout, new byte[32], new PingOptions());
                if (reply.Status == IPStatus.Success)
                {
                    totalTime += reply.RoundtripTime;
                }
            }
            return totalTime / echoNum;
        }


        private static void OnExit()
        {
            //if (_notifyThread != null && _notifyThread.IsAlive)
            //    _notifyThread.Abort();
            try
            {
                if (Ni != null)
                {
                    Ni.Visible = false;
                    Application.DoEvents();
                    Ni.Icon = null;
                    Ni.Dispose();
                }
                Application.Exit();
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

        // A delegate type to be used as the handler routine 
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            // Put your own handler here
            OnExit();
            return true;
        }


    }


}
